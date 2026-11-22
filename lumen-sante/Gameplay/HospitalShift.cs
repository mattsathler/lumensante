using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using lumen_sante.Clinical;
using lumen_sante.Interface;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal static class HospitalShift
    {
        private static readonly Vector3 ShiftPoint = new Vector3(354.0f, -1397.0f, 32.0f);
        internal static readonly (Vector3 Position, float Heading) ConsultationRouteEntrance =
            (new Vector3(349.914f, -1403.067f, 32.505f), 230.4f);
        internal static readonly (Vector3 Position, float Heading) DiagnosticOneCorridor =
            (new Vector3(364.009f, -1414.967f, 32.505f), 298.2f);
        internal static readonly (Vector3 Position, float Heading) DiagnosticTwoCorridor =
            (new Vector3(368.086f, -1418.346f, 32.505f), 232.0f);
        private static readonly (string Name, Vector3 DoctorSeat, float DoctorHeading, Vector3 DoctorInteraction,
            Vector3 PatientSeat, float PatientHeading, Vector3 Table, float TableHeading, Vector3 TableExit,
            (Vector3 Position, float Heading)[] Route)[] DiagnosticRooms =
        {
            ("Sala Diagnóstico II", new Vector3(364.570f, -1419.356f, 32.00f), 46.2f,
                new Vector3(364.03f, -1419.77f, 32.51f), new Vector3(362.636f, -1418.714f, 31.955f), 251.8f,
                new Vector3(362.067f, -1421.484f, 33.221f), 51.9f, new Vector3(362.25f, -1420.60f, 32.51f),
                new[] { ConsultationRouteEntrance, DiagnosticOneCorridor,
                    (new Vector3(361.593f, -1418.238f, 32.505f), 246.2f) }),
            ("Sala Diagnóstico III", new Vector3(368.598f, -1422.852f, 32.055f), 41.1f,
                new Vector3(368.546f, -1422.840f, 32.505f), new Vector3(366.862f, -1422.129f, 31.955f), 253.4f,
                new Vector3(363.155f, -1424.863f, 33.215f), 231.6f, new Vector3(363.880f, -1424.329f, 32.505f),
                new[] { ConsultationRouteEntrance, DiagnosticTwoCorridor,
                    (new Vector3(365.606f, -1421.805f, 32.505f), 219.9f) })
        };
        private const string InspectionAnimation = "anim@amb@casino@hangout@ped_male@stand@02b@base";
        private const string InspectionClip = "base";
        private const string VitalsAnimation = "anim@amb@business@bgen@bgen_inspecting@";
        private const string VitalsClip = "inspecting_med_lookingaround_inspector";
        private static readonly (string Name, Vector3 Bed, float Heading, Vector3 InteractionPoint)[] InpatientRooms =
        {
            ("Room 1", new Vector3(356.498f, -1417.208f, 37.415f), 45.9f, new Vector3(357f, -1416f, 36f)),
            ("Room 2", new Vector3(361.020f, -1421.058f, 37.464f), 48.3f, new Vector3(361.020f, -1421.058f, 36.05f)),
            ("Room 3", new Vector3(365.623f, -1424.951f, 37.417f), 55.5f, new Vector3(365.623f, -1424.951f, 36f)),
            ("Room 4", new Vector3(372.641f, -1395.098f, 37.468f), 234.3f, new Vector3(372.641f, -1395.098f, 36.05f))
        };
        private static readonly int[] InpatientBlipColors = { 3, 5, 7, 17 };
        private const float PatientInteractionDistanceSquared = 2.25f;
        internal const string PatientChairAnimation = "amb@prop_human_seat_chair@male@generic@base";
        internal const string PatientChairClip = "base";
        private const int PulseRefreshMilliseconds = 4000;

        private const float ShiftInteractionDistanceSquared = 1.25f * 1.25f;
        private const Keys InteractionKey = Keys.E;
        private const Keys PositionKey = Keys.F6;

        private enum PatientLocation
        {
            WalkingToConsultation,
            Chair,
            ExaminationTable,
            WalkingToInpatientBed,
            InpatientBed,
            UndergoingExam
        }

        private sealed class PatientCase
        {
            internal Ped Ped;
            internal Model Model;
            internal PatientAppearance.Snapshot Appearance;
            internal PatientEscort Escort;
            internal Point Pickup;
            internal long NextEscortAttemptAt;
            internal long NextNurseVisitAt;
            internal long NextRestoreAt;
            internal string Name;
            internal int DiagnosticRoomIndex;
            internal int InpatientRoomIndex = -1;
            internal Blip Blip;
            internal Vector3 InpatientInteractionPoint;
            internal PatientLocation Location;
            internal NpcRoute InpatientTransfer;
            internal long NextBedPoseAttempt;
            internal DiseaseDefinition Disease;
            internal ExamOrders Orders;
            internal int SeverityIndex;
            internal long NextPulseRefresh;
            internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation) Vitals;
            internal bool Deceased;
            internal bool CoronersCalled;
            internal bool SeverityClockRunning;
            internal long NextSeverityAt;
        }

        internal static void Run(HospitalStaffRoutes routes, WaitingRoom waitingRoom, AmbientPopulation population, Func<bool> consumeShiftToggle,
            Action<Ped, bool> updateAmbient)
        {
            Point[] tableOrigins = DiagnosticRooms.Select((room, index) => routes.AddDiagnosticTransferAccess(
                index, room.Name, room.Table, room.TableExit, room.TableHeading, room.Route[room.Route.Length - 1])).ToArray();
            Point[] inpatientRoomDestinations = InpatientRooms.Select((room, index) =>
                HospitalUpperFloor.AddInpatientRoomAccess(routes.Path, index)).ToArray();
            Point[] consultationChairs = DiagnosticRooms.Select((room, index) =>
                routes.AddConsultationChairAccess(index, room.PatientSeat, room.PatientHeading,
                    room.Route[room.Route.Length - 1])).ToArray();
            var menuPool = new MenuPool();
            var examClipboard = new ExamClipboard();
            var menu = new UIMenu("Lumen Santé", "Central Los Santos Medical Center");
            var shiftAction = new UIMenuItem("Iniciar plantão");
            var callPatient = new UIMenuItem("Chamar paciente") { Enabled = false };
            menu.AddItem(shiftAction);
            menu.AddItem(callPatient);
            var roomStatusItems = DiagnosticRooms.Select(room => new UIMenuItem(room.Name) { Enabled = false }).ToArray();
            foreach (UIMenuItem status in roomStatusItems)
            {
                menu.AddItem(status);
            }
            menuPool.Add(menu);
            bool interactionWasDown = false;
            bool autoStartPending = true;

            var random = new Random();
            var consultations = new PatientCase[DiagnosticRooms.Length];
            var inpatients = new PatientCase[InpatientRooms.Length];
            var cases = consultations.Concat(inpatients);
            var pendingExams = new Queue<(PatientCase Patient, ExamOrder Order)>();
            (PatientCase Patient, ExamOrder Order) activeExam = default((PatientCase, ExamOrder));
            var activeOrders = new List<ExamOrder>();
            ExamProcedure procedure = null;
            ExamProcedure nursingVisit = null;
            PatientCase visitedPatient = null;
            var emptyCase = new PatientCase();
            PatientCase current = emptyCase;
            PatientCase shownCase = null;
            Ped shiftPlayer = null;
            long shiftStartedAt = 0;
            long? clinicalPausedAt = null;
            int doctorRoomIndex = 0;
            Ped examiningDoctor = null;
            string activeExamAnimation = null;
            string activeExamClip = null;

            bool CanInteractWithPatient() =>
                shiftPlayer != null && current.Ped != null && current.Ped.Exists() && current.Ped.IsAlive && !current.Deceased;

            var patientMenu = new PatientMenu(menuPool, DiagnosticRooms[current.DiagnosticRoomIndex].Name, InpatientRooms.Select(room => room.Name).ToArray(),
                CanInteractWithPatient);
            var hospitalComputer = new HospitalComputer(menuPool, DiagnosticRooms[current.DiagnosticRoomIndex].Name, patientMenu,
                () => current.Location == PatientLocation.Chair && current.DiagnosticRoomIndex == doctorRoomIndex && CanInteractWithPatient(),
                () => consultations[doctorRoomIndex] != null, () => consultations[doctorRoomIndex]?.Name,
                CanCallPatient, () => CallPatientToRoom(doctorRoomIndex));

            bool CanCallPatient() =>
                shiftPlayer != null && waitingRoom.WaitingPatientCount > 0;

            bool IsNear(Ped doctor, Vector3 point) =>
                doctor.Exists() && doctor.IsAlive && doctor.IsOnFoot &&
                (doctor.Position - point).LengthSquared() <= PatientInteractionDistanceSquared;

            void UpdateDoctorAnimation(Ped doctor, string dictionary, string clip)
            {
                if (doctor == examiningDoctor && dictionary == activeExamAnimation)
                {
                    return;
                }

                if (examiningDoctor != null && examiningDoctor.Exists())
                {
                    NativeFunction.Natives.STOP_ANIM_TASK(examiningDoctor, activeExamAnimation, activeExamClip, 4f);
                }

                examiningDoctor = doctor;
                activeExamAnimation = dictionary;
                activeExamClip = clip;
                if (doctor == null)
                {
                    return;
                }

                try
                {
                    HospitalReceptionist.LoadAnimation(dictionary);
                    Vector3 target = current.Location == PatientLocation.InpatientBed
                        ? InpatientRooms[current.InpatientRoomIndex].Bed : DiagnosticRooms[current.DiagnosticRoomIndex].Table;
                    Vector3 access = current.Location == PatientLocation.InpatientBed
                        ? current.InpatientInteractionPoint : target;
                    if (!IsNear(doctor, access) || !CanInteractWithPatient())
                    {
                        return;
                    }

                    Vector3 towardPatient = target - doctor.Position;
                    doctor.Heading = (float)(Math.Atan2(-towardPatient.X, towardPatient.Y) * 180 / Math.PI + 360) % 360;
                    doctor.Tasks.PlayAnimation(dictionary, clip, 4f, AnimationFlags.Loop);
                }
                catch (InvalidOperationException error)
                {
                    Game.LogTrivial("[Lumen Santé] Falha na animação do exame: " + error.Message);
                }
            }

            void PlacePatient(Vector3 position, float heading, string dictionary, string clip)
            {
                HospitalReceptionist.LoadAnimation(dictionary);
                if (current.Ped == null || !current.Ped.Exists() || !current.Ped.IsAlive || current.Deceased)
                {
                    throw new InvalidOperationException("Paciente indisponível.");
                }

                current.Ped.Tasks.Clear();
                NativeFunction.Natives.SET_ENTITY_COLLISION(current.Ped, false, true);
                HospitalReceptionist.PlaceAt(current.Ped, position, heading, dictionary, clip);
                current.Ped.IsPositionFrozen = true;
            }

            void ReturnPatientToChair()
            {
                PlacePatient(DiagnosticRooms[current.DiagnosticRoomIndex].PatientSeat, DiagnosticRooms[current.DiagnosticRoomIndex].PatientHeading, PatientChairAnimation, PatientChairClip);
                current.Location = PatientLocation.Chair;
                if (shownCase == current)
                {
                    patientMenu.Close();
                }
            }

            void BindPatient(PatientCase selected)
            {
                if (shownCase == selected)
                {
                    return;
                }

                shownCase = selected;
                if (selected?.Orders != null)
                {
                    patientMenu.SetConsultationRoom(DiagnosticRooms[selected.DiagnosticRoomIndex].Name);
                    patientMenu.SetPatient(selected.Name, selected.Disease, random, selected.Orders);
                    patientMenu.UpdateVitals(selected.Disease.Stages[selected.SeverityIndex], selected.SeverityIndex, selected.Vitals);

                }
            }

            void RefreshPatientList() => hospitalComputer.SetPatients(inpatients
                .Where(admission => admission?.Orders?.IsInpatient == true)
                .Select(admission => new BedManagementPanel.PatientEntry(admission.Orders, admission.Ped, admission.Deceased))
                .ToArray());

            void BeginConsultation()
            {
                ReturnPatientToChair();
                current.Disease = DiseaseCatalog.Pick(random);
                current.SeverityIndex = 0;
                current.Vitals = current.Disease.Stages[current.SeverityIndex].SampleVitals(random);
                current.SeverityClockRunning = true;
                current.NextSeverityAt = GameClock.NowMilliseconds + random.Next(
                    current.Disease.MinimumStageDurationMilliseconds,
                    current.Disease.MaximumStageDurationMilliseconds + 1);
                current.NextPulseRefresh = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(PulseRefreshMilliseconds);
                current.Orders = new ExamOrders(current.Name, DiagnosticRooms[current.DiagnosticRoomIndex].Name, current.Disease.Id, new PatientProfile(random));
                if (hospitalComputer.IsOpen && doctorRoomIndex == current.DiagnosticRoomIndex)
                {
                    BindPatient(current);
                }
                Game.DisplayNotification(current.Name + " está na " + DiagnosticRooms[current.DiagnosticRoomIndex].Name + ".");
                Game.LogTrivial("[Lumen Santé] Paciente recebido: " + current.Name + ", " + DiagnosticRooms[current.DiagnosticRoomIndex].Name + ".");
            }

            callPatient.Activated += (sender, item) =>
            {
                Ped doctor = Game.LocalPlayer.Character;
                if (!CanCallPatient() || doctor != shiftPlayer || !doctor.Exists() || !doctor.IsAlive ||
                    !doctor.IsOnFoot || (doctor.Position - ShiftPoint).LengthSquared() > ShiftInteractionDistanceSquared)
                {
                    return;
                }

                int roomIndex = consultations[doctorRoomIndex] == null
                    ? doctorRoomIndex : Array.FindIndex(consultations, consultation => consultation == null);
                if (roomIndex < 0)
                {
                    Game.DisplayNotification("Todas as salas de diagnóstico estão ocupadas.");
                    return;
                }
                if (CallPatientToRoom(roomIndex))
                {
                    menu.Visible = false;
                }
            };

            bool CallPatientToRoom(int roomIndex)
            {
                AmbientPatrol nurse = population.FindNurse(HospitalStaffRoutes.Reception);
                if (nurse == null)
                {
                    Game.DisplayNotification("Aguarde um enfermeiro disponível para buscar o paciente.");
                    return false;
                }
                if (!CanCallPatient() || consultations[roomIndex] != null ||
                    !waitingRoom.TryCallPatient(out Ped called, out Point approach))
                {
                    return false;
                }

                current = new PatientCase
                {
                    Ped = called,
                    Model = called.Model,
                    Appearance = new PatientAppearance.Snapshot(called),
                    DiagnosticRoomIndex = roomIndex
                };
                consultations[roomIndex] = current;
                current.Name = PatientNames.Pick(random, !current.Ped.IsMale);
                current.Pickup = approach;
                current.Location = PatientLocation.WalkingToConsultation;
                try
                {
                    StartPatientEscort(nurse);
                }
                catch (InvalidOperationException error)
                {
                    Game.LogTrivial("[Lumen Santé] Busca do paciente aguardando nova tentativa: " + error.Message);
                }
                Game.DisplayNotification("Um enfermeiro buscará " + current.Name + " na recepção.");
                return true;
            }

            void StartPatientEscort(AmbientPatrol nurse)
            {
                current.NextEscortAttemptAt = GameClock.NowMilliseconds + GameClock.Minutes(5);
                current.Escort = new PatientEscort(routes.Path, nurse, current.Ped, current.Pickup,
                    consultationChairs[current.DiagnosticRoomIndex]);
            }

            void UpdateConsultationArrival()
            {
                if (current.Escort == null)
                {
                    if (GameClock.NowMilliseconds < current.NextEscortAttemptAt) return;
                    AmbientPatrol nurse = population.FindNurse(current.Pickup);
                    if (nurse == null) return;
                    StartPatientEscort(nurse);
                }
                if (current.Escort.Update())
                {
                    current.Escort = null;
                    BeginConsultation();
                }
            }
            patientMenu.LieDownRequested += () =>
            {
                if (!CanInteractWithPatient() || current.Location != PatientLocation.Chair || current.DiagnosticRoomIndex != doctorRoomIndex ||
                    !hospitalComputer.IsSeated(Game.LocalPlayer.Character))
                {
                    return;
                }

                try
                {
                    var room = DiagnosticRooms[current.DiagnosticRoomIndex];
                    var pose = BedAnimations.Lounger;
                    PlacePatient(room.Table, room.TableHeading, pose.Dictionary, pose.Clip);
                    current.Location = PatientLocation.ExaminationTable;
                    patientMenu.Close();
                    Game.DisplayNotification("O paciente está pronto para o exame físico.");
                }
                catch (InvalidOperationException error)
                {
                    Game.DisplayNotification(error.Message);
                }
            };

            patientMenu.ChairRequested += () =>
            {
                if (CanInteractWithPatient() && current.Location == PatientLocation.ExaminationTable &&
                    IsNear(Game.LocalPlayer.Character, DiagnosticRooms[current.DiagnosticRoomIndex].Table))
                {
                    try
                    {
                        ReturnPatientToChair();
                    }
                    catch (InvalidOperationException error)
                    {
                        Game.DisplayNotification(error.Message);
                    }
                }
            };

            patientMenu.AdmissionRequested += roomIndex =>
            {
                if (!CanInteractWithPatient() || current.Location != PatientLocation.ExaminationTable ||
                    !IsNear(Game.LocalPlayer.Character, DiagnosticRooms[current.DiagnosticRoomIndex].Table))
                {
                    return;
                }

                var room = InpatientRooms[roomIndex];
                if (inpatients[roomIndex] != null)
                {
                    Game.DisplayNotification(room.Name + " está ocupado.");
                    return;
                }
                Point origin = tableOrigins[current.DiagnosticRoomIndex];
                Point[] waypoints = routes.Path.FindRoute(origin, inpatientRoomDestinations[roomIndex]);
                if (waypoints == null || waypoints.Length == 0)
                {
                    Game.DisplayNotification("Não há rota disponível para " + room.Name + ".");
                    return;
                }

                current.Ped.Tasks.Clear();
                current.Ped.IsPositionFrozen = false;
                NativeFunction.Natives.SET_ENTITY_COLLISION(current.Ped, true, true);
                current.InpatientTransfer = new NpcRoute();
                current.InpatientTransfer.Start(current.Ped, origin, waypoints, teleportOnTimeout: false);
                current.InpatientRoomIndex = roomIndex;
                current.Location = PatientLocation.WalkingToInpatientBed;
                inpatients[roomIndex] = current;
                consultations[current.DiagnosticRoomIndex] = null;
                patientMenu.Close();
                Game.DisplayNotification(current.Name + " está a caminho do " + room.Name + ".");
            };

            void UpdateInpatientTransfer()
            {
                NpcRoute transfer = current.InpatientTransfer;
                if (GameClock.NowMilliseconds < current.NextBedPoseAttempt ||
                    (transfer.IsWalking && !transfer.Update(current.Ped)))
                {
                    return;
                }

                var room = InpatientRooms[current.InpatientRoomIndex];
                var pose = BedAnimations.Lounger;
                try
                {
                    PlacePatient(room.Bed, room.Heading, pose.Dictionary, pose.Clip);
                    current.InpatientInteractionPoint = room.InteractionPoint;
                    current.Location = PatientLocation.InpatientBed;
                    current.InpatientTransfer = null;
                    current.Orders.AdmitTo(room.Name);
                    RefreshPatientList();
                    Game.DisplayNotification(current.Name + " foi internado no " + room.Name + ".");
                    Game.LogTrivial("[Lumen Santé] Internado no " + room.Name + ": " + current.Name + ".");
                }
                catch (InvalidOperationException error)
                {
                    current.NextBedPoseAttempt = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(3000);
                    Game.LogTrivial("[Lumen Santé] Falha na animação do leito: " + error.Message);
                }
            }

            hospitalComputer.TreatmentSelected += (orders, diseaseId) =>
            {
                PatientCase admission = cases.FirstOrDefault(candidate => candidate?.Orders == orders);
                if (shiftPlayer == null || admission == null || admission.Deceased ||
                    !DiseaseCatalog.Treatments.Any(disease => disease.Id == diseaseId) || !orders.SelectTreatment(diseaseId))
                {
                    return;
                }
                admission.NextNurseVisitAt = GameClock.NowMilliseconds;
                string label = DiseaseCatalog.Treatments.First(disease => disease.Id == diseaseId).TreatmentLabel;
                Game.DisplayNotification(admission.Name + " — tratamento selecionado: " + label + ".");
                Game.LogTrivial("[Lumen Santé] Tratamento de " + admission.Name + ": " + label);
            };
            patientMenu.ExamRequested += order =>
            {
                Ped doctor = Game.LocalPlayer.Character;
                if (shiftPlayer == null || shiftPlayer != doctor || !doctor.Exists() || !doctor.IsAlive ||
                    current.Location != PatientLocation.InpatientBed || !IsNear(doctor, current.InpatientInteractionPoint) ||
                    !CanInteractWithPatient() || current.Orders == null)
                {
                    return;
                }

                int index = Array.IndexOf(current.Orders.Items, order);
                if (index >= 0 && order.TryRequest(current.SeverityIndex + 1))
                {
                    pendingExams.Enqueue((current, order));
                    patientMenu.UpdateExamStatus(index, order);
                    hospitalComputer.RefreshExams(current.Orders);
                    Game.LogTrivial("[Lumen Santé] Exame solicitado para " + current.Name + ": " + order.Id + ".");
                }
            };

            void RefreshExamStatus(PatientCase admission, ExamOrder order)
            {
                if (shownCase == admission)
                {
                    patientMenu.UpdateExamStatus(Array.IndexOf(admission.Orders.Items, order), order);
                }
                hospitalComputer.RefreshExams(admission.Orders);
            }

            void StartNextExam()
            {
                while (pendingExams.Count > 0)
                {
                    var request = pendingExams.Peek();
                    PatientCase admission = request.Patient;
                    if (admission == visitedPatient) return;
                    if (admission.Deceased || admission.Ped == null || !admission.Ped.Exists() ||
                        !admission.Ped.IsAlive || admission.Location != PatientLocation.InpatientBed)
                    {
                        pendingExams.Dequeue();
                        request.Order.Cancel();
                        RefreshExamStatus(admission, request.Order);
                        continue;
                    }

                    Point center = inpatientRoomDestinations[admission.InpatientRoomIndex];
                    AmbientPatrol nurse = population.FindNurse(center);
                    if (nurse == null)
                    {
                        return;
                    }

                    pendingExams.Dequeue();
                    var room = InpatientRooms[admission.InpatientRoomIndex];
                    try
                    {
                        procedure = new ExamProcedure(routes, nurse, admission.Ped, center, room.Bed, room.Heading, request.Order);
                        activeExam = request;
                        activeOrders.Add(request.Order);
                        GroupPendingExams();
                        Game.LogTrivial("[Lumen Santé] Enfermeira a caminho de " + room.Name + ": " + request.Order.Name + ".");
                    }
                    catch (InvalidOperationException error)
                    {
                        request.Order.Cancel();
                        RefreshExamStatus(admission, request.Order);
                        Game.DisplayNotification("O exame não pôde ser iniciado.");
                        Game.LogTrivial("[Lumen Santé] Falha ao iniciar exame: " + error.Message);
                    }
                    return;
                }
            }

            void GroupPendingExams()
            {
                ExamKind kind = activeExam.Order.Kind;
                if (!procedure.CanGroupOrders || (kind != ExamKind.Mri && kind != ExamKind.Laboratory)) return;
                int count = pendingExams.Count;
                for (int index = 0; index < count; index++)
                {
                    var request = pendingExams.Dequeue();
                    if (request.Patient == activeExam.Patient && request.Order.Kind == kind &&
                        request.Order.Status == ExamStatus.Pending)
                    {
                        activeOrders.Add(request.Order);
                        procedure.IncludeDuration(request.Order.DurationMilliseconds);
                        Game.LogTrivial("[Lumen Santé] Exame agrupado para " + request.Patient.Name + ": " + request.Order.Name);
                    }
                    else
                    {
                        pendingExams.Enqueue(request);
                    }
                }
            }

            void UpdateExamWork()
            {
                if (procedure == null)
                {
                    StartNextExam();
                    return;
                }

                ExamProcedure.Result result;
                GroupPendingExams();
                try
                {
                    result = procedure.Update();
                }
                catch (InvalidOperationException error)
                {
                    Game.LogTrivial("[Lumen Santé] Falha durante o exame: " + error.Message);
                    procedure.Cancel();
                    result = ExamProcedure.Result.Running;
                }

                PatientCase admission = activeExam.Patient;
                if (!admission.Deceased && admission.Ped != null && admission.Ped.Exists())
                {
                    admission.Location = procedure.PatientAway ? PatientLocation.UndergoingExam : PatientLocation.InpatientBed;
                }
                foreach (ExamOrder order in activeOrders)
                {
                    if (order.TrySetStatus(procedure.OrderStatus))
                    {
                        RefreshExamStatus(admission, order);
                    }
                }
                if (result == ExamProcedure.Result.Running)
                {
                    return;
                }

                long completedAt = GameClock.NowMilliseconds - shiftStartedAt;
                foreach (ExamOrder order in activeOrders)
                {
                    if (result == ExamProcedure.Result.Completed && order.TryComplete(completedAt))
                    {
                        Game.LogTrivial("[Lumen Santé] Exame realizado para " + admission.Name + ": " + order.Id + ".");
                    }
                    else
                    {
                        order.Cancel();
                    }
                    RefreshExamStatus(admission, order);
                }
                activeOrders.Clear();
                procedure = null;
                activeExam = default((PatientCase, ExamOrder));
            }

            var deceasedMenu = new UIMenu("Paciente", "Central Los Santos Medical Center");
            var coronersAction = new UIMenuItem("Chamar os Coroners");
            deceasedMenu.AddItem(coronersAction);
            menuPool.Add(deceasedMenu);

            coronersAction.Activated += (sender, item) =>
            {
                if (!coronersAction.Enabled || !current.Deceased || current.Ped == null || !current.Ped.Exists())
                {
                    return;
                }

                coronersAction.Enabled = false;
                coronersAction.Text = "Coroners chamados";
                current.CoronersCalled = true;
                string location = current.Orders?.Room ?? "Recepção";
                Game.DisplayNotification("Coroners avisados para " + location + ".");
                Game.LogTrivial("[Lumen Santé] Coroners chamados: " + current.Name + ", " + location + ".");
                // TODO: Enviar a equipe de NPCs para recolher o corpo e liberar o leito.
            };

            void RegisterDeath()
            {
                current.Escort?.Release();
                current.Escort = null;
                current.SeverityClockRunning = false;
                current.Deceased = true;
                current.Ped.IsPositionFrozen = false;
                NativeFunction.Natives.SET_ENTITY_COLLISION(current.Ped, true, true);
                if (shownCase == current)
                {
                    patientMenu.Close();
                }
                if (current.InpatientRoomIndex >= 0)
                {
                    RefreshPatientList();
                }
                string location = current.Orders?.Room ?? "Recepção";
                Game.DisplayNotification(current.Name + " veio a óbito em " + location + ".");
                Game.LogTrivial("[Lumen Santé] Óbito: " + current.Name + ", " + location + ".");
            }

            void UpdatePatientBlip()
            {
                if (current.InpatientRoomIndex < 0 || current.Orders?.IsInpatient != true) return;
                var room = InpatientRooms[current.InpatientRoomIndex];
                Vector3 position = current.Ped != null && current.Ped.Exists() ? current.Ped.Position : room.Bed;
                if (current.Blip == null || !current.Blip.Exists())
                {
                    current.Blip = new Blip(position);
                    current.Blip.Name = current.Name + " — " + room.Name;
                    NativeFunction.Natives.SET_BLIP_SPRITE(current.Blip, 1);
                    NativeFunction.Natives.SET_BLIP_COLOUR(current.Blip, InpatientBlipColors[current.InpatientRoomIndex]);
                }
                NativeFunction.Natives.SET_BLIP_COORDS(current.Blip, position.X, position.Y, position.Z);
            }

            void ClearAdmission()
            {
                if (current.Blip != null && current.Blip.Exists()) current.Blip.Delete();
                current.Blip = null;
                current.Escort?.Release();
                current.Escort = null;
                if (shownCase == current)
                {
                    BindPatient(null);
                    patientMenu.Close();
                    deceasedMenu.Visible = false;
                }

                if (current.InpatientRoomIndex >= 0)
                {
                    inpatients[current.InpatientRoomIndex] = null;
                    RefreshPatientList();
                }
                else if (consultations[current.DiagnosticRoomIndex] == current)
                {
                    consultations[current.DiagnosticRoomIndex] = null;
                }

                if (current.Ped != null && current.Ped.Exists())
                {
                    current.Ped.Delete();
                }

                current.Ped = null;
            }

            void UpdateTreatmentVisits()
            {
                if (nursingVisit != null)
                {
                    if (visitedPatient.Deceased || visitedPatient.Ped == null || !visitedPatient.Ped.Exists() ||
                        nursingVisit.Update() != ExamProcedure.Result.Running)
                    {
                        nursingVisit.ReleaseNurse();
                        nursingVisit = null;
                        visitedPatient.NextNurseVisitAt = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(30, 61));
                        visitedPatient = null;
                    }
                    return;
                }
                PatientCase patient = inpatients.FirstOrDefault(admission => admission != null && !admission.Deceased &&
                    admission.Orders.TreatmentDiseaseId != null && admission.Ped != null && admission.Ped.Exists() &&
                    admission.Ped.IsAlive && admission.Location == PatientLocation.InpatientBed &&
                    admission != activeExam.Patient && GameClock.NowMilliseconds >= admission.NextNurseVisitAt);
                if (patient == null) return;
                Point center = inpatientRoomDestinations[patient.InpatientRoomIndex];
                AmbientPatrol nurse = population.FindNurse(center);
                if (nurse == null) return;
                var room = InpatientRooms[patient.InpatientRoomIndex];
                try
                {
                    nursingVisit = new ExamProcedure(routes, nurse, patient.Ped, center, room.Bed, room.Heading,
                        new ExamOrder("treatment-visit", "Visita de enfermagem", 5 * 60 * 1000, ExamKind.Bedside));
                    visitedPatient = patient;
                }
                catch (InvalidOperationException error)
                {
                    patient.NextNurseVisitAt = GameClock.NowMilliseconds + GameClock.Minutes(5);
                    Game.LogTrivial("[Lumen Santé] Falha na visita de enfermagem: " + error.Message);
                }
            }

            void ClearAllAdmissions()
            {
                nursingVisit?.ReleaseNurse();
                nursingVisit = null;
                procedure?.ReleaseNurse();
                procedure = null;
                activeExam = default((PatientCase, ExamOrder));
                pendingExams.Clear();
                foreach (ExamOrder order in activeOrders) order.Cancel();
                activeOrders.Clear();
                foreach (PatientCase admission in cases)
                {
                    if (admission != null)
                    {
                        current = admission;
                        ClearAdmission();
                    }
                }

                current = emptyCase;
            }

            void PauseClinicalClock()
            {
                clinicalPausedAt = GameClock.NowMilliseconds;
                hospitalComputer.Close();
                patientMenu.Close();
                deceasedMenu.Visible = false;
            }

            void RestorePatient()
            {
                if (GameClock.NowMilliseconds < current.NextRestoreAt)
                {
                    return;
                }
                current.NextRestoreAt = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(5000);
                Ped replacement = null;
                try
                {
                    bool inpatient = current.InpatientRoomIndex >= 0;
                    Vector3 position = inpatient ? InpatientRooms[current.InpatientRoomIndex].Bed
                        : DiagnosticRooms[current.DiagnosticRoomIndex].PatientSeat;
                    float heading = inpatient ? InpatientRooms[current.InpatientRoomIndex].Heading
                        : DiagnosticRooms[current.DiagnosticRoomIndex].PatientHeading;
                    var pose = inpatient ? BedAnimations.Lounger
                        : (Name: "Cadeira", Dictionary: PatientChairAnimation, Clip: PatientChairClip);
                    HospitalReceptionist.LoadAnimation(pose.Dictionary);
                    replacement = new Ped(current.Model, position, heading);
                    current.Escort?.Release();
                    current.Escort = null;
                    current.Appearance?.Apply(replacement);
                    replacement.IsPersistent = true;
                    NativeFunction.Natives.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(replacement, true);
                    NativeFunction.Natives.SET_ENTITY_COLLISION(replacement, false, true);
                    HospitalReceptionist.PlaceAt(replacement, position, heading, pose.Dictionary, pose.Clip);
                    replacement.IsPositionFrozen = true;
                    current.Ped = replacement;
                    current.InpatientTransfer = null;
                    current.Location = inpatient ? PatientLocation.InpatientBed : PatientLocation.Chair;
                    if (inpatient)
                    {
                        var room = InpatientRooms[current.InpatientRoomIndex];
                        current.InpatientInteractionPoint = room.InteractionPoint;
                        current.Orders.AdmitTo(room.Name);
                        RefreshPatientList();
                    }
                    if (current.Deceased)
                    {
                        replacement.IsPositionFrozen = false;
                        replacement.Health = 0;
                    }
                    else if (current.Disease == null)
                    {
                        BeginConsultation();
                    }
                    Game.LogTrivial("[Lumen Santé] NPC do paciente restaurado: " + current.Name);
                }
                catch (InvalidOperationException error)
                {
                    if (replacement != null && replacement.Exists()) replacement.Delete();
                    current.Ped = null;
                    Game.LogTrivial("[Lumen Santé] Falha ao restaurar paciente: " + error.Message);
                }
            }

            void ToggleShift(Ped player)
            {
                bool starting = shiftPlayer == null;
                if (starting && (!player.Exists() || !player.IsAlive))
                {
                    return;
                }

                shiftPlayer = starting ? player : null;
                if (starting)
                {
                    shiftStartedAt = GameClock.NowMilliseconds;
                    if (clinicalPausedAt.HasValue)
                    {
                        long paused = GameClock.NowMilliseconds - clinicalPausedAt.Value;
                        foreach (PatientCase admission in cases.Where(admission => admission != null))
                        {
                            admission.NextSeverityAt += paused;
                            admission.NextPulseRefresh += paused;
                        }
                        clinicalPausedAt = null;
                    }
                }
                else
                {
                    PauseClinicalClock();
                }

                shiftAction.Text = starting ? "Encerrar plantão" : "Iniciar plantão";
                string message = starting ? "Plantão iniciado." : "Plantão encerrado.";
                Game.DisplayNotification(message);
                Game.LogTrivial("[Lumen Santé] " + message);
                menu.Visible = false;
            }

            shiftAction.Activated += (sender, item) => ToggleShift(Game.LocalPlayer.Character);

            var shiftBlip = new Blip(ShiftPoint);
            try
            {
                shiftBlip.Name = "Lumen Santé - Plantão";
                shiftBlip.Sprite = BlipSprite.Hospital;

                while (true)
                {
                    GameFiber.Yield();
                    GameClock.Update();
                    if (Game.IsPaused)
                    {
                        continue;
                    }

                    Ped player = Game.LocalPlayer.Character;
                    bool playerAvailable = player.Exists() && player.IsAlive;
                    updateAmbient(player, playerAvailable);
                    if (playerAvailable && Game.IsKeyDown(PositionKey))
                    {
                        Vector3 position = player.Position;
                        string coordinates = string.Format(CultureInfo.InvariantCulture,
                            "X: {0:F2}  Y: {1:F2}  Z: {2:F2}  H: {3:F1}°", position.X, position.Y, position.Z, player.Heading);
                        Game.DisplayNotification(coordinates);
                        Game.LogTrivial("[Lumen Santé] " + coordinates);
                    }

                    if (shiftPlayer != null && (!playerAvailable || player != shiftPlayer))
                    {
                        hospitalComputer.Close();
                        shiftPlayer = null;
                        PauseClinicalClock();
                        shiftAction.Text = "Iniciar plantão";
                        menu.Visible = false;
                        Game.LogTrivial("[Lumen Santé] Plantão interrompido.");
                    }

                    bool manualToggle = consumeShiftToggle();
                    if (autoStartPending && playerAvailable)
                    {
                        autoStartPending = false;
                        ToggleShift(player);
                    }
                    else if (manualToggle)
                    {
                        ToggleShift(player);
                    }

                    PatientCase interactionCase = current;
                    foreach (PatientCase admission in cases)
                    {
                        if (admission == null)
                        {
                            continue;
                        }

                        current = admission;

                        UpdatePatientBlip();
                        if (current.Ped == null || !current.Ped.Exists())
                        {
                            current.Escort?.Release();
                            current.Escort = null;
                            if (playerAvailable && (player.Position - ShiftPoint).LengthSquared() < 100f * 100f)
                            {
                                if (activeExam.Patient == current)
                                {
                                    procedure?.ReleaseNurse();
                                    procedure = null;
                                    foreach (ExamOrder order in activeOrders)
                                    {
                                        order.Cancel();
                                        RefreshExamStatus(current, order);
                                    }
                                    activeOrders.Clear();
                                    activeExam = default((PatientCase, ExamOrder));
                                }
                                RestorePatient();
                            }
                            continue;
                        }

                        if (current.Ped != null && !current.Deceased && !current.Ped.IsAlive)
                        {
                            RegisterDeath();
                        }

                        bool walkingToConsultation = current.Location == PatientLocation.WalkingToConsultation;
                        if (walkingToConsultation && current.Ped.IsAlive && !current.Deceased)
                        {
                            try
                            {
                                UpdateConsultationArrival();
                            }
                            catch (InvalidOperationException error)
                            {
                                current.Pickup = current.Escort?.RetryPickup ?? current.Pickup;
                                current.Escort?.Release();
                                current.Escort = null;
                                current.NextEscortAttemptAt = GameClock.NowMilliseconds + GameClock.Minutes(5);
                                Game.DisplayNotification(error.Message);
                                Game.LogTrivial("[Lumen Santé] Falha na chegada ao consultório: " + error.Message);
                            }
                        }


                        if (current.Location == PatientLocation.WalkingToInpatientBed && CanInteractWithPatient())
                        {
                            UpdateInpatientTransfer();
                        }

                        if (shiftPlayer != null && current.SeverityClockRunning)
                        {
                            long now = GameClock.NowMilliseconds;
                            bool stageChanged = now >= current.NextSeverityAt;
                            if (stageChanged || now >= current.NextPulseRefresh)
                            {
                                if (stageChanged)
                                {
                                    int previousSeverity = current.SeverityIndex;
                                    var chances = current.Disease.Chances(current.SeverityIndex,
                                        current.Orders.TreatmentDiseaseId == current.Disease.Id);
                                    double roll = random.NextDouble();
                                    if (roll < chances.Improvement)
                                    {
                                        current.SeverityIndex = Math.Max(0, current.SeverityIndex - 1);
                                    }
                                    else if (roll < chances.Improvement + chances.Worsening)
                                    {
                                        current.SeverityIndex = Math.Min(current.Disease.TerminalIndex, current.SeverityIndex + 1);
                                    }
                                    if (previousSeverity != current.SeverityIndex && current.SeverityIndex < current.Disease.TerminalIndex)
                                    {
                                        string change = current.SeverityIndex < previousSeverity ? "melhorou" : "piorou";
                                        Game.DisplayNotification(current.Name + " " + change + ".");
                                        Game.LogTrivial("[Lumen Santé] " + current.Name + " " + change + ".");
                                    }
                                    current.NextSeverityAt = now + random.Next(
                                        current.Disease.MinimumStageDurationMilliseconds,
                                        current.Disease.MaximumStageDurationMilliseconds + 1);
                                }
                                DiseaseStage stage = current.Disease.Stages[current.SeverityIndex];
                                current.NextPulseRefresh = now + GameClock.FromRealMilliseconds(PulseRefreshMilliseconds);
                                if (stageChanged)
                                {
                                    current.Vitals = stage.SampleVitals(random);
                                }
                                else
                                {
                                    current.Vitals.Pulse = stage.SamplePulse(random);
                                }
                                if (shownCase == current)
                                {
                                    patientMenu.UpdateVitals(stage, current.SeverityIndex, current.Vitals);
                                }

                                if (current.SeverityIndex == current.Disease.TerminalIndex)
                                {
                                    current.Ped.Health = 0;
                                    RegisterDeath();
                                }
                            }
                        }

                    }
                    current = hospitalComputer.IsOpen
                        ? consultations[doctorRoomIndex] ?? emptyCase : interactionCase;
                    if (shiftPlayer != null)
                    {
                        UpdateExamWork();
                    }
                    waitingRoom.Update(playerAvailable);
                    try
                    {
                        UpdateTreatmentVisits();
                    }
                    catch (InvalidOperationException error)
                    {
                        nursingVisit?.ReleaseNurse();
                        nursingVisit = null;
                        if (visitedPatient != null)
                        {
                            visitedPatient.NextNurseVisitAt = GameClock.NowMilliseconds + GameClock.Minutes(5);
                            visitedPatient = null;
                        }
                        Game.LogTrivial("[Lumen Santé] Visita de enfermagem interrompida: " + error.Message);
                    }

                    bool onFoot = playerAvailable && player.IsOnFoot;
                    bool atReception = onFoot &&
                        (player.Position - ShiftPoint).LengthSquared() <= ShiftInteractionDistanceSquared;
                    PatientCase nearbyCase = null;
                    float closestDistance = PatientInteractionDistanceSquared;
                    if (onFoot && shiftPlayer != null)
                    {
                        foreach (PatientCase admission in cases)
                        {
                            if (admission?.Ped == null || !admission.Ped.Exists())
                            {
                                continue;
                            }

                            Vector3 point;
                            if (admission.Deceased)
                            {
                                point = admission.Ped.Position;
                            }
                            else if (admission.Location == PatientLocation.ExaminationTable)
                            {
                                point = DiagnosticRooms[admission.DiagnosticRoomIndex].Table;
                            }
                            else if (admission.Location == PatientLocation.InpatientBed)
                            {
                                point = admission.InpatientInteractionPoint;
                            }
                            else
                            {
                                continue;
                            }

                            float distance = (player.Position - point).LengthSquared();
                            if (distance <= closestDistance)
                            {
                                closestDistance = distance;
                                nearbyCase = admission;
                            }
                        }
                    }

                    bool atDeceasedPatient = nearbyCase?.Deceased == true;
                    bool atTable = !atDeceasedPatient && nearbyCase?.Location == PatientLocation.ExaminationTable;
                    bool atBed = !atDeceasedPatient && nearbyCase?.Location == PatientLocation.InpatientBed;
                    int nearbyComputer = onFoot && shiftPlayer != null
                        ? Array.FindIndex(DiagnosticRooms, room => IsNear(player, room.DoctorInteraction)) : -1;
                    bool atComputer = nearbyComputer >= 0;
                    bool interactionDown = Game.IsKeyDown(InteractionKey);
                    bool interactionPressed = interactionDown && !interactionWasDown;
                    interactionWasDown = interactionDown;

                    if (!atReception)
                    {
                        menu.Visible = false;
                    }
                    callPatient.Enabled = CanCallPatient();
                    if (menu.Visible || atReception)
                    {
                        for (int index = 0; index < roomStatusItems.Length; index++)
                        {
                            roomStatusItems[index].RightLabel = consultations[index] == null ? "Livre" : "Ocupada";
                        }
                    }

                    if (!atDeceasedPatient || current != nearbyCase)
                    {
                        deceasedMenu.Visible = false;
                    }

                    if (!hospitalComputer.IsOpen && (current != nearbyCase || (!atTable && !atBed)))
                    {
                        patientMenu.Close();
                    }

                    if (!menu.Visible && !patientMenu.IsOpen && !deceasedMenu.Visible && !hospitalComputer.IsOpen &&
                        (atReception || atDeceasedPatient || atComputer || atTable || atBed))
                    {
                        string hint = atDeceasedPatient || atTable || atBed ? $"Pressione E para interagir com {nearbyCase.Name}."
                            : atComputer ? "Pressione E para sentar na cadeira."
                            : "Pressione E para abrir o menu de plantão.";
                        Game.DisplayHelp(hint, 1);
                        if (interactionPressed)
                        {
                            if (atDeceasedPatient)
                            {
                                current = nearbyCase;
                                BindPatient(current);
                                deceasedMenu.TitleText = current.Name;
                                deceasedMenu.SubtitleText = current.Orders?.Room ?? "Recepção";
                                coronersAction.Enabled = !current.CoronersCalled;
                                coronersAction.Text = current.CoronersCalled ? "Coroners chamados" : "Chamar os Coroners";
                                deceasedMenu.Visible = true;
                            }
                            else if (atTable)
                            {
                                current = nearbyCase;
                                BindPatient(current);
                                patientMenu.OpenExamination();
                            }
                            else if (atBed)
                            {
                                current = nearbyCase;
                                BindPatient(current);
                                patientMenu.OpenInpatient();
                            }
                            else if (atComputer)
                            {
                                var room = DiagnosticRooms[nearbyComputer];
                                hospitalComputer.Sit(player, room.Name, room.DoctorInteraction, room.DoctorSeat, room.DoctorHeading);
                                if (hospitalComputer.IsSeated(player))
                                {
                                    doctorRoomIndex = nearbyComputer;
                                    current = consultations[doctorRoomIndex] ?? emptyCase;
                                    if (current.Orders != null)
                                    {
                                        BindPatient(current);
                                    }
                                }
                            }
                            else
                            {
                                menu.Visible = true;
                            }
                        }
                    }

                    menuPool.ProcessMenus();
                    hospitalComputer.Update(player, playerAvailable);
                    if ((atTable || atBed) && current == nearbyCase && patientMenu.IsInspecting)
                    {
                        UpdateDoctorAnimation(player, InspectionAnimation, InspectionClip);
                    }
                    else if ((atTable || atBed) && current == nearbyCase && patientMenu.IsCheckingVitals)
                    {
                        UpdateDoctorAnimation(player, VitalsAnimation, VitalsClip);
                    }
                    else
                    {
                        UpdateDoctorAnimation(null, null, null);
                    }
                    examClipboard.Update(player, playerAvailable && shiftPlayer != null && patientMenu.IsChoosingExams);
                }
            }
            finally
            {
                examClipboard.Clear();
                UpdateDoctorAnimation(null, null, null);
                menu.Visible = false;
                hospitalComputer.Close();
                ClearAllAdmissions();
                if (shiftBlip.Exists())
                {
                    shiftBlip.Delete();
                }
            }
        }
    }
}
