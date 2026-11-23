using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using Rage.Native;
using lumen_sante.Clinical;
using lumen_sante.Interface;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal static partial class HospitalShift
    {
        private sealed class WorkerCoordinator
        {
            private const int ConsultationMinutes = 20;
            private enum TaskStage { ToComputer, Calling, WaitingPatient, Consulting, ToPatient, Visiting }

            private sealed class Assignment
            {
                internal AmbientPatrol Doctor;
                internal CareAuthor Author;
                internal PatientCase Patient;
                internal int Room = -1;
                internal Point Destination;
                internal NpcRoute Walk = new NpcRoute();
                internal TaskStage Stage;
                internal long Revision;
                internal long Until;
                internal long Deadline;
                internal bool Seated;
            }

            private readonly HospitalPath path;
            private readonly AmbientPopulation population;
            private readonly WaitingRoom waitingRoom;
            private readonly PatientCase[] consultations;
            private readonly Point[] computers;
            private readonly Point[] beds;
            private readonly Func<PatientCase[]> patients;
            private readonly Func<int, Ped, bool, bool> call;
            private readonly Func<PatientCase, InpatientUnit, bool, bool> admit;
            private readonly Func<PatientCase, CareAuthor, long, bool> discharge;
            private readonly Func<PatientCase, ExamOrder, CareAuthor, bool> request;
            private readonly Func<PatientCase, bool> returnToWaiting;
            private readonly Func<PatientCase, bool> blocked;
            private readonly Func<int, bool> playerAtComputer;
            private readonly Func<bool> playerDiagnosing;
            private readonly Action refresh;
            private readonly Random random;
            private readonly WorkerDiagnosticRules rules = new WorkerDiagnosticRules(DiseaseCatalog.All);
            private readonly WorkerReservations reservations = new WorkerReservations();
            private readonly List<Assignment> assignments = new List<Assignment>();
            private readonly Dictionary<AmbientPatrol, CareAuthor> authors = new Dictionary<AmbientPatrol, CareAuthor>();
            private readonly long[] playerActivity = new long[3];
            private readonly double[] playerCoverage = new double[3];
            private long nextSchedule;

            internal WorkerCoordinator(HospitalPath path, AmbientPopulation population, WaitingRoom waitingRoom,
                PatientCase[] consultations, Point[] computers, Point[] beds,
                Func<PatientCase[]> patients, Func<int, Ped, bool, bool> call,
                Func<PatientCase, InpatientUnit, bool, bool> admit, Func<PatientCase, CareAuthor, long, bool> discharge,
                Func<PatientCase, ExamOrder, CareAuthor, bool> request, Func<PatientCase, bool> returnToWaiting,
                Func<PatientCase, bool> blocked, Func<int, bool> playerAtComputer,
                Func<bool> playerDiagnosing, Action refresh, Random random)
            {
                this.path = path;
                this.population = population;
                this.waitingRoom = waitingRoom;
                this.consultations = consultations;
                this.computers = computers;
                this.beds = beds;
                this.patients = patients;
                this.call = call;
                this.admit = admit;
                this.discharge = discharge;
                this.request = request;
                this.returnToWaiting = returnToWaiting;
                this.blocked = blocked;
                this.playerAtComputer = playerAtComputer;
                this.playerDiagnosing = playerDiagnosing;
                this.refresh = refresh;
                this.random = random;
            }

            private static string PatientResource(PatientCase patient) => "patient:" + patient.Orders.AdmissionId.ToString("N");
            private static string StationResource(int room) => "station:" + room;
            internal bool IsPatientReserved(PatientCase patient) => reservations.IsReserved(PatientResource(patient));

            private static int Area(PatientCase patient) => patient.InpatientRoomIndex < 0 ? 0 :
                InpatientRooms[patient.InpatientRoomIndex].Unit == InpatientUnit.Ward ? 1 : 2;

            private double Coverage(int area, long now) => playerCoverage[area] *
                Math.Max(0, 1d - (double)(now - playerActivity[area]) / GameClock.Minutes(60));

            internal void PlayerTakesPatient(PatientCase patient)
            {
                long now = GameClock.NowMilliseconds;
                int area = Area(patient);
                playerCoverage[area] = Math.Min(6, Coverage(area, now) + 1);
                playerActivity[area] = now;
                CancelPatient(patient);
                patient.WorkerConsultation = false;
                patient.WorkerDueAt = now + GameClock.Minutes(10);
            }

            internal void CancelPatient(PatientCase patient)
            {
                foreach (Assignment task in assignments.Where(task => task.Patient == patient).ToArray()) Release(task);
            }

            internal void YieldComputer(int room)
            {
                foreach (Assignment task in assignments.Where(task => task.Room == room).ToArray())
                {
                    task.Patient.WorkerConsultation = false;
                    task.Patient.WorkerDueAt = GameClock.NowMilliseconds + GameClock.Minutes(10);
                    Release(task);
                }
            }

            internal void Update()
            {
                long now = GameClock.NowMilliseconds;
                foreach (AmbientPatrol departed in authors.Keys.Where(doctor => !doctor.Actor.IsAlive).ToArray()) authors.Remove(departed);
                foreach (Assignment task in assignments.ToArray())
                {
                    try { UpdateAssignment(task, now); }
                    catch (InvalidOperationException error)
                    {
                        Game.LogTrivial("[Lumen Santé] Atendimento do Doctor interrompido: " + error.Message);
                        task.Patient.WorkerDueAt = now + GameClock.Minutes(5);
                        Release(task);
                    }
                }
                if (now < nextSchedule) return;
                nextSchedule = now + GameClock.FromRealMilliseconds(2000);
                PatientCase[] available = patients().Where(CanAttend).ToArray();
                foreach (AmbientPatrol doctor in population.AvailableDoctors.ToArray())
                {
                    PatientCase patient = available.Where(CanAttend).OrderByDescending(candidate => Priority(candidate, now) -
                        Math.Sqrt((candidate.Ped.Position - doctor.Actor.Actor.Position).LengthSquared()) / 100d)
                        .FirstOrDefault(candidate => candidate.Deceased || candidate.Location != PatientLocation.WaitingReception || FindConsultationRoom() >= 0);
                    if (patient == null) break;
                    try { Assign(doctor, patient, now); }
                    catch (InvalidOperationException error) { Game.LogTrivial("[Lumen Santé] Doctor aguardará nova tarefa: " + error.Message); }
                }
            }

            private bool CanAttend(PatientCase patient)
            {
                if (patient?.Orders == null || patient.Ped == null || !patient.Ped.Exists() || (!patient.Deceased && !patient.Ped.IsAlive) ||
                    patient.Orders.IsDischarged || patient.WorkerDueAt > GameClock.NowMilliseconds || blocked(patient) || IsPatientReserved(patient)) return false;
                if (patient.Deceased) return !patient.CoronersCalled;
                if (patient.Appointment != null && patient.InpatientRoomIndex < 0) return false;
                if (patient.Orders.IsCareProtected && patient.InpatientRoomIndex < 0) return false;
                if (patient.Orders.IsRecovered && patient.Orders.IsCareProtected) return false;
                if (patient.Location == PatientLocation.Chair && playerDiagnosing()) return false;
                if (patient.Location == PatientLocation.Chair && !patient.WorkerConsultation &&
                    consultations.Any(other => other != patient && other?.WorkerConsultation == true)) return false;
                return patient.Location == PatientLocation.Chair || patient.Location == PatientLocation.InpatientBed ||
                    (patient.Location == PatientLocation.WaitingReception && waitingRoom.IsWaiting(patient.Ped));
            }

            private double Priority(PatientCase patient, long now)
            {
                double waiting = (double)(now - patient.LastWorkerVisit) / GameClock.Minutes(15);
                return waiting + (patient.Deceased ? 10 : 0) + (patient.Orders.IsRecovered ? 8 : 0) + (patient.AwaitingBed ? 4 : 0) +
                    Math.Max(0, patient.RiskLevel - 3) - Coverage(Area(patient), now) * 3;
            }

            private int FindConsultationRoom()
            {
                if (playerDiagnosing()) return -1;
                int committed = consultations.Count(patient => patient?.WorkerConsultation == true) +
                    assignments.Count(task => task.Stage == TaskStage.ToComputer || task.Stage == TaskStage.Calling);
                if (committed >= 1) return -1;
                foreach (int index in Enumerable.Range(0, consultations.Length).OrderBy(i => i == 1 ? 0 : 1))
                    if (consultations[index] == null && !playerAtComputer(index) && !reservations.IsReserved(StationResource(index))) return index;
                return -1;
            }

            private void Assign(AmbientPatrol doctor, PatientCase patient, long now)
            {
                CareAuthor author;
                if (!authors.TryGetValue(doctor, out author))
                {
                    author = CareAuthor.Doctor(doctor.StaffId, doctor.Name);
                    authors.Add(doctor, author);
                }
                int room = patient.Deceased ? -1 : patient.Location == PatientLocation.WaitingReception ? FindConsultationRoom() :
                    patient.Location == PatientLocation.Chair ? patient.DiagnosticRoomIndex : -1;
                if (!patient.Deceased && patient.Location == PatientLocation.WaitingReception && room < 0) return;
                if (room >= 0 && playerAtComputer(room)) return;
                string[] resources = room >= 0 ? new[] { PatientResource(patient), StationResource(room) } : new[] { PatientResource(patient) };
                if (!reservations.TryAcquire(author.Id, resources)) return;
                Point destination = patient.Deceased ? path.FindNearestWaypoint(patient.Ped.Position) :
                    patient.Location == PatientLocation.WaitingReception ? computers[room] :
                    room >= 0 ? computers[room] : beds[patient.InpatientRoomIndex];
                if (destination == null) { reservations.Release(author.Id); return; }
                var task = new Assignment
                {
                    Doctor = doctor, Author = author, Patient = patient, Room = room, Destination = destination,
                    Revision = patient.Orders.CareRevision,
                    Stage = !patient.Deceased && patient.Location == PatientLocation.WaitingReception ? TaskStage.ToComputer : TaskStage.ToPatient,
                    Deadline = now + GameClock.FromRealMilliseconds(120000)
                };
                assignments.Add(task);
                if (!patient.Deceased && patient.Location == PatientLocation.Chair) patient.WorkerConsultation = true;
                try
                {
                    Point origin = doctor.AssignToExam();
                    doctor.Actor.Actor.Tasks.Clear();
                    Point[] route = path.FindRoute(origin, destination);
                    if (route == null) throw new InvalidOperationException("Sem rota para o atendimento de " + patient.Name);
                    if (route.Length > 0) task.Walk.Start(doctor.Actor.Actor, origin, route, teleportOnTimeout: false);
                    Game.LogTrivial("[Lumen Santé] " + author.Name + " atenderá " + patient.Name + ".");
                }
                catch
                {
                    patient.WorkerDueAt = now + GameClock.Minutes(5);
                    Release(task);
                    throw;
                }
            }

            private void UpdateAssignment(Assignment task, long now)
            {
                PatientCase patient = task.Patient;
                if (!task.Doctor.Actor.IsAlive || patient.Ped == null || !patient.Ped.Exists() || (!patient.Deceased && !patient.Ped.IsAlive) ||
                    (patient.Deceased && patient.CoronersCalled) ||
                    patient.Orders.IsDischarged || patient.Orders.CareRevision != task.Revision || blocked(patient) ||
                    (task.Room >= 0 && (playerAtComputer(task.Room) || playerDiagnosing())) || now >= task.Deadline)
                {
                    patient.WorkerDueAt = now + GameClock.Minutes(5);
                    string reason = "paciente, estação ou profissional indisponível";
                    if (patient.Orders.CareRevision != task.Revision) reason = "prontuário alterado";
                    if (now >= task.Deadline) reason = "prazo do atendimento excedido";
                    Game.LogTrivial("[Lumen Santé] " + task.Author.Name + " interrompeu o atendimento de " + patient.Name + ": " + reason + ".");
                    Release(task);
                    return;
                }
                if (task.Stage == TaskStage.ToComputer || task.Stage == TaskStage.ToPatient)
                {
                    if (task.Walk.IsWalking && !task.Walk.Update(task.Doctor.Actor.Actor)) return;
                    if ((task.Doctor.Actor.Actor.Position - task.Destination.Position).LengthSquared() > 2.25f)
                        throw new InvalidOperationException("O Doctor não alcançou o ponto de atendimento.");
                    task.Deadline = now + GameClock.FromRealMilliseconds(120000);
                    if (task.Room >= 0) Sit(task);
                    else
                    {
                        HospitalReceptionist.LoadAnimation(InspectionAnimation);
                        task.Doctor.Actor.Actor.Tasks.PlayAnimation(InspectionAnimation, InspectionClip, 4f, AnimationFlags.Loop);
                    }
                    task.Stage = task.Stage == TaskStage.ToComputer ? TaskStage.Calling :
                        patient.Location == PatientLocation.Chair ? TaskStage.Consulting : TaskStage.Visiting;
                    int duration = 8;
                    if (task.Stage == TaskStage.Calling) duration = 2;
                    else if (task.Stage == TaskStage.Consulting) duration = ConsultationMinutes;
                    task.Until = now + GameClock.Minutes(duration);
                    return;
                }
                if (now < task.Until) return;
                if (task.Stage == TaskStage.Calling)
                {
                    if (!call(task.Room, patient.Ped, true))
                    {
                        task.Until = now + GameClock.Minutes(2);
                        return;
                    }
                    task.Stage = TaskStage.WaitingPatient;
                    return;
                }
                if (task.Stage == TaskStage.WaitingPatient)
                {
                    if (patient.Location != PatientLocation.Chair) return;
                    task.Stage = TaskStage.Consulting;
                    task.Until = now + GameClock.Minutes(ConsultationMinutes);
                    task.Deadline = now + GameClock.FromRealMilliseconds(120000);
                    return;
                }
                if (!patient.Deceased && patient.Location != PatientLocation.Chair && patient.Location != PatientLocation.InpatientBed)
                {
                    Release(task);
                    return;
                }
                Evaluate(task, now);
                patient.LastWorkerVisit = now;
                patient.WorkerDueAt = now + GameClock.Minutes(20);
                Release(task);
                refresh();
            }

            private void Evaluate(Assignment task, long now)
            {
                PatientCase patient = task.Patient;
                ExamOrders orders = patient.Orders;
                if (patient.Deceased)
                {
                    patient.CoronersCalled = true;
                    patient.CoronerPickupAt = now + GameClock.Minutes(30);
                    orders.RecordCoronerRequest(task.Author, now);
                    return;
                }
                if (!orders.IsRecovered && orders.WorkerDiagnosis.Assess(rules, patient.Disease, orders.Items, random))
                {
                    orders.RecordWorkerAssessment(task.Author, now, orders.WorkerDiagnosis.Hypothesis?.Name);
                    Game.LogTrivial("[Lumen Santé] " + task.Author.Name + ": " + patient.Name + " — " +
                        (orders.WorkerDiagnosis.Hypothesis?.Name ?? "diagnóstico em aberto"));
                }
                DiseaseDefinition hypothesis = orders.WorkerDiagnosis.Hypothesis;
                if (!orders.IsRecovered && !orders.IsCareProtected && hypothesis != null) ApplyPlan(task, hypothesis, now);
                if (orders.IsCareProtected)
                {
                    if (!orders.IsRecovered && patient.Location == PatientLocation.InpatientBed) OrderExams(task, hypothesis ?? patient.Disease);
                    return;
                }
                if (orders.IsRecovered)
                {
                    discharge(patient, task.Author, orders.CareRevision);
                    return;
                }
                if (patient.Location == PatientLocation.Chair)
                {
                    if (hypothesis != null && !hypothesis.RequiresSpecificTreatment && patient.RiskLevel <= 2)
                    {
                        discharge(patient, task.Author, orders.CareRevision);
                        return;
                    }
                    InpatientUnit preferred = Coverage(1, now) > Coverage(2, now) ? InpatientUnit.Room : InpatientUnit.Ward;
                    InpatientUnit fallback = preferred == InpatientUnit.Room ? InpatientUnit.Ward : InpatientUnit.Room;
                    if (admit(patient, preferred, true) || admit(patient, fallback, true)) patient.AwaitingBed = false;
                    else
                    {
                        patient.AwaitingBed = true;
                        if (patient.RiskLevel <= 3) returnToWaiting(patient);
                    }
                }
                else OrderExams(task, hypothesis ?? patient.Disease);
            }

            private void ApplyPlan(Assignment task, DiseaseDefinition hypothesis, long now)
            {
                ExamOrders orders = task.Patient.Orders;
                MedicationEffect medication = hypothesis.MedicationEffects.OrderByDescending(effect => effect.ImprovementChance).FirstOrDefault();
                foreach (MedicationClass active in orders.ActiveMedications(now))
                    if (medication == null || medication.Medication != active)
                        orders.StopMedication(active, task.Author, now, orders.CareRevision);
                if (medication != null && !orders.ActiveMedications(now).Contains(medication.Medication))
                    orders.PrescribeMedication(medication.Medication, now + GameClock.Minutes(8 * 60), task.Author, now, orders.CareRevision);
                if (hypothesis.RequiresSpecificTreatment)
                {
                    if (orders.SelectTreatment(hypothesis.Id, task.Author, now, orders.CareRevision)) task.Patient.NextNurseVisitAt = now;
                }
                else orders.ClearTreatment(task.Author, now, orders.CareRevision);
            }

            private void OrderExams(Assignment task, DiseaseDefinition hypothesis)
            {
                ExamOrders orders = task.Patient.Orders;
                if (orders.WorkerDiagnosis.HasConfirmingExam) return;
                const int maxParallelExams = 4;
                int pending = orders.Items.Count(exam => exam.Status == ExamStatus.Pending ||
                    exam.Status == ExamStatus.InProgress || exam.Status == ExamStatus.Processing);
                foreach (ExamOrder exam in orders.Items.Where(exam => exam.Status == ExamStatus.Available &&
                    rules.ExamPriority(hypothesis, exam.Id, task.Patient.SeverityIndex + 1) > 0)
                    .OrderByDescending(exam => rules.ExamPriority(hypothesis, exam.Id, task.Patient.SeverityIndex + 1))
                    .ThenBy(exam => exam.DurationMilliseconds).Take(Math.Max(0, maxParallelExams - pending)))
                    request(task.Patient, exam, task.Author);
            }

            private void Sit(Assignment task)
            {
                var room = DiagnosticRooms[task.Room];
                Ped doctor = task.Doctor.Actor.Actor;
                HospitalReceptionist.LoadAnimation(HospitalComputer.ComputerAnimation);
                NativeFunction.Natives.SET_ENTITY_COLLISION(doctor, false, true);
                HospitalReceptionist.PlaceAt(doctor, room.DoctorSeat, room.DoctorHeading, HospitalComputer.ComputerAnimation, HospitalComputer.ComputerClip);
                doctor.IsPositionFrozen = true;
                task.Seated = true;
            }

            private void Release(Assignment task)
            {
                reservations.Release(task.Author.Id);
                assignments.Remove(task);
                if (task.Room >= 0 && task.Patient.Location == PatientLocation.Chair)
                    task.Patient.WorkerConsultation = false;
                task.Walk.Clear();
                if (!task.Doctor.Actor.IsAlive) return;
                Point origin = task.Walk.LastGraphPoint ?? task.Destination;
                try
                {
                    Ped doctor = task.Doctor.Actor.Actor;
                    doctor.IsPositionFrozen = false;
                    NativeFunction.Natives.SET_ENTITY_COLLISION(doctor, true, true);
                    doctor.Tasks.Clear();
                    if (task.Seated) NpcWalking.TeleportToWaypoint(doctor, computers[task.Room].Position);
                    origin = path.FindNearestWaypoint(doctor.Position) ?? origin;
                }
                catch (InvalidOperationException error) { Game.LogTrivial("[Lumen Santé] Falha ao liberar Doctor: " + error.Message); }
                finally { task.Doctor.ResumeAfterExam(origin); }
            }

            internal void Clear()
            {
                foreach (Assignment task in assignments.ToArray()) Release(task);
                authors.Clear();
            }
        }
    }
}
