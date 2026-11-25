using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    internal static partial class HospitalShift
    {
        private static readonly Vector3 ShiftPoint = new Vector3(354.0f, -1397.0f, 32.0f);
        internal static readonly (Vector3 Position, float Heading) ConsultationRouteEntrance =
            (new Vector3(349.914f, -1403.067f, 32.505f), 230.4f);
        internal static readonly (Vector3 Position, float Heading) DiagnosticOneCorridor =
            (new Vector3(364.000f, -1414.226f, 32.505f), 134.5f);
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
        private static readonly (string Name, Vector3 Bed, float Heading, Vector3 InteractionPoint, InpatientUnit Unit)[] InpatientRooms =
        {
            ("Quarto 1", new Vector3(356.498f, -1417.208f, 37.415f), 45.9f, new Vector3(357f, -1416f, 36f), InpatientUnit.Room),
            ("Quarto 2", new Vector3(361.020f, -1421.058f, 37.464f), 48.3f, new Vector3(361.020f, -1421.058f, 36.05f), InpatientUnit.Room),
            ("Quarto 3", new Vector3(365.623f, -1424.951f, 37.417f), 55.5f, new Vector3(365.623f, -1424.951f, 36f), InpatientUnit.Room),
            ("Quarto 4", new Vector3(372.641f, -1395.098f, 37.468f), 234.3f, new Vector3(372.641f, -1395.098f, 36.05f), InpatientUnit.Room),
            WardBed("I", new Vector3(361.425f, -1407.225f, 33.438f), 317.7f),
            WardBed("II", new Vector3(366.635f, -1411.460f, 33.403f), 323.5f),
            WardBed("III", new Vector3(369.167f, -1413.594f, 33.453f), 324.7f),
            WardBed("IV", new Vector3(372.657f, -1409.277f, 33.453f), 134.1f),
            WardBed("V", new Vector3(370.085f, -1407.032f, 33.453f), 136.0f),
            WardBed("VI", new Vector3(367.524f, -1405.049f, 33.453f), 137.6f),
            WardBed("VII", new Vector3(365.114f, -1402.882f, 33.453f), 140.5f)
        };
        private static readonly int[] InpatientBlipColors = { 3, 5, 7, 17, 1, 2, 18, 19, 27, 46, 52 };

        private static (string Name, Vector3 Bed, float Heading, Vector3 InteractionPoint, InpatientUnit Unit)
            WardBed(string number, Vector3 position, float heading)
        {
            double radians = heading * Math.PI / 180;
            var approach = new Vector3(position.X - (float)Math.Sin(radians),
                position.Y + (float)Math.Cos(radians), 32.505f);
            return ("Enfermaria " + number, position, heading, approach, InpatientUnit.Ward);
        }
        private const float PatientInteractionDistanceSquared = 2.25f;
        internal const string PatientChairAnimation = "amb@prop_human_seat_chair@male@generic@base";
        internal const string PatientChairClip = "base";
        private const int PulseRefreshMilliseconds = 4000;

        private const float ShiftInteractionDistanceSquared = 1.25f * 1.25f;
        private const Keys InteractionKey = Keys.E;
        private const Keys PositionKey = Keys.F6;

        private enum PatientLocation
        {
            WaitingReception,
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
            internal NpcRoute ConsultationChairWalk;
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
            internal NpcRoute DischargeWalk;
            internal bool OutsideHospital;
            internal bool AwaitingReturn;
            internal long ReturnAt;
            internal bool SpontaneousReturn;
            internal Guid AgendaOwner;
            internal bool AgendaRecord => AgendaOwner != Guid.Empty;
            internal MedicalAgenda.Appointment Appointment;
            internal DiseaseDefinition Disease;
            internal ExamOrders Orders;
            internal int RiskLevel = 1;
            internal int SeverityIndex
            {
                get => Math.Max(0, RiskLevel - 1);
                set => RiskLevel = value + 1;
            }
            internal long NextPulseRefresh;
            internal (int Pulse, int Systolic, int Diastolic, int RespiratoryRate, int OxygenSaturation, double? Temperature) Vitals;
            internal bool Deceased;
            internal bool CoronersCalled;
            internal long CoronerPickupAt;
            internal bool SeverityClockRunning;
            internal bool HasConsulted;
            internal bool ConsultationRequestedByPlayer;
            internal bool TransferRequestedByPlayer;
            internal bool AwaitingBed;
            internal long WorkerDueAt;
            internal long LastWorkerVisit;
            internal long NextSeverityAt;
        }

        internal static void Run(HospitalStaffRoutes routes, WaitingRoom waitingRoom, AmbientPopulation population, bool startedDuringLoading, Func<bool> consumeShiftToggle, Func<bool> consumeResidenceRequest,
            Func<Ped, bool> isHospitalSceneReady, Action<Ped, bool, bool> updateAmbient)
        {
            Point[] tableOrigins = DiagnosticRooms.Select((room, index) => routes.AddDiagnosticTransferAccess(
                index, room.Name, room.Table, room.TableExit, room.TableHeading, room.Route[room.Route.Length - 1])).ToArray();
            Point[] inpatientRoomDestinations = InpatientRooms.Select((room, index) =>
                room.Unit == InpatientUnit.Room
                    ? HospitalUpperFloor.AddInpatientRoomAccess(routes.Path, index)
                    : routes.AddWardBedAccess(room.Name, room.InteractionPoint, room.Heading)).ToArray();
            var consultationAccess = DiagnosticRooms.Select((room, index) =>
                routes.AddConsultationAccess(index, room.PatientSeat, room.PatientHeading,
                    room.Route[room.Route.Length - 1])).ToArray();
            Point[] doctorAccess = DiagnosticRooms.Select((room, index) => routes.AddDoctorWorkAccess(
                index, room.DoctorInteraction, room.DoctorHeading, consultationAccess[index].Access)).ToArray();
            var menuPool = new MenuPool();
            var menu = new UIMenu("Lumen Santé", "Central Los Santos Medical Center");
            var shiftAction = MenuPalette.Item("Iniciar plantão", ActionTone.Accent);
            var callPatient = MenuPalette.Item("Chamar paciente", ActionTone.Accent, enabled: false);
            menu.AddItem(shiftAction);
            menu.AddItem(callPatient);
            var roomStatusItems = DiagnosticRooms.Select(room => MenuPalette.Item(room.Name, enabled: false)).ToArray();
            foreach (UIMenuItem status in roomStatusItems)
            {
                menu.AddItem(status);
            }
            menuPool.Add(menu);
            bool interactionWasDown = false;
            bool doctorsWasDown = false;

            var random = new Random();
            var consultations = new PatientCase[DiagnosticRooms.Length];
            var inpatients = new PatientCase[InpatientRooms.Length];
            var receptionCases = new Dictionary<Ped, PatientCase>();
            var cases = consultations.Concat(inpatients).Concat(receptionCases.Values);
            var dischargedCases = new List<PatientCase>();
            var doctorProfiles = new DoctorProfiles(System.IO.Path.Combine(PluginPaths.InstallationDirectory, "LumenSante"),
                message => Game.LogTrivial("[Lumen Santé] " + message));
            var residence = new NativeResidence();
            bool homeStartupPending = startedDuringLoading && doctorProfiles.Default != null;
            var homeStartupClock = Stopwatch.StartNew();
            var startupReadiness = new StartupReadiness();
            // Manual loads keep the current location, but should also wait for a stable
            // player before applying a saved model.
            bool waitingForSave = true;
            bool startupFadeOwned = false;
            var startupFadeClock = new Stopwatch();
            void ReleaseStartupFade()
            {
                if (!startupFadeOwned) return;
                startupFadeOwned = false;
                startupFadeClock.Reset();
                NativeFunction.Natives.DO_SCREEN_FADE_IN(350);
            }
            var agendas = new Dictionary<Guid, MedicalAgenda>();
            MedicalAgenda agenda = null;
            Guid agendaProfile = Guid.Empty;
            DateTime agendaNow = default(DateTime);
            long nextAgendaUpdate = 0;
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
            WorkerCoordinator workers = null;
            long hospitalStartedAt = GameClock.NowMilliseconds;
            int doctorRoomIndex = 0;
            Ped examiningDoctor = null;
            string activeExamAnimation = null;
            string activeExamClip = null;
            HospitalComputer hospitalComputer = null;
            DoctorMenu doctors = null;
            bool hospitalWebsiteObserved = false;
            bool hospitalSceneReady = false;
            int hospitalBrowserHandle = 0;
            long nextWebsiteUpdate = 0;
            int previousWebsitePage = -1;
            var websiteClock = Stopwatch.StartNew();
            uint internetScript = NativeFunction.CallByHash<uint>(0xD24D37CC275948CCUL, "appinternet");
            Guid[] websitePatientIds = Array.Empty<Guid>();
            long[] websiteCareRevisions = Array.Empty<long>();
            int websitePatientGeneration = 0;
            Guid websiteSelectedPatient = Guid.Empty;
            Guid[] websiteAgendaIds = Array.Empty<Guid>();
            Guid websiteAgendaOwner = Guid.Empty;
            int websiteAgendaGeneration = 0;
            int lastHospitalPage = 7;
            Guid lastHospitalPatient = Guid.Empty;
            int websiteOpenStage = 0;
            bool websiteOpenFromPhone = false;
            int websiteOpenSequence = 0;
            int websiteOpenPage = 7;
            Guid websiteOpenPatient = Guid.Empty;
            bool websiteOpenPatientPending = false;
            long websiteOpenStarted = 0;
            string websiteAgendaFeedback = "";
            bool websiteAgendaActive = false;
            Guid websiteNotesPatient = Guid.Empty;
            string websiteNotesOriginal = null;

            void ReleaseHospitalBrowser()
            {
                if (hospitalBrowserHandle == 0) return;
                IntPtr pointer = Marshal.AllocHGlobal(sizeof(int));
                try
                {
                    Marshal.WriteInt32(pointer, hospitalBrowserHandle);
                    NativeFunction.CallByHash<int>(0x1D132D614DD86811UL, pointer.ToInt64());
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                    hospitalBrowserHandle = 0;
                }
            }

            bool IsAccessible(PatientCase patient) => patient != null &&
                (!patient.AgendaRecord || (doctorProfiles.Default != null && patient.AgendaOwner == doctors.ActiveId));

            bool CanInteractWithPatient() =>
                shiftPlayer != null && IsAccessible(current) && current.Ped != null &&
                current.Ped.Exists() && current.Ped.IsAlive && !current.Deceased;

            var patientMenu = new PatientMenu(menuPool, DiagnosticRooms[current.DiagnosticRoomIndex].Name,
                CanInteractWithPatient);
            hospitalComputer = new HospitalComputer(menuPool, DiagnosticRooms[current.DiagnosticRoomIndex].Name, patientMenu,
                () => current.Location == PatientLocation.Chair && current.DiagnosticRoomIndex == doctorRoomIndex && CanInteractWithPatient(),
                () => consultations[doctorRoomIndex] != null,
                () => IsAccessible(consultations[doctorRoomIndex]) ? consultations[doctorRoomIndex].Name : null,
                CanCallPatient, () => CallPatientToRoom(doctorRoomIndex), CanCallSelectedPatient,
                orders => CanCallSelectedPatient(orders) &&
                    CallPatientToRoom(doctorRoomIndex, cases.First(candidate => candidate?.Orders == orders).Ped));
            hospitalComputer.ConfigureAgenda(agenda, () => agendaNow, CancelPendingReturn);
            void RequestHospitalWebsite(bool fromPhone)
            {
                websiteOpenPatientPending = false;
                var roomPatient = fromPhone ? null : consultations[doctorRoomIndex];
                websiteOpenPatient = roomPatient != null && IsAccessible(roomPatient) &&
                    (roomPatient.Location == PatientLocation.Chair || roomPatient.Location == PatientLocation.ExaminationTable) &&
                    !roomPatient.Deceased
                    ? roomPatient.Orders.AdmissionId : Guid.Empty;
                websiteOpenPage = websiteOpenPatient != Guid.Empty ? 8 : lastHospitalPage;
                if (websiteOpenPatient == Guid.Empty && websiteOpenPage == 8 && lastHospitalPatient != Guid.Empty &&
                    GetAccessiblePatients().Any(entry => entry.Orders.AdmissionId == lastHospitalPatient))
                    websiteOpenPatient = lastHospitalPatient;
                websiteOpenSequence = websiteOpenSequence % 500000 + 1;
                websiteOpenStarted = websiteClock.ElapsedMilliseconds;
                websiteOpenFromPhone = fromPhone;
                // The phone controller can rebuild its app table when an app is selected.
                // Explicitly request the browser for both entry points instead of assuming
                // the custom phone row will start it successfully.
                websiteOpenStage = 1;
                NativeFunction.Natives.REQUEST_SCRIPT("appinternet");
            }
            hospitalComputer.WebsiteOpenRequested += () => RequestHospitalWebsite(false);
            var phoneShortcut = new NativePhoneShortcut();

            bool CanCallSelectedPatient(ExamOrders orders) => orders != null && CanCallPatient() &&
                consultations[doctorRoomIndex] == null && cases.Any(candidate => candidate?.Orders == orders &&
                    IsAccessible(candidate) && !candidate.Deceased && candidate.Location == PatientLocation.WaitingReception &&
                    waitingRoom.IsWaiting(candidate.Ped));

            bool CanCallPatient() =>
                shiftPlayer != null && NextPatient() != null;

            Ped NextPatient() => waitingRoom.FirstWaiting(patient =>
                    !receptionCases.TryGetValue(patient, out PatientCase candidate) ||
                    (IsAccessible(candidate) && !workers.IsPatientReserved(candidate))) ??
                waitingRoom.FirstWaiting(patient =>
                    !receptionCases.TryGetValue(patient, out PatientCase candidate) || IsAccessible(candidate));

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
                if (!IsAccessible(selected)) selected = null;
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

            BedManagementPanel.PatientEntry[] GetAccessiblePatients() => cases.Concat(dischargedCases)
                .Where(admission => admission?.Orders != null && IsAccessible(admission))
                .GroupBy(admission => admission.Orders.AdmissionId)
                .Select(group => group.First())
                .Select(admission => new BedManagementPanel.PatientEntry(admission.Orders, admission.Ped,
                    admission.Deceased, dischargedCases.Contains(admission),
                    admission.InpatientRoomIndex < 0 ||
                    (admission.Location != PatientLocation.WalkingToInpatientBed &&
                     admission.Location != PatientLocation.InpatientBed &&
                     admission.Location != PatientLocation.UndergoingExam) ? (InpatientUnit?)null :
                        InpatientRooms[admission.InpatientRoomIndex].Unit,
                    !admission.Deceased && !admission.Orders.IsRecovered && !admission.Orders.IsDischarged &&
                    (admission.Location == PatientLocation.Chair ||
                     admission.Location == PatientLocation.WalkingToInpatientBed ||
                     admission.Location == PatientLocation.InpatientBed ||
                     admission.Location == PatientLocation.UndergoingExam),
                    admission.Appointment != null))
                .ToArray();

            void RefreshPatientList() => hospitalComputer.SetPatients(GetAccessiblePatients());

            PatientCase RegisterReceptionPatient(Ped patient, string scheduledName = null)
            {
                PatientCase admission = dischargedCases.FirstOrDefault(candidate => candidate.AwaitingReturn && candidate.Ped == patient);
                if (admission == null)
                {
                    admission = new PatientCase
                    {
                        Ped = patient,
                        Model = patient.Model,
                        Appearance = new PatientAppearance.Snapshot(patient),
                        Name = scheduledName ?? PatientNames.Pick(random, !patient.IsMale),
                        Disease = DiseaseCatalog.Pick(random),
                        RiskLevel = random.Next(1, 6)
                    };
                    admission.Vitals = admission.Disease.Stages[admission.SeverityIndex].SampleVitals(random);
                    admission.Orders = new ExamOrders(admission.Name, "Recepção", admission.Disease.Id, new PatientProfile(random));
                    admission.Orders.RecordInitialComplaint(admission.Disease, admission.SeverityIndex, random);
                    admission.LastWorkerVisit = GameClock.NowMilliseconds;
                }
                admission.WorkerDueAt = GameClock.NowMilliseconds;
                admission.Location = PatientLocation.WaitingReception;
                if (scheduledName != null && admission.Orders.IsDischarged) admission.Orders.WaitForFollowUp();
                else admission.Orders.Readmit("Recepção");
                receptionCases[patient] = admission;
                return admission;
            }

            void SyncReceptionPatients()
            {
                var waiting = new HashSet<Ped>(waitingRoom.Patients);
                bool changed = false;
                foreach (Ped patient in receptionCases.Keys.Where(patient => !waiting.Contains(patient)).ToArray())
                {
                    if (receptionCases[patient].AwaitingBed) continue;
                    PatientCase lost = receptionCases[patient];
                    if (lost.Appointment != null && lost.Appointment.State == MedicalAgenda.Status.Arrived)
                        lost.Appointment.State = MedicalAgenda.Status.Ready;
                    if (lost.AgendaRecord)
                    {
                        lost.Ped = null;
                        lost.Appointment = null;
                        dischargedCases.Add(lost);
                    }
                    receptionCases.Remove(patient);
                    changed = true;
                }
                foreach (Ped patient in waiting)
                {
                    if (receptionCases.ContainsKey(patient)) continue;
                    RegisterReceptionPatient(patient);
                    changed = true;
                }
                if (changed) RefreshPatientList();
            }

            void NotifyScheduledArrival(Ped patient)
            {
                if (!receptionCases.TryGetValue(patient, out PatientCase admission) ||
                    !IsAccessible(admission) || admission.Appointment == null || admission.AwaitingBed) return;

                string purpose = admission.Appointment.FollowUp ? "o retorno" : "a consulta";
                Game.DisplayNotification("Agenda: " + admission.Name + " chegou à recepção para " + purpose + ".");
            }

            void BeginConsultation()
            {
                ReturnPatientToChair();
                bool notifyPlayer = current.ConsultationRequestedByPlayer;
                current.ConsultationRequestedByPlayer = false;
                if (current.HasConsulted)
                {
                    if (hospitalComputer.IsOpen && doctorRoomIndex == current.DiagnosticRoomIndex) BindPatient(current);
                    RefreshPatientList();
                    if (notifyPlayer) Game.DisplayNotification(current.Name + " retornou para reavaliação.");
                    return;
                }
                current.HasConsulted = true;
                current.SeverityClockRunning = true;
                current.NextSeverityAt = GameClock.NowMilliseconds + random.Next(
                    current.Disease.MinimumStageDurationMilliseconds,
                    current.Disease.MaximumStageDurationMilliseconds + 1);
                current.NextPulseRefresh = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(PulseRefreshMilliseconds);
                RefreshPatientList();
                if (hospitalComputer.IsOpen && doctorRoomIndex == current.DiagnosticRoomIndex)
                {
                    BindPatient(current);
                }
                if (notifyPlayer) Game.DisplayNotification(current.Name + " está na " + DiagnosticRooms[current.DiagnosticRoomIndex].Name + ".");
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

                bool IsFree(int index) => consultations[index] == null && !workers.IsComputerReserved(index);
                int roomIndex = IsFree(doctorRoomIndex) ? doctorRoomIndex :
                    Enumerable.Range(0, consultations.Length).Where(IsFree).DefaultIfEmpty(-1).First();
                if (roomIndex < 0)
                {
                    Game.DisplayNotification("As salas de diagnóstico estão ocupadas ou reservadas.");
                    return;
                }
                if (CallPatientToRoom(roomIndex))
                {
                    menu.Visible = false;
                }
            };

            bool CallPatientToRoom(int roomIndex, Ped requestedPatient = null, bool autonomous = false)
            {
                if (!autonomous)
                {
                    requestedPatient = requestedPatient ?? NextPatient();
                    if (requestedPatient == null || (receptionCases.TryGetValue(requestedPatient, out PatientCase assigned) &&
                        !IsAccessible(assigned))) return false;
                }
                if (autonomous && (requestedPatient == null ||
                    (receptionCases.TryGetValue(requestedPatient, out PatientCase selected) && selected.Appointment != null)))
                    return false;
                AmbientPatrol nurse = population.FindNurse(HospitalStaffRoutes.Reception);
                if (nurse == null)
                {
                    if (!autonomous) Game.DisplayNotification("Aguarde um enfermeiro disponível para buscar o paciente.");
                    return false;
                }
                if ((!autonomous && !CanCallPatient()) || consultations[roomIndex] != null ||
                    !waitingRoom.TryCallPatient(out Ped called, out Point approach, requestedPatient))
                {
                    return false;
                }

                PatientCase admission = receptionCases.TryGetValue(called, out PatientCase waitingCase)
                    ? waitingCase : RegisterReceptionPatient(called);
                receptionCases.Remove(called);
                dischargedCases.Remove(admission);
                admission.AwaitingReturn = false;
                if (!autonomous) workers?.PlayerTakesPatient(admission);
                admission.ConsultationRequestedByPlayer = !autonomous;
                admission.AwaitingBed = false;
                admission.DiagnosticRoomIndex = roomIndex;
                admission.Orders.Readmit(DiagnosticRooms[roomIndex].Name);
                consultations[roomIndex] = admission;
                admission.Pickup = approach;
                admission.Location = PatientLocation.WalkingToConsultation;
                if (admission.Appointment != null)
                {
                    admission.Appointment.State = MedicalAgenda.Status.InCare;
                    SaveAgenda();
                }
                RefreshPatientList();
                try
                {
                    StartPatientEscort(admission, nurse);
                }
                catch (InvalidOperationException error)
                {
                    Game.LogTrivial("[Lumen Santé] Busca do paciente aguardando nova tentativa: " + error.Message);
                }
                if (!autonomous) Game.DisplayNotification("Um enfermeiro buscará " + admission.Name + " na recepção.");
                return true;
            }

            void StartPatientEscort(PatientCase admission, AmbientPatrol nurse)
            {
                admission.NextEscortAttemptAt = GameClock.NowMilliseconds + GameClock.Minutes(5);
                admission.Escort = new PatientEscort(routes.Path, nurse, admission.Ped, admission.Pickup,
                    consultationAccess[admission.DiagnosticRoomIndex].Access);
            }

            void UpdateConsultationArrival()
            {
                if (current.ConsultationChairWalk != null)
                {
                    if (!current.ConsultationChairWalk.IsWalking || current.ConsultationChairWalk.Update(current.Ped))
                    {
                        BeginConsultation();
                        current.ConsultationChairWalk.Clear();
                        current.ConsultationChairWalk = null;
                        current.Escort?.Release(stopPatient: false);
                        current.Escort = null;
                    }
                    return;
                }
                if (current.Escort == null)
                {
                    if (GameClock.NowMilliseconds < current.NextEscortAttemptAt) return;
                    AmbientPatrol nurse = population.FindNurse(current.Pickup);
                    if (nurse == null) return;
                    StartPatientEscort(current, nurse);
                }
                if (current.Escort.Update())
                {
                    var access = consultationAccess[current.DiagnosticRoomIndex];
                    current.ConsultationChairWalk = new NpcRoute();
                    current.ConsultationChairWalk.Start(current.Ped, access.Access, new[] { access.Chair }, teleportOnTimeout: true);
                    current.Escort.Release(stopPatient: false);
                    current.Escort = null;
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

            patientMenu.AdmissionRequested += unit =>
            {
                if (!CanInteractWithPatient() || current.Location != PatientLocation.Chair ||
                    current.DiagnosticRoomIndex != doctorRoomIndex || !hospitalComputer.IsSeated(Game.LocalPlayer.Character)) return;
                workers?.PlayerTakesPatient(current);
                AdmitPatient(current, unit, false);
            };
            patientMenu.TransferRequested += unit =>
            {
                if (!CanInteractWithPatient() || current.Location != PatientLocation.InpatientBed ||
                    current.InpatientRoomIndex < 0 || InpatientRooms[current.InpatientRoomIndex].Unit == unit) return;
                if (current == activeExam.Patient || current == visitedPatient || workers?.IsPatientReserved(current) == true)
                {
                    Game.DisplayNotification("Aguarde o término do procedimento antes de transferir o paciente.");
                    return;
                }
                TransferInpatientPatient(current, unit);
            };

            bool AdmitPatient(PatientCase admission, InpatientUnit unit, bool autonomous)
            {
                if (admission.Deceased || admission.Ped == null || !admission.Ped.Exists() || !admission.Ped.IsAlive ||
                    admission.Location != PatientLocation.Chair || admission.Orders.IsDischarged) return false;
                if (autonomous && (admission.Orders.IsCareProtected || (unit == InpatientUnit.Room &&
                    Enumerable.Range(0, inpatients.Length).Count(index => InpatientRooms[index].Unit == unit && inpatients[index] == null) <= 1))) return false;
                if (!Enumerable.Range(0, inpatients.Length).Any(index =>
                    InpatientRooms[index].Unit == unit && inpatients[index] == null))
                {
                    if (!autonomous) Game.DisplayNotification(unit == InpatientUnit.Room
                        ? "Todos os quartos estão ocupados." : "Todos os leitos da enfermaria estão ocupados.");
                    return false;
                }
                Point origin = consultationAccess[admission.DiagnosticRoomIndex].Access;
                int roomIndex = -1;
                Point[] waypoints = null;
                double shortestDistance = double.MaxValue;
                for (int index = 0; index < inpatients.Length; index++)
                {
                    if (inpatients[index] != null || InpatientRooms[index].Unit != unit) continue;
                    Point[] candidate = routes.Path.FindRoute(origin, inpatientRoomDestinations[index]);
                    if (candidate == null || candidate.Length == 0) continue;
                    double distance = 0;
                    Vector3 previous = admission.Ped.Position;
                    foreach (Point point in candidate)
                    {
                        distance += Math.Sqrt((point.Position - previous).LengthSquared());
                        previous = point.Position;
                    }
                    distance += Math.Sqrt((InpatientRooms[index].Bed - previous).LengthSquared());
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        roomIndex = index;
                        waypoints = candidate;
                    }
                }
                if (roomIndex < 0)
                {
                    if (!autonomous) Game.DisplayNotification("Não há rota disponível para os leitos livres.");
                    return false;
                }
                var room = InpatientRooms[roomIndex];

                admission.Ped.Tasks.Clear();
                admission.Ped.IsPositionFrozen = false;
                NativeFunction.Natives.SET_ENTITY_COLLISION(admission.Ped, true, true);
                NpcWalking.TeleportToWaypoint(admission.Ped, origin.Position);
                admission.InpatientTransfer = new NpcRoute();
                admission.InpatientTransfer.Start(admission.Ped, origin, waypoints, teleportOnTimeout: false);
                admission.InpatientRoomIndex = roomIndex;
                admission.TransferRequestedByPlayer = !autonomous;
                admission.Location = PatientLocation.WalkingToInpatientBed;
                inpatients[roomIndex] = admission;
                consultations[admission.DiagnosticRoomIndex] = null;
                RefreshPatientList();
                if (shownCase == admission) patientMenu.Close();
                if (!autonomous) Game.DisplayNotification(admission.Name + " está a caminho do " + room.Name + ".");
                Game.LogTrivial("[Lumen Santé] Encaminhamento para " + room.Name + ": " + admission.Name + ".");
                return true;
            }

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
                    if (current.TransferRequestedByPlayer) Game.DisplayNotification(current.Name + " foi internado no " + room.Name + ".");
                    current.TransferRequestedByPlayer = false;
                    Game.LogTrivial("[Lumen Santé] Internado no " + room.Name + ": " + current.Name + ".");
                }
                catch (InvalidOperationException error)
                {
                    current.NextBedPoseAttempt = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(3000);
                    Game.LogTrivial("[Lumen Santé] Falha na animação do leito: " + error.Message);
                }
            }

            bool TransferInpatientPatient(PatientCase admission, InpatientUnit unit)
            {
                if (admission.Deceased || admission.Location != PatientLocation.InpatientBed ||
                    admission.InpatientRoomIndex < 0 || admission.Ped == null || !admission.Ped.Exists() ||
                    !admission.Ped.IsAlive || admission.Orders.IsDischarged) return false;

                int sourceIndex = admission.InpatientRoomIndex;
                int targetIndex = -1;
                Point[] waypoints = null;
                double shortestDistance = double.MaxValue;
                Point origin = inpatientRoomDestinations[sourceIndex];
                for (int index = 0; index < inpatients.Length; index++)
                {
                    if (inpatients[index] != null || InpatientRooms[index].Unit != unit) continue;
                    Point[] candidate = routes.Path.FindRoute(origin, inpatientRoomDestinations[index]);
                    if (candidate == null || candidate.Length == 0) continue;
                    double distance = 0;
                    Vector3 previous = admission.Ped.Position;
                    foreach (Point point in candidate)
                    {
                        distance += Math.Sqrt((point.Position - previous).LengthSquared());
                        previous = point.Position;
                    }
                    distance += Math.Sqrt((InpatientRooms[index].Bed - previous).LengthSquared());
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        targetIndex = index;
                        waypoints = candidate;
                    }
                }

                if (targetIndex < 0)
                {
                    Game.DisplayNotification(unit == InpatientUnit.Room
                        ? "Todos os quartos estão ocupados ou sem rota disponível."
                        : "Todos os leitos da enfermaria estão ocupados ou sem rota disponível.");
                    return false;
                }

                try
                {
                    admission.Ped.Tasks.Clear();
                    admission.Ped.IsPositionFrozen = false;
                    NativeFunction.Natives.SET_ENTITY_COLLISION(admission.Ped, true, true);
                    NpcWalking.TeleportToWaypoint(admission.Ped, origin.Position);
                    var transfer = new NpcRoute();
                    transfer.Start(admission.Ped, origin, waypoints, teleportOnTimeout: false);

                    admission.InpatientTransfer = transfer;
                    admission.InpatientRoomIndex = targetIndex;
                    admission.TransferRequestedByPlayer = true;
                    admission.Location = PatientLocation.WalkingToInpatientBed;
                    inpatients[sourceIndex] = null;
                    inpatients[targetIndex] = admission;
                    admission.NextBedPoseAttempt = 0;
                    if (shownCase == admission) patientMenu.Close();
                    RefreshPatientList();
                    Game.DisplayNotification(admission.Name + " está a caminho do " + InpatientRooms[targetIndex].Name + ".");
                    Game.LogTrivial("[Lumen Santé] Transferência de internação: " + admission.Name + " para " +
                        InpatientRooms[targetIndex].Name + ".");
                    return true;
                }
                catch (Exception error)
                {
                    admission.InpatientTransfer?.Clear();
                    admission.InpatientTransfer = null;
                    admission.Ped.IsPositionFrozen = true;
                    NativeFunction.Natives.SET_ENTITY_COLLISION(admission.Ped, false, true);
                    var room = InpatientRooms[sourceIndex];
                    HospitalReceptionist.PlaceAt(admission.Ped, room.Bed, room.Heading,
                        BedAnimations.Lounger.Dictionary, BedAnimations.Lounger.Clip);
                    Game.LogTrivial("[Lumen Santé] Falha ao iniciar transferência entre unidades: " + error.Message);
                    Game.DisplayNotification("Não foi possível iniciar a transferência do paciente.");
                    return false;
                }
            }

            bool ReturnToWaiting(PatientCase admission)
            {
                if (admission.Location != PatientLocation.Chair || admission.Deceased || admission.Orders.IsCareProtected ||
                    !waitingRoom.ReceiveFromConsultation(admission.Ped, consultationAccess[admission.DiagnosticRoomIndex].Access)) return false;
                consultations[admission.DiagnosticRoomIndex] = null;
                admission.Location = PatientLocation.WaitingReception;
                admission.Orders.Readmit("Recepção — Aguardando leito");
                receptionCases[admission.Ped] = admission;
                RefreshPatientList();
                return true;
            }

            patientMenu.DischargeRequested += (orders, schedule) =>
            {
                PatientCase admission = cases.FirstOrDefault(candidate => candidate?.Orders == orders);
                if (!IsAccessible(admission) || shownCase != admission || shiftPlayer == null) return;
                if (schedule && (agenda?.Available != true || agendaNow == default(DateTime)))
                {
                    Game.DisplayNotification("Agenda indisponível. A alta com retorno não foi registrada.");
                    return;
                }
                workers?.PlayerTakesPatient(admission);
                if (!DischargePatient(admission, CareAuthor.Player, orders.CareRevision)) return;
                if (schedule)
                {
                    admission.AgendaOwner = doctors.ActiveId;
                    var appointment = agenda.ScheduleReturn(agendaNow, CaptureAgendaRecord(admission));
                    if (appointment != null && SaveAgenda(admission.AgendaOwner))
                        Game.DisplayNotification("Retorno de " + admission.Name + " em " + appointment.Date.ToString("dd/MM 'às' HH:mm") + ".");
                    else Game.DisplayNotification("Não foi possível gravar o retorno. Consulte a agenda.");
                }
                else if (admission.AgendaRecord) SaveAgenda(admission.AgendaOwner);
            };

            bool DischargePatient(PatientCase admission, CareAuthor author, long expectedRevision)
            {
                if (admission.Appointment != null && !author.IsPlayer) return false;
                if (admission.Deceased || admission.Ped == null || !admission.Ped.Exists() || !admission.Ped.IsAlive ||
                    (admission.Location != PatientLocation.Chair && admission.Location != PatientLocation.InpatientBed) ||
                    !admission.Orders.CanApplyCare(author, expectedRevision)) return false;
                Point origin = routes.Path.FindNearestWaypoint(admission.Ped.Position, tableOrigins);
                if (origin == null)
                {
                    if (author.IsPlayer) Game.DisplayNotification("Não há ponto de pathing disponível para a alta.");
                    return false;
                }
                Point[] exit = routes.Path.FindRoute(origin, HospitalStaffRoutes.PatientSpawn);
                if (exit == null || exit.Length == 0)
                {
                    if (author.IsPlayer) Game.DisplayNotification("Não há rota disponível para a saída do hospital.");
                    return false;
                }

                if (activeExam.Patient == admission)
                {
                    procedure?.ReleaseNurse();
                    procedure = null;
                    foreach (ExamOrder order in activeOrders) order.Cancel();
                    activeOrders.Clear();
                    activeExam = default((PatientCase, ExamOrder));
                }
                if (visitedPatient == admission)
                {
                    nursingVisit?.ReleaseNurse();
                    nursingVisit = null;
                    visitedPatient = null;
                }
                int pendingCount = pendingExams.Count;
                for (int index = 0; index < pendingCount; index++)
                {
                    var request = pendingExams.Dequeue();
                    if (request.Patient == admission) request.Order.Cancel();
                    else pendingExams.Enqueue(request);
                }

                admission.Escort?.Release();
                admission.Escort = null;
                admission.ConsultationChairWalk?.Clear();
                admission.ConsultationChairWalk = null;
                admission.InpatientTransfer?.Clear();
                admission.InpatientTransfer = null;
                admission.Ped.IsPositionFrozen = false;
                NativeFunction.Natives.SET_ENTITY_COLLISION(admission.Ped, true, true);
                NpcWalking.TeleportToWaypoint(admission.Ped, origin.Position);
                admission.DischargeWalk = new NpcRoute();
                admission.DischargeWalk.Start(admission.Ped, origin, new[] { origin }.Concat(exit).ToArray());
                if (admission.InpatientRoomIndex >= 0) inpatients[admission.InpatientRoomIndex] = null;
                else consultations[admission.DiagnosticRoomIndex] = null;
                admission.InpatientRoomIndex = -1;
                if (admission.Blip != null && admission.Blip.Exists()) admission.Blip.Delete();
                admission.Blip = null;
                admission.ReturnAt = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(8 * 60, 24 * 60 + 1));
                admission.SpontaneousReturn = !author.IsPlayer && !admission.AgendaRecord;
                if (!admission.Orders.Discharge(GameClock.NowMilliseconds, author, expectedRevision)) return false;
                if (admission.Appointment != null)
                {
                    admission.Appointment.State = MedicalAgenda.Status.Completed;
                    admission.Appointment = null;
                }
                dischargedCases.Add(admission);
                if (author.IsPlayer) Game.DisplayNotification("Alta registrada para " + admission.Name + ".");
                Game.LogTrivial("[Lumen Santé] " + author.Name + " registrou alta de " + admission.Name + ".");
                if (shownCase == admission) BindPatient(null);
                RefreshPatientList();
                return true;
            }

            void UpdateDischargedPatients(Ped player, bool playerAvailable)
            {
                long now = GameClock.NowMilliseconds;
                bool patientListChanged = false;
                foreach (PatientCase admission in dischargedCases)
                {
                    if (admission.DischargeWalk != null)
                    {
                        if (admission.Ped != null && admission.Ped.Exists() && !admission.Ped.IsAlive)
                        {
                            admission.Deceased = true;
                            patientListChanged = true;
                            admission.SeverityClockRunning = false;
                        }
                        if (admission.Ped != null && admission.Ped.Exists() && admission.Ped.IsAlive &&
                            admission.DischargeWalk.IsWalking && !admission.DischargeWalk.Update(admission.Ped)) continue;
                        if (admission.Ped != null && admission.Ped.Exists()) admission.Ped.Delete();
                        admission.Ped = null;
                        admission.DischargeWalk.Clear();
                        admission.DischargeWalk = null;
                        admission.OutsideHospital = true;
                        patientListChanged = true;
                    }
                    if (admission.AwaitingReturn)
                    {
                        if (admission.Ped != null && admission.Ped.Exists())
                        {
                            if (admission.Ped.IsAlive) continue;
                            admission.Deceased = true;
                            patientListChanged = true;
                            admission.SeverityClockRunning = false;
                        }
                        admission.AwaitingReturn = false;
                        admission.OutsideHospital = true;
                    }
                    if (!admission.OutsideHospital || !admission.SpontaneousReturn || admission.Orders.IsRecovered || admission.Deceased) continue;
                    while (admission.NextSeverityAt <= now && admission.RiskLevel > 0 &&
                        admission.RiskLevel < admission.Disease.Stages.Count)
                    {
                        long evaluatedAt = admission.NextSeverityAt;
                        admission.RiskLevel = admission.Disease.Evolve(admission.RiskLevel,
                            false,
                            admission.Orders.ActiveMedications(evaluatedAt), random);
                        admission.NextSeverityAt += random.Next(admission.Disease.MinimumStageDurationMilliseconds,
                            admission.Disease.MaximumStageDurationMilliseconds + 1);
                    }
                    if (admission.RiskLevel == 0)
                    {
                        admission.Orders.MarkRecovered();
                        patientListChanged = true;
                        admission.SeverityClockRunning = false;
                        continue;
                    }
                    if (admission.RiskLevel == admission.Disease.Stages.Count)
                    {
                        admission.Deceased = true;
                        patientListChanged = true;
                        admission.SeverityClockRunning = false;
                        continue;
                    }
                    if (!hospitalSceneReady || now < admission.ReturnAt || !waitingRoom.CanReceiveReturningPatient) continue;

                    Ped returning = null;
                    try
                    {
                        returning = new Ped(admission.Model, HospitalStaffRoutes.PatientSpawn.Position,
                            HospitalStaffRoutes.PatientSpawn.Heading);
                        admission.Appearance.Apply(returning);
                        waitingRoom.ReceiveReturningPatient(returning);
                        admission.Ped = returning;
                        patientListChanged = true;
                        admission.OutsideHospital = false;
                        admission.AwaitingReturn = true;
                        admission.Vitals = admission.Disease.Stages[admission.SeverityIndex].SampleVitals(random);
                        Game.LogTrivial("[Lumen Santé] " + admission.Name + " voltou ao hospital para reavaliação.");
                    }
                    catch (InvalidOperationException error)
                    {
                        if (returning != null && returning.Exists()) returning.Delete();
                        admission.ReturnAt = now + GameClock.Minutes(30);
                        Game.LogTrivial("[Lumen Santé] Retorno adiado: " + error.Message);
                    }
                }
                if (patientListChanged) RefreshPatientList();
            }

            MedicalAgenda.Record CaptureAgendaRecord(PatientCase admission) => new MedicalAgenda.Record
            {
                Orders = admission.Orders.Capture(GameClock.NowMilliseconds, agendaNow),
                Risk = admission.RiskLevel, Model = admission.Model.Hash, Outfit = admission.Appearance?.Capture(),
                Deceased = admission.Deceased
            };

            bool SaveAgenda(Guid onlyOwner = default(Guid))
            {
                if (agendaNow == default(DateTime)) return false;
                foreach (PatientCase patient in cases.Concat(dischargedCases).Where(patient => patient?.AgendaRecord == true).Distinct())
                    if ((onlyOwner == Guid.Empty || patient.AgendaOwner == onlyOwner) &&
                        agendas.TryGetValue(patient.AgendaOwner, out MedicalAgenda ownerAgenda))
                        ownerAgenda.UpdateRecord(CaptureAgendaRecord(patient));
                bool saved = true;
                foreach (var entry in agendas)
                    if ((onlyOwner == Guid.Empty || entry.Key == onlyOwner) && entry.Value.Available)
                        saved = entry.Value.Save() && saved;
                return saved;
            }

            bool CancelPendingReturn(Guid id)
            {
                var item = agenda?.Appointments.FirstOrDefault(candidate => candidate.Id == id &&
                    candidate.State == MedicalAgenda.Status.Pending);
                if (item == null) return false;
                item.State = MedicalAgenda.Status.Cancelled;
                if (SaveAgenda(agendaProfile)) return true;
                item.State = MedicalAgenda.Status.Pending;
                return false;
            }

            MedicalAgenda LoadDoctorAgenda(Guid owner)
            {
                if (!agendas.TryGetValue(owner, out MedicalAgenda schedule))
                {
                    schedule = new MedicalAgenda(doctorProfiles.AgendaPath(owner),
                        message => Game.LogTrivial("[Lumen Santé] " + message));
                    agendas.Add(owner, schedule);
                    RestoreAgendaRecords(owner, schedule);
                }
                return schedule;
            }

            void RestoreAgendaRecords(Guid owner, MedicalAgenda schedule)
            {
                if (agendaNow == default(DateTime)) return;
                var known = new HashSet<Guid>(cases.Concat(dischargedCases)
                    .Where(patient => patient?.AgendaOwner == owner).Select(patient => patient.Orders.AdmissionId));
                foreach (MedicalAgenda.Record record in schedule.Records)
                {
                    if (!known.Add(record.Orders.Id)) continue;
                    DiseaseDefinition disease = DiseaseCatalog.Find(record.Orders.Disease);
                    dischargedCases.Add(new PatientCase
                    {
                        AgendaOwner = owner, OutsideHospital = true, Deceased = record.Deceased,
                        Name = record.Orders.Name, Disease = disease, Model = new Model(record.Model),
                        Appearance = record.Outfit?.Length == 52 ? new PatientAppearance.Snapshot(record.Outfit) : null,
                        RiskLevel = disease == null ? 1 : Math.Max(1, Math.Min(record.Risk, disease.Stages.Count - 1)),
                        Orders = ExamOrders.Restore(record.Orders, GameClock.NowMilliseconds, agendaNow)
                    });
                }
            }

            bool ArchiveDoctor(Guid owner)
            {
                if (shiftPlayer != null) return false;
                if (agendas.TryGetValue(owner, out MedicalAgenda schedule) && schedule.Available && !SaveAgenda(owner))
                    return false;
                if (!doctorProfiles.Remove(owner)) return false;
                foreach (PatientCase admission in cases.Where(patient => patient?.AgendaOwner == owner).ToArray())
                {
                    workers.CancelPatient(admission);
                    if (activeExam.Patient == admission)
                    {
                        procedure?.ReleaseNurse();
                        procedure = null;
                        foreach (ExamOrder order in activeOrders) order.Cancel();
                        activeOrders.Clear();
                        activeExam = default((PatientCase, ExamOrder));
                    }
                    if (visitedPatient == admission)
                    {
                        nursingVisit?.ReleaseNurse();
                        nursingVisit = null;
                        visitedPatient = null;
                    }
                    if (admission.Location == PatientLocation.WaitingReception) waitingRoom.RemovePatient(admission.Ped);
                    admission.InpatientTransfer?.Clear();
                    ClearAdmission(admission);
                }
                foreach (PatientCase admission in dischargedCases.Where(patient => patient.AgendaOwner == owner).ToArray())
                {
                    admission.DischargeWalk?.Clear();
                    if (admission.Ped != null && admission.Ped.Exists()) admission.Ped.Delete();
                    dischargedCases.Remove(admission);
                }
                agendas.Remove(owner);
                RefreshPatientList();
                return true;
            }

            void UpdateAgenda()
            {
                Guid selected = doctorProfiles.Default != null ? doctors.ActiveId : Guid.Empty;
                if (selected == agendaProfile && GameClock.NowMilliseconds < nextAgendaUpdate) return;
                nextAgendaUpdate = GameClock.NowMilliseconds + GameClock.FromRealMilliseconds(1000);
                DateTime gameDate = MedicalAgenda.ReadGameDate();
                if (gameDate == default(DateTime)) return;
                agendaNow = gameDate;
                if (selected != agendaProfile)
                {
                    SaveAgenda();
                    agenda = selected == Guid.Empty ? null : LoadDoctorAgenda(selected);
                    agendaProfile = selected;
                    hospitalComputer.ConfigureAgenda(agenda, () => agendaNow, CancelPendingReturn);
                    RefreshPatientList();
                }
                agenda?.Generate(agendaNow, random);
                var live = cases.Where(patient => patient?.Appointment != null).ToLookup(patient => patient.AgendaOwner);
                foreach (var entry in agendas)
                {
                    if (entry.Value.Available)
                        UpdateDoctorAgenda(entry.Key, entry.Value, shiftPlayer != null && entry.Key == agendaProfile,
                            live[entry.Key].ToDictionary(patient => patient.Appointment.Id));
                }
                SaveAgenda();
            }

            void UpdateDoctorAgenda(Guid owner, MedicalAgenda schedule, bool onDuty, Dictionary<Guid, PatientCase> live)
            {
                bool listChanged = false;
                foreach (var item in schedule.Appointments.Where(item => item.Due <= agendaNow.Ticks &&
                    (item.State == MedicalAgenda.Status.Scheduled || item.State == MedicalAgenda.Status.Ready ||
                     item.State == MedicalAgenda.Status.Arrived || item.State == MedicalAgenda.Status.InCare))
                    .OrderBy(item => item.Due))
                {
                    live.TryGetValue(item.Id, out PatientCase active);
                    bool inCare = item.State == MedicalAgenda.Status.InCare ||
                        (active != null && active.Location != PatientLocation.WaitingReception);
                    if (!inCare && MedicalAgenda.ShouldExpire(item, agendaNow, onDuty))
                    {
                        if (active != null)
                        {
                            waitingRoom.RemovePatient(active.Ped);
                            receptionCases.Remove(active.Ped);
                            active.Ped = null;
                            active.Appointment = null;
                            if (active.AgendaRecord && !dischargedCases.Contains(active)) dischargedCases.Add(active);
                            listChanged = true;
                        }
                        schedule.Expire(item);
                        continue;
                    }
                    if (!onDuty || active != null) continue;
                    if (item.State == MedicalAgenda.Status.Scheduled || item.State == MedicalAgenda.Status.Arrived)
                        item.State = MedicalAgenda.Status.Ready;
                    if (!hospitalSceneReady || !waitingRoom.CanReceiveReturningPatient) continue;

                    PatientCase archived = dischargedCases.FirstOrDefault(patient =>
                        patient.AgendaOwner == owner && patient.Orders.AdmissionId == item.PatientId);
                    if (archived != null && (archived.Deceased || archived.Disease == null))
                    {
                        schedule.Expire(item);
                        continue;
                    }
                    if (archived?.DischargeWalk != null) continue;
                    Point spawn = HospitalStaffRoutes.PatientSpawn;
                    Ped arriving = null;
                    try
                    {
                        arriving = new Ped(archived?.Model ?? PatientAppearance.ScheduledModel(item.Female),
                            spawn.Position, spawn.Heading);
                        if (!arriving.Exists()) throw new InvalidOperationException("NPC indisponível.");
                        archived?.Appearance?.Apply(arriving);
                        waitingRoom.ReceiveReturningPatient(arriving);
                    }
                    catch (InvalidOperationException error)
                    {
                        if (arriving != null && arriving.Exists()) arriving.Delete();
                        Game.LogTrivial("[Lumen Santé] Chegada agendada adiada: " + error.Message);
                        break;
                    }

                    if (archived != null)
                    {
                        archived.Ped = arriving;
                        archived.AwaitingReturn = true;
                    }
                    PatientCase patientCase = RegisterReceptionPatient(arriving, item.Name);
                    patientCase.Vitals = patientCase.Disease.Stages[patientCase.SeverityIndex].SampleVitals(random);
                    dischargedCases.Remove(patientCase);
                    patientCase.AwaitingReturn = false;
                    patientCase.OutsideHospital = false;
                    patientCase.AgendaOwner = owner;
                    patientCase.Appointment = item;
                    item.PatientId = patientCase.Orders.AdmissionId;
                    if (!inCare) item.State = MedicalAgenda.Status.Arrived;
                    listChanged = true;
                }
                if (listChanged) RefreshPatientList();
            }

            hospitalComputer.MedicationToggled += (orders, medication) =>
            {
                PatientCase admission = cases.FirstOrDefault(candidate => candidate?.Orders == orders);
                Ped doctor = Game.LocalPlayer.Character;
                if (shiftPlayer != doctor || !IsAccessible(admission) || admission.Deceased ||
                    admission.Location == PatientLocation.WaitingReception ||
                    !hospitalComputer.IsSeated(doctor)) return;
                var option = MedicationCatalog.Options.FirstOrDefault(choice => choice.Class == medication);
                if (option.Name == null) return;
                bool active = orders.ActiveMedications(GameClock.NowMilliseconds).Contains(medication);
                if (active)
                {
                    if (!orders.StopMedication(medication, CareAuthor.Player, GameClock.NowMilliseconds, orders.CareRevision)) return;
                }
                else
                {
                    if (orders.IsRecovered) return;
                    if (!orders.PrescribeMedication(medication, GameClock.NowMilliseconds + GameClock.Minutes(8 * 60),
                        CareAuthor.Player, GameClock.NowMilliseconds, orders.CareRevision)) return;
                }
                Game.DisplayNotification((active ? "Medicação removida: " : "Medicação receitada: ") + option.Name + " para " + admission.Name + ".");
                workers?.PlayerTakesPatient(admission);
            };
            hospitalComputer.ExamRequested += (orders, order) =>
            {
                PatientCase admission = cases.FirstOrDefault(candidate => candidate?.Orders == orders);
                Ped doctor = Game.LocalPlayer.Character;
                if (shiftPlayer != doctor || !IsAccessible(admission) ||
                    !hospitalComputer.IsSeated(doctor)) return;
                workers?.PlayerTakesPatient(admission);
                RequestPatientExam(admission, order, true);
            };
            hospitalComputer.TreatmentSelected += (orders, diseaseId) =>
            {
                PatientCase admission = cases.FirstOrDefault(candidate => candidate?.Orders == orders);
                if (shiftPlayer == null || !IsAccessible(admission) || admission.Deceased || admission.Orders.IsRecovered ||
                    admission.Location == PatientLocation.WaitingReception ||
                    !DiseaseCatalog.Treatments.Any(disease => disease.Id == diseaseId) ||
                    !orders.SelectTreatment(diseaseId, CareAuthor.Player, GameClock.NowMilliseconds, orders.CareRevision))
                {
                    return;
                }
                admission.NextNurseVisitAt = GameClock.NowMilliseconds;
                workers?.PlayerTakesPatient(admission);
                string label = DiseaseCatalog.Treatments.First(disease => disease.Id == diseaseId).TreatmentLabel;
                Game.DisplayNotification(admission.Name + " — tratamento selecionado: " + label + ".");
                Game.LogTrivial("[Lumen Santé] Tratamento de " + admission.Name + ": " + label);
            };
            patientMenu.PatientInteraction += orders =>
            {
                if (orders != null && CanInteractWithPatient() && shownCase?.Orders == orders)
                {
                    workers?.PlayerTakesPatient(shownCase);
                    orders.RegisterPlayerInteraction(GameClock.NowMilliseconds);
                }
            };
            bool RequestPatientExam(PatientCase admission, ExamOrder order, bool requestedByPlayer, CareAuthor doctor = null)
            {
                bool readyForRequest = admission.Location == PatientLocation.Chair ||
                    admission.Location == PatientLocation.WalkingToInpatientBed ||
                    admission.Location == PatientLocation.InpatientBed ||
                    admission.Location == PatientLocation.UndergoingExam;
                if (admission.Deceased || admission.Orders.IsRecovered || admission.Orders.IsDischarged || !readyForRequest ||
                    admission.Ped == null || !admission.Ped.Exists() || !admission.Ped.IsAlive) return false;
                int index = Array.IndexOf(admission.Orders.Items, order);
                if (index >= 0 && (requestedByPlayer ? order.TryRequest(admission.SeverityIndex + 1) :
                    order.TryRequestByNamedWorker(admission.SeverityIndex + 1, doctor)))
                {
                    pendingExams.Enqueue((admission, order));
                    RefreshExamStatus(admission);
                    Game.LogTrivial("[Lumen Santé] Exame solicitado para " + admission.Name + ": " + order.Id + ".");
                    return true;
                }
                return false;
            }

            void RefreshExamStatus(PatientCase admission)
            {
                hospitalComputer.RefreshExams(admission.Orders);
            }

            void StartNextExam()
            {
                int pendingAttempts = pendingExams.Count;
                while (pendingAttempts-- > 0)
                {
                    var request = pendingExams.Peek();
                    PatientCase admission = request.Patient;
                    if (admission == visitedPatient || workers?.IsPatientReserved(admission) == true)
                    {
                        pendingExams.Enqueue(pendingExams.Dequeue());
                        continue;
                    }
                    if (admission.Deceased || admission.Orders.IsDischarged || admission.Ped == null ||
                        !admission.Ped.Exists() || !admission.Ped.IsAlive)
                    {
                        pendingExams.Dequeue();
                        request.Order.Cancel();
                        RefreshExamStatus(admission);
                        continue;
                    }
                    if (admission.Location != PatientLocation.InpatientBed)
                    {
                        pendingExams.Enqueue(pendingExams.Dequeue());
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
                        RefreshExamStatus(admission);
                        if (request.Order.RequestedByPlayer) Game.DisplayNotification("O exame não pôde ser iniciado.");
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
                        RefreshExamStatus(admission);
                    }
                }
                if (result == ExamProcedure.Result.Running)
                {
                    return;
                }

                long completedAt = GameClock.NowMilliseconds - hospitalStartedAt;
                foreach (ExamOrder order in activeOrders)
                {
                    if (result == ExamProcedure.Result.Completed && order.TryComplete(completedAt))
                    {
                        order.RecordPerformer(procedure.NurseName);
                        Game.LogTrivial("[Lumen Santé] Exame realizado para " + admission.Name + ": " + order.Id + " por " + procedure.NurseName + ".");
                    }
                    else
                    {
                        order.Cancel();
                    }
                    RefreshExamStatus(admission);
                }
                activeOrders.Clear();
                procedure = null;
                activeExam = default((PatientCase, ExamOrder));
            }

            var deceasedMenu = new UIMenu("Paciente", "Central Los Santos Medical Center");
            var coronersAction = MenuPalette.Item("Chamar os Coroners", ActionTone.Accent);
            deceasedMenu.AddItem(coronersAction);
            menuPool.Add(deceasedMenu);

            coronersAction.Activated += (sender, item) =>
            {
                if (!coronersAction.Enabled || !current.Deceased || current.Ped == null || !current.Ped.Exists())
                {
                    return;
                }

                MenuPalette.SetEnabled(coronersAction, false, ActionTone.Accent);
                coronersAction.Text = "Coroners chamados";
                workers?.PlayerTakesPatient(current);
                current.CoronersCalled = true;
                current.CoronerPickupAt = GameClock.NowMilliseconds + GameClock.Minutes(30);
                current.Orders.RecordCoronerRequest(CareAuthor.Player, GameClock.NowMilliseconds);
                string location = current.Orders?.Room ?? "Recepção";
                Game.DisplayNotification("Coroners avisados para " + location + ".");
                Game.LogTrivial("[Lumen Santé] Coroners chamados: " + current.Name + ", " + location + ".");
            };

            void RegisterDeath()
            {
                current.Escort?.Release();
                current.Escort = null;
                current.ConsultationChairWalk?.Clear();
                current.ConsultationChairWalk = null;
                current.SeverityClockRunning = false;
                current.Deceased = true;
                if (current.Appointment != null)
                {
                    current.Appointment.State = MedicalAgenda.Status.Completed;
                    current.Appointment = null;
                    SaveAgenda();
                }
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
                Game.LogTrivial("[Lumen Santé] Óbito: " + current.Name + ", " + location + ".");
            }

            void UpdatePatientBlip()
            {
                if (!IsAccessible(current))
                {
                    if (current.Blip != null && current.Blip.Exists()) current.Blip.Delete();
                    current.Blip = null;
                    return;
                }
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

            void ClearAdmission(PatientCase admission)
            {
                if (admission.Ped != null) receptionCases.Remove(admission.Ped);
                if (admission.Blip != null && admission.Blip.Exists()) admission.Blip.Delete();
                admission.Blip = null;
                admission.Escort?.Release();
                admission.Escort = null;
                admission.ConsultationChairWalk?.Clear();
                admission.ConsultationChairWalk = null;
                if (shownCase == admission)
                {
                    BindPatient(null);
                    patientMenu.Close();
                    deceasedMenu.Visible = false;
                }

                if (admission.InpatientRoomIndex >= 0)
                {
                    inpatients[admission.InpatientRoomIndex] = null;
                    RefreshPatientList();
                }
                else if (consultations[admission.DiagnosticRoomIndex] == admission)
                {
                    consultations[admission.DiagnosticRoomIndex] = null;
                }

                if (admission.Ped != null && admission.Ped.Exists())
                {
                    admission.Ped.Delete();
                }

                admission.Ped = null;
                if (admission.AgendaRecord && admission.Deceased && !dischargedCases.Contains(admission))
                {
                    dischargedCases.Add(admission);
                    RefreshPatientList();
                }
            }

            void UpdateTreatmentVisits()
            {
                if (nursingVisit != null)
                {
                    ExamProcedure.Result result = visitedPatient.Deceased || visitedPatient.Ped == null ||
                        !visitedPatient.Ped.Exists() ? ExamProcedure.Result.Cancelled : nursingVisit.Update();
                    if (result != ExamProcedure.Result.Running)
                    {
                        if (result == ExamProcedure.Result.Completed)
                            visitedPatient.Orders.RecordNurseProcedure(
                                CareAuthor.Nurse(nursingVisit.NurseId, nursingVisit.NurseName),
                                GameClock.NowMilliseconds, "Visita de tratamento");
                        nursingVisit.ReleaseNurse();
                        nursingVisit = null;
                        visitedPatient.NextNurseVisitAt = GameClock.NowMilliseconds + GameClock.Minutes(random.Next(30, 61));
                        visitedPatient = null;
                    }
                    return;
                }
                PatientCase patient = inpatients.FirstOrDefault(admission => admission != null && !admission.Deceased &&
                    workers?.IsPatientReserved(admission) != true &&
                    !admission.Orders.IsRecovered && admission.Orders.TreatmentDiseaseId != null && admission.Ped != null && admission.Ped.Exists() &&
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
                foreach (PatientCase discharged in dischargedCases)
                {
                    discharged.DischargeWalk?.Clear();
                    if (discharged.Ped != null && discharged.Ped.Exists()) discharged.Ped.Delete();
                }
                dischargedCases.Clear();
                nursingVisit?.ReleaseNurse();
                nursingVisit = null;
                procedure?.ReleaseNurse();
                procedure = null;
                activeExam = default((PatientCase, ExamOrder));
                pendingExams.Clear();
                foreach (ExamOrder order in activeOrders) order.Cancel();
                activeOrders.Clear();
                foreach (PatientCase admission in cases.ToArray())
                {
                    if (admission != null)
                    {
                        current = admission;
                        ClearAdmission(current);
                    }
                }

                current = emptyCase;
            }

            void ClosePlayerInteraction()
            {
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
                    workers?.CancelPatient(current);
                    if (current.AwaitingBed && current.Location == PatientLocation.WaitingReception)
                    {
                        if (!current.Deceased && !waitingRoom.CanReceiveReturningPatient) return;
                        Point reception = HospitalStaffRoutes.PatientArrivalIdle;
                        replacement = new Ped(current.Model, reception.Position, reception.Heading);
                        current.Appearance?.Apply(replacement);
                        replacement.IsPersistent = true;
                        replacement.BlockPermanentEvents = true;
                        if (current.Deceased) replacement.Health = 0;
                        else if (!waitingRoom.ReceiveAtReception(replacement))
                        {
                            replacement.Delete();
                            return;
                        }
                        foreach (Ped previous in receptionCases.Where(pair => pair.Value == current).Select(pair => pair.Key).ToArray())
                            receptionCases.Remove(previous);
                        current.Ped = replacement;
                        receptionCases[replacement] = current;
                        RefreshPatientList();
                        return;
                    }
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
                    current.ConsultationChairWalk?.Clear();
                    current.ConsultationChairWalk = null;
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
                        if (current.TransferRequestedByPlayer) Game.DisplayNotification(current.Name + " foi internado no " + room.Name + ".");
                        current.TransferRequestedByPlayer = false;
                        RefreshPatientList();
                    }
                    if (current.Deceased)
                    {
                        replacement.IsPositionFrozen = false;
                        replacement.Health = 0;
                    }
                    else if (!current.HasConsulted)
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

            void ToggleShift(Ped player, bool notifyPlayer = true)
            {
                bool starting = shiftPlayer == null;
                if (starting && (player == null || !player.Exists() || !player.IsAlive))
                {
                    return;
                }
                if (starting && (doctors.IsOpen || !doctors.CanStartShift(player)))
                {
                    if (notifyPlayer) Game.DisplayNotification("Defina um médico padrão e ative um médico pelo menu Médicos (F9).");
                    return;
                }

                shiftPlayer = starting ? player : null;
                if (!starting)
                {
                    doctors.Close();
                    ClosePlayerInteraction();
                }
                doctors.ApplyShiftOutfit(starting);

                shiftAction.Text = starting ? "Encerrar plantão" : "Iniciar plantão";
                string message = starting ? "Plantão iniciado." : "Plantão encerrado.";
                if (notifyPlayer) Game.DisplayNotification(message);
                Game.LogTrivial("[Lumen Santé] " + message);
                menu.Visible = false;
            }

            shiftAction.Activated += (sender, item) => ToggleShift(Game.LocalPlayer.Character);

            doctors = new DoctorMenu(menuPool, doctorProfiles, () => shiftPlayer != null, ArchiveDoctor);
            workers = new WorkerCoordinator(routes.Path, population, waitingRoom, consultations, doctorAccess,
                inpatientRoomDestinations,
                () => cases.Where(patient => patient != null).ToArray(), CallPatientToRoom, AdmitPatient, DischargePatient,
                (admission, order, author) => RequestPatientExam(admission, order, false, author), ReturnToWaiting,
                admission => admission == activeExam.Patient || admission == visitedPatient ||
                    (shownCase == admission && patientMenu.IsOpen) ||
                    (hospitalComputer.IsOpen && consultations[doctorRoomIndex] == admission),
                index => hospitalComputer.IsOpen && doctorRoomIndex == index,
                RefreshPatientList, random);

            var shiftBlip = new Blip(ShiftPoint);
            waitingRoom.PatientReady += NotifyScheduledArrival;
            try
            {
                shiftBlip.Name = "Lumen Santé - Plantão";
                shiftBlip.Sprite = BlipSprite.Hospital;

                while (true)
                {
                    GameFiber.Yield();
                    if (phoneShortcut.Update()) RequestHospitalWebsite(true);
                    if (websiteOpenStage != 0 && ((!websiteOpenFromPhone && !hospitalComputer.IsUsingNativeBrowser) ||
                        websiteClock.ElapsedMilliseconds - websiteOpenStarted >= 10000))
                    {
                        if (websiteOpenFromPhone || hospitalComputer.IsUsingNativeBrowser)
                            Game.DisplayNotification("Não foi possível abrir o site do hospital no navegador.");
                        Game.LogTrivial("[Lumen Santé] Abertura do navegador expirou na etapa " + websiteOpenStage +
                            " (celular: " + websiteOpenFromPhone + ").");
                        NativeFunction.Natives.SET_SCRIPT_AS_NO_LONGER_NEEDED("appinternet");
                        websiteOpenStage = 0;
                    }
                    if (websiteOpenStage == 1 && NativeFunction.Natives.HAS_SCRIPT_LOADED<bool>("appinternet"))
                    {
                        if (NativeFunction.Natives.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH<int>(internetScript) == 0)
                            NativeFunction.Natives.START_NEW_SCRIPT<int>("appinternet", 4592);
                        NativeFunction.Natives.SET_SCRIPT_AS_NO_LONGER_NEEDED("appinternet");
                        websiteOpenStage = 2;
                    }
                    bool internetOpen = NativeFunction.Natives.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH<int>(internetScript) > 0;
                    if (!internetOpen && hospitalBrowserHandle != 0)
                    {
                        ReleaseHospitalBrowser();
                        nextWebsiteUpdate = 0;
                        websitePatientIds = Array.Empty<Guid>();
                        websiteCareRevisions = Array.Empty<long>();
                        NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(19);
                        NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(18);
                        NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(20);
                        websiteSelectedPatient = Guid.Empty;
                        websiteAgendaIds = Array.Empty<Guid>();
                        NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(17);
                    }
                    int hospitalPage = NativeFunction.CallByHash<int>(0x01A358D9128B7A86UL);
                    if (websiteOpenStage >= 2 && internetOpen)
                    {
                        if (hospitalBrowserHandle == 0)
                            hospitalBrowserHandle = NativeFunction.Natives.REQUEST_SCALEFORM_MOVIE<int>("web_browser");
                        if (hospitalBrowserHandle != 0 && NativeFunction.Natives.HAS_SCALEFORM_MOVIE_LOADED<bool>(hospitalBrowserHandle))
                        {
                            bool eyefindLoaded = NativeFunction.CallByHash<int>(0x97D47996FC48CBADUL) == 2;
                            if (!eyefindLoaded && websiteOpenStage == 2 &&
                                NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(hospitalBrowserHandle, "GO_TO_WEBPAGE"))
                            {
                                NativeFunction.Natives.BEGIN_TEXT_COMMAND_SCALEFORM_STRING("STRING");
                                NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME("WWW_EYEFIND_INFO");
                                NativeFunction.Natives.END_TEXT_COMMAND_SCALEFORM_STRING();
                                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                                websiteOpenStage = 3;
                            }
                            else if (eyefindLoaded && NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(hospitalBrowserHandle, "SET_DATA_SLOT"))
                            {
                                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(93);
                                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(websiteOpenSequence);
                                NativeFunction.Natives.BEGIN_TEXT_COMMAND_SCALEFORM_STRING("STRING");
                                NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(
                                    websiteOpenPatient == Guid.Empty ? "" : websiteOpenPatient.ToString("N"));
                                NativeFunction.Natives.END_TEXT_COMMAND_SCALEFORM_STRING();
                                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(websiteOpenPage);
                                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                                if (NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(hospitalBrowserHandle, "GO_TO_WEBPAGE"))
                                {
                                    string address = websiteOpenPage == 8 ? "WWW_EYEFIND_INFO_S_PACIENTES" :
                                        websiteOpenPage == 9 ? "WWW_EYEFIND_INFO_S_AGENDA" : "WWW_EYEFIND_INFO_S_LUMEN";
                                    NativeFunction.Natives.BEGIN_TEXT_COMMAND_SCALEFORM_STRING("STRING");
                                    NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(address);
                                    NativeFunction.Natives.END_TEXT_COMMAND_SCALEFORM_STRING();
                                    NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                                    websiteOpenPatientPending = websiteOpenPage == 8;
                                    websiteOpenStage = 0;
                                }
                            }
                        }
                    }
                    bool hospitalWebsiteActive =
                        internetOpen &&
                        NativeFunction.CallByHash<int>(0x97D47996FC48CBADUL) == 2 &&
                        (hospitalPage == 7 || hospitalPage == 8 || hospitalPage == 9);
                    if (hospitalWebsiteActive && websiteOpenStage == 0) lastHospitalPage = hospitalPage;
                    if (hospitalWebsiteActive && hospitalPage == 8 && NativeFunction.Natives.GET_GLOBAL_ACTIONSCRIPT_FLAG<int>(20) != 0)
                    {
                        NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(20);
                        lastHospitalPatient = Guid.Empty;
                    }
                    int activeWebsitePage = hospitalWebsiteActive ? hospitalPage : -1;
                    if (activeWebsitePage != previousWebsitePage)
                    {
                        nextWebsiteUpdate = 0;
                        previousWebsitePage = activeWebsitePage;
                    }
                    bool agendaPageOpen = hospitalWebsiteActive && hospitalPage == 9;
                    if (agendaPageOpen && !websiteAgendaActive)
                        NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(17);
                    websiteAgendaActive = agendaPageOpen;
                    if (agendaPageOpen)
                    {
                        int request = NativeFunction.Natives.GET_GLOBAL_ACTIONSCRIPT_FLAG<int>(17);
                        if (request != 0)
                        {
                            NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(17);
                            int generation = request / 4096;
                            int index = request % 4096 - 1;
                            bool valid = generation == websiteAgendaGeneration && index >= 0 &&
                                index < websiteAgendaIds.Length && websiteAgendaOwner == agendaProfile &&
                                agendaProfile == doctors.ActiveId && agenda?.Available == true;
                            websiteAgendaFeedback = valid && CancelPendingReturn(websiteAgendaIds[index])
                                ? "Acompanhamento encerrado." : "Não foi possível gravar a alteração.";
                            nextWebsiteUpdate = 0;
                        }
                    }
                    if (websiteNotesPatient != Guid.Empty)
                    {
                        int keyboardState = NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>();
                        if (keyboardState != 0)
                        {
                            if (keyboardState == 1 && shiftPlayer != null &&
                                shiftPlayer == Game.LocalPlayer.Character && hospitalWebsiteActive)
                            {
                                PatientCase notesPatient = cases.Concat(dischargedCases).FirstOrDefault(candidate =>
                                    candidate?.Orders?.AdmissionId == websiteNotesPatient && IsAccessible(candidate));
                                IntPtr result = NativeFunction.CallByHash<IntPtr>(0x8362B09B91893647UL);
                                string notes = result == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(result);
                                if (notesPatient != null && notes != null && notesPatient.Orders.Notes == websiteNotesOriginal)
                                    notesPatient.Orders.Notes = notes;
                            }
                            websiteNotesPatient = Guid.Empty;
                            websiteNotesOriginal = null;
                            nextWebsiteUpdate = 0;
                        }
                    }
                    if (hospitalWebsiteActive && hospitalPage == 8)
                    {
                        int request = NativeFunction.Natives.GET_GLOBAL_ACTIONSCRIPT_FLAG<int>(19);
                        if (request != 0)
                        {
                            int action = NativeFunction.Natives.GET_GLOBAL_ACTIONSCRIPT_FLAG<int>(18);
                            NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(19);
                            NativeFunction.Natives.RESET_GLOBAL_ACTIONSCRIPT_FLAG(18);
                            int generation = request / 4096;
                            int index = request % 4096 - 1;
                            if (generation == websitePatientGeneration && index >= 0 && index < websitePatientIds.Length)
                            {
                                Guid patientId = websitePatientIds[index];
                                PatientCase patient = cases.FirstOrDefault(candidate =>
                                    candidate?.Orders?.AdmissionId == patientId && IsAccessible(candidate))
                                    ?? dischargedCases.FirstOrDefault(candidate =>
                                        candidate?.Orders?.AdmissionId == patientId && IsAccessible(candidate));
                                if (patient != null)
                                {
                                    websiteSelectedPatient = patientId;
                                    lastHospitalPatient = patientId;
                                }
                                if (action == 3 && patient != null && shiftPlayer != null &&
                                    shiftPlayer == Game.LocalPlayer.Character &&
                                    websiteNotesPatient == Guid.Empty)
                                {
                                    string originalNotes = patient.Orders.Notes ?? string.Empty;
                                    if (originalNotes.Length > 1023)
                                        Game.DisplayNotification("Anotação longa demais para editar no teclado do site.");
                                    else
                                    {
                                        websiteNotesPatient = patientId;
                                        websiteNotesOriginal = originalNotes;
                                        NativeFunction.Natives.DISPLAY_ONSCREEN_KEYBOARD(1, "FMMC_KEY_TIP8", "",
                                            websiteNotesOriginal, "", "", "", 1024);
                                    }
                                }
                                if (action == 2 && patient != null && CanCallSelectedPatient(patient.Orders) &&
                                    CallPatientToRoom(doctorRoomIndex, patient.Ped))
                                    nextWebsiteUpdate = 0;
                                if (action >= 100 && action < 300 && patient != null && shiftPlayer != null &&
                                    shiftPlayer == Game.LocalPlayer.Character)
                                {
                                    int examIndex = action - 100;
                                    var entry = GetAccessiblePatients().FirstOrDefault(item => item.Orders == patient.Orders);
                                    if (entry.Orders != null && entry.CanRequestExams && !entry.Archived &&
                                        !patient.Orders.IsRecovered && !patient.Orders.IsDischarged &&
                                        examIndex < patient.Orders.Items.Length &&
                                        patient.Orders.Items[examIndex].Status == ExamStatus.Available)
                                    {
                                        workers?.PlayerTakesPatient(patient);
                                        RequestPatientExam(patient, patient.Orders.Items[examIndex], true);
                                    }
                                }
                                if (action >= 300 && action < 400 && patient != null &&
                                    shiftPlayer != null && shiftPlayer == Game.LocalPlayer.Character && !patient.Deceased &&
                                    patient.Location != PatientLocation.WaitingReception && !patient.Orders.IsDischarged &&
                                    patient.Orders.CareRevision == websiteCareRevisions[index])
                                {
                                    int medicationIndex = (action - 300) / 2;
                                    bool desiredActive = (action - 300) % 2 == 1;
                                    if (medicationIndex < MedicationCatalog.Options.Count)
                                    {
                                        var medication = MedicationCatalog.Options[medicationIndex].Class;
                                        bool active = patient.Orders.ActiveMedications(GameClock.NowMilliseconds).Contains(medication);
                                        bool changed = false;
                                        if (active && !desiredActive)
                                            changed = patient.Orders.StopMedication(medication, CareAuthor.Player,
                                                GameClock.NowMilliseconds, websiteCareRevisions[index]);
                                        else if (!active && desiredActive && !patient.Orders.IsRecovered)
                                            changed = patient.Orders.PrescribeMedication(medication,
                                                GameClock.NowMilliseconds + GameClock.Minutes(8 * 60),
                                                CareAuthor.Player, GameClock.NowMilliseconds, websiteCareRevisions[index]);
                                        if (changed) workers?.PlayerTakesPatient(patient);
                                    }
                                }
                                if (action >= 400 && patient != null && shiftPlayer != null &&
                                    shiftPlayer == Game.LocalPlayer.Character &&
                                    !patient.Deceased && !patient.Orders.IsDischarged && !patient.Orders.IsRecovered &&
                                    patient.Location != PatientLocation.WaitingReception &&
                                    patient.Orders.CareRevision == websiteCareRevisions[index])
                                {
                                    int treatmentIndex = action - 400;
                                    var treatments = DiseaseCatalog.Treatments.ToArray();
                                    if (treatmentIndex < treatments.Length && patient.Orders.SelectTreatment(
                                        treatments[treatmentIndex].Id, CareAuthor.Player,
                                        GameClock.NowMilliseconds, websiteCareRevisions[index]))
                                    {
                                        patient.NextNurseVisitAt = GameClock.NowMilliseconds;
                                        workers?.PlayerTakesPatient(patient);
                                    }
                                }
                                nextWebsiteUpdate = 0;
                            }
                            else if (action > 1)
                            {
                                Game.DisplayNotification("A lista de pacientes foi atualizada. Tente novamente.");
                                nextWebsiteUpdate = 0;
                            }
                        }
                    }
                    if (!hospitalWebsiteObserved &&
                        hospitalWebsiteActive)
                    {
                        hospitalWebsiteObserved = true;
                        Game.LogTrivial("[Lumen Santé] Navegador: página de teste do hospital detectada.");
                    }
                    if (hospitalWebsiteActive && websiteClock.ElapsedMilliseconds >= nextWebsiteUpdate)
                    {
                        nextWebsiteUpdate = websiteClock.ElapsedMilliseconds + 1000;
                        if (hospitalBrowserHandle == 0)
                            hospitalBrowserHandle = NativeFunction.Natives.REQUEST_SCALEFORM_MOVIE<int>("web_browser");
                        if (hospitalBrowserHandle != 0 &&
                            NativeFunction.Natives.HAS_SCALEFORM_MOVIE_LOADED<bool>(hospitalBrowserHandle))
                        {
                            bool sentAny = false;
                            bool sendFailed = false;
                            void SendWebsiteSlot(int slot, params object[] values)
                            {
                                if (sendFailed) return;
                                if (!NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(hospitalBrowserHandle, "SET_DATA_SLOT"))
                                {
                                    sendFailed = true;
                                    return;
                                }
                                NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(slot);
                                foreach (object value in values)
                                {
                                    if (value is int number)
                                        NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(number);
                                    else
                                    {
                                        NativeFunction.Natives.BEGIN_TEXT_COMMAND_SCALEFORM_STRING("STRING");
                                        NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME((string)value ?? string.Empty);
                                        NativeFunction.Natives.END_TEXT_COMMAND_SCALEFORM_STRING();
                                    }
                                }
                                NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                                sentAny = true;
                            }

                            var visiblePatients = shiftPlayer == null ? Array.Empty<BedManagementPanel.PatientEntry>()
                                : GetAccessiblePatients().OrderBy(patient => patient.Orders.PatientName).ToArray();
                            var patientIds = visiblePatients.Select(patient => patient.Orders.AdmissionId).ToArray();
                            var careRevisions = visiblePatients.Select(patient => patient.Orders.CareRevision).ToArray();
                            if (!websitePatientIds.SequenceEqual(patientIds) || !websiteCareRevisions.SequenceEqual(careRevisions))
                            {
                                websitePatientGeneration = websitePatientGeneration % 500000 + 1;
                                websitePatientIds = patientIds;
                                websiteCareRevisions = careRevisions;
                            }
                            var accessibleIds = new HashSet<Guid>(patientIds);
                            if (hospitalPage == 8 && websiteOpenPatientPending)
                                SendWebsiteSlot(93, websiteOpenSequence,
                                    websiteOpenPatient == Guid.Empty ? "" : websiteOpenPatient.ToString("N"), 8);
                            var roomPatient = consultations[doctorRoomIndex];
                            SendWebsiteSlot(94, shiftPlayer != null ? 1 : 0, DiagnosticRooms[doctorRoomIndex].Name,
                                roomPatient != null ? 1 : 0,
                                roomPatient != null && IsAccessible(roomPatient) ? roomPatient.Name : "");
                            for (int index = 0; index < visiblePatients.Length; index++)
                            {
                                var entry = visiblePatients[index];
                                var orders = entry.Orders;
                                SendWebsiteSlot(200 + index, entry.Archived ? 3 :
                                        entry.Unit == InpatientUnit.Ward ? 1 : entry.Unit == InpatientUnit.Room ? 2 : 0,
                                    orders.AdmissionId.ToString("N"), orders.PatientName ?? "", orders.Room ?? "",
                                    entry.Deceased ? "Óbito registrado" : orders.IsDischarged ? "Alta hospitalar" :
                                        orders.IsRecovered ? "Recuperado" : orders.IsInpatient ? "Internado" :
                                        orders.Room.StartsWith("Recepção", StringComparison.Ordinal) ? "Aguardando atendimento" : "Em consulta",
                                    orders.Profile?.Age ?? 0, orders.Profile?.BloodType ?? "", orders.Profile?.Phone ?? "",
                                    entry.Scheduled ? 1 : 0, orders.VisitCount, index,
                                    index < 4095 && CanCallSelectedPatient(orders) ? 1 : 0);
                            }

                            if (hospitalPage == 9)
                            {
                                bool agendaAvailable = agenda?.Available == true &&
                                    agendaNow != default(DateTime) && agendaProfile == doctors.ActiveId;
                                var appointments = agendaAvailable
                                    ? agenda.Appointments.Where(item => item.State != MedicalAgenda.Status.Cancelled)
                                        .OrderBy(item => item.Due).ToArray()
                                    : Array.Empty<MedicalAgenda.Appointment>();
                                var appointmentIds = appointments.Select(item => item.Id).ToArray();
                                if (websiteAgendaOwner != agendaProfile || !websiteAgendaIds.SequenceEqual(appointmentIds))
                                {
                                    if (websiteAgendaOwner != agendaProfile) websiteAgendaFeedback = "";
                                    websiteAgendaGeneration = websiteAgendaGeneration % 500000 + 1;
                                    websiteAgendaIds = appointmentIds;
                                    websiteAgendaOwner = agendaProfile;
                                }
                                for (int index = 0; index < appointments.Length; index++)
                                {
                                    var item = appointments[index];
                                    SendWebsiteSlot(5000 + index, item.Id.ToString("N"), item.PatientId.ToString("N"), item.Name ?? "",
                                        (item.Date.Date - agendaNow.Date).Days,
                                        item.Date.ToString("ddd dd/MM · HH:mm", CultureInfo.GetCultureInfo("pt-BR")),
                                        (int)item.State, item.FollowUp ? 1 : 0, index,
                                        accessibleIds.Contains(item.PatientId) ? 1 : 0);
                                }
                                SendWebsiteSlot(95, agendaAvailable ? 1 : 0, agendaProfile.ToString("N"),
                                    agendaAvailable ? agendaNow.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) : "",
                                    agendaAvailable ? ((int)agendaNow.DayOfWeek + 6) % 7 : 0,
                                    appointments.Length, websiteAgendaGeneration, websiteAgendaFeedback ?? "");
                            }
                            var selectedPatient = hospitalPage == 8 ? visiblePatients.FirstOrDefault(entry =>
                                entry.Orders.AdmissionId == websiteSelectedPatient) : default(BedManagementPanel.PatientEntry);
                            if (selectedPatient.Orders != null)
                            {
                                var orders = selectedPatient.Orders;
                                string patientId = orders.AdmissionId.ToString("N");
                                var reports = new Dictionary<ExamOrder, ExamReport>();
                                for (int index = 0; index < orders.Items.Length; index++)
                                {
                                    var exam = orders.Items[index];
                                    var report = exam.Status == ExamStatus.Completed ? new ExamReport(orders, exam) : null;
                                    if (report != null) reports[exam] = report;
                                    SendWebsiteSlot(1000 + index, patientId, exam.Name ?? "", (int)exam.Status,
                                        report?.Findings ?? "", report?.Method ?? "", report?.Unit ?? "",
                                        report?.Physician ?? "", report?.PerformedBy ?? "", report?.Protocol ?? "", (int)exam.Kind);
                                }
                                var history = orders.SymptomHistory(DiseaseCatalog.Find(orders.DiseaseId))
                                    .Select(item => (Title: item.Prompt, Detail: item.Answer))
                                    .Concat(orders.Items.Where(exam => exam.Status == ExamStatus.Completed)
                                        .OrderByDescending(exam => exam.CompletedAtMilliseconds)
                                        .Select(exam => (Title: exam.Name + " · Realizado por " +
                                            (exam.PerformedBy ?? "Enfermagem"), Detail: reports[exam].Findings)))
                                    .ToArray();
                                for (int index = 0; index < history.Length; index++)
                                    SendWebsiteSlot(2000 + index, patientId, history[index].Title ?? "", history[index].Detail ?? "");
                                var treatments = DiseaseCatalog.Treatments.ToArray();
                                for (int index = 0; index < treatments.Length; index++)
                                    SendWebsiteSlot(3000 + index, patientId, treatments[index].TreatmentLabel ?? "",
                                        treatments[index].Id == orders.TreatmentDiseaseId ? 1 : 0);
                                var activeMedications = orders.ActiveMedications(GameClock.NowMilliseconds);
                                for (int index = 0; index < MedicationCatalog.Options.Count; index++)
                                    SendWebsiteSlot(4000 + index, patientId, MedicationCatalog.Options[index].Name ?? "",
                                        activeMedications.Contains(MedicationCatalog.Options[index].Class) ? 1 : 0);
                                SendWebsiteSlot(96, patientId, orders.Items.Length,
                                    selectedPatient.CanRequestExams && !selectedPatient.Archived && !orders.IsRecovered && !orders.IsDischarged ? 1 : 0,
                                    orders.Notes ?? "", history.Length, treatments.Length, MedicationCatalog.Options.Count,
                                    !selectedPatient.Deceased && !selectedPatient.Archived && !orders.IsDischarged &&
                                        !orders.Room.StartsWith("Recepção", StringComparison.Ordinal) ? 1 : 0);
                            }
                            SendWebsiteSlot(97, 2601, shiftPlayer != null ? 1 : 0,
                                visiblePatients.Count(patient => !patient.Archived),
                                shiftPlayer == null ? 0 : cases.Count(patient => patient?.Orders != null &&
                                    IsAccessible(patient) && patient.Location == PatientLocation.WaitingReception),
                                visiblePatients.Length, websitePatientGeneration);
                            if (sendFailed)
                            {
                                nextWebsiteUpdate = websiteClock.ElapsedMilliseconds + 250;
                            }
                            else if (sentAny)
                            {
                                if (NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(hospitalBrowserHandle, "UPDATE_TEXT"))
                                {
                                    NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                                    if (hospitalPage == 8) websiteOpenPatientPending = false;
                                }
                                else
                                {
                                    nextWebsiteUpdate = websiteClock.ElapsedMilliseconds + 250;
                                }
                            }
                        }
                    }

                    doctors.ProcessCloseShortcut();
                    GameClock.Update();
                    if (Game.IsPaused)
                    {
                        if (waitingForSave) startupReadiness.Observe(false, 0, 0f, 0f, 0f, homeStartupClock.ElapsedMilliseconds);
                        if (startupFadeOwned)
                        {
                            homeStartupPending = false;
                            ReleaseStartupFade();
                        }
                        continue;
                    }

                    Ped player = Game.LocalPlayer.Character;
                    bool playerAvailable = player != null && player.Exists() && player.IsAlive;
                    if (startupFadeOwned)
                    {
                        if (startupFadeClock.ElapsedMilliseconds >= 45000)
                        {
                            ReleaseStartupFade();
                            Game.LogTrivial("[Lumen Santé] Tela de transição liberada após o limite; aguardando o médico para iniciar em casa.");
                        }
                        else if (NativeFunction.Natives.IS_SCREEN_FADING_IN<bool>() ||
                            NativeFunction.Natives.IS_SCREEN_FADED_IN<bool>())
                            NativeFunction.Natives.DO_SCREEN_FADE_OUT(0);
                    }
                    float simulationRadius = NpcRoute.BackgroundSimulation ? 120f : 160f;
                    NpcRoute.BackgroundSimulation = !playerAvailable ||
                        (player.Position - ShiftPoint).LengthSquared() > simulationRadius * simulationRadius;
                    hospitalSceneReady = playerAvailable && isHospitalSceneReady(player);
                    updateAmbient(player, playerAvailable, hospitalSceneReady);
                    SyncReceptionPatients();
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
                        ClosePlayerInteraction();
                        shiftAction.Text = "Iniciar plantão";
                        menu.Visible = false;
                        Game.LogTrivial("[Lumen Santé] Plantão interrompido.");
                    }

                    if (!playerAvailable && doctors.IsOpen) doctors.Close();
                    if (waitingForSave)
                    {
                        bool ready = !Game.IsLoading && playerAvailable && player.IsOnFoot &&
                            !Game.Console.IsOpen && !doctors.IsOpen &&
                            NativeFunction.Natives.IS_PLAYER_PLAYING<bool>(NativeFunction.Natives.PLAYER_ID<int>()) &&
                            NativeFunction.Natives.IS_PLAYER_CONTROL_ON<bool>(NativeFunction.Natives.PLAYER_ID<int>()) &&
                            !NativeFunction.Natives.IS_PLAYER_SWITCH_IN_PROGRESS<bool>() &&
                            !NativeFunction.Natives.IS_PAUSE_MENU_ACTIVE<bool>() &&
                            !NativeFunction.Natives.IS_CUTSCENE_ACTIVE<bool>() &&
                            !NativeFunction.Natives.GET_MISSION_FLAG<bool>() &&
                            NativeFunction.Natives.IS_SCREEN_FADED_IN<bool>() &&
                            NativeFunction.Natives.HAS_COLLISION_LOADED_AROUND_ENTITY<bool>(player);
                        Vector3 position = playerAvailable ? player.Position : Vector3.Zero;
                        if (startupReadiness.Observe(ready, playerAvailable ? player.Handle.GetHashCode() : 0,
                            position.X, position.Y, position.Z, homeStartupClock.ElapsedMilliseconds))
                        {
                            waitingForSave = false;
                            homeStartupClock.Restart();
                            if (homeStartupPending)
                            {
                                NativeFunction.Natives.DO_SCREEN_FADE_OUT(0);
                                startupFadeOwned = true;
                                startupFadeClock.Restart();
                            }
                            Game.LogTrivial("[Lumen Santé] Save estável; restaurando o médico padrão.");
                        }
                    }
                    if (!waitingForSave && playerAvailable && doctors.RestoreProfile(player))
                    {
                        player = Game.LocalPlayer.Character;
                        nextAgendaUpdate = 0;
                    }

                    if (!waitingForSave && homeStartupPending && homeStartupClock.ElapsedMilliseconds >= 60000)
                    {
                        homeStartupPending = false;
                        ReleaseStartupFade();
                        Game.LogTrivial("[Lumen Santé] Início em casa cancelado: o jogador não ficou disponível a tempo.");
                    }
                    if (!waitingForSave && homeStartupPending && playerAvailable && doctors.CanStartShift(player) &&
                        !Game.IsLoading && !Game.Console.IsOpen && !doctors.IsOpen &&
                        !NativeFunction.Natives.IS_CUTSCENE_ACTIVE<bool>() &&
                        !NativeFunction.Natives.GET_MISSION_FLAG<bool>() &&
                        NativeFunction.Natives.IS_PLAYER_CONTROL_ON<bool>(NativeFunction.Natives.PLAYER_ID<int>()))
                    {
                        homeStartupPending = false;
                        try
                        {
                            bool arrived = residence.StartAtHome(player, doctors.ActiveId, doctors.Active.ResidenceId, startupFadeOwned);
                            if (arrived && startupFadeOwned) NativeStartupIntro.Play(player, ReleaseStartupFade);
                        }
                        finally { ReleaseStartupFade(); }
                    }

                    bool manualToggle = consumeShiftToggle();
                    if (manualToggle)
                    {
                        ToggleShift(player);
                    }
                    if (consumeResidenceRequest())
                    {
                        if (shiftPlayer != null || !doctors.CanStartShift(player))
                            Game.DisplayNotification("Encerre o plantão e selecione um médico para acessar a residência.");
                        else
                            residence.Toggle(player, doctors.ActiveId, doctors.Active.ResidenceId);
                    }

                    bool doctorsDown = Game.IsKeyDown(Keys.F9);
                    bool doctorsPressed = doctorsDown && !doctorsWasDown;
                    doctorsWasDown = doctorsDown;
                    if (doctorsPressed && playerAvailable && !Game.Console.IsOpen)
                    {
                        if (waitingForSave)
                        {
                            waitingForSave = false;
                            homeStartupPending = false;
                            ReleaseStartupFade();
                            Game.LogTrivial("[Lumen Santé] Início automático cancelado pela abertura do menu de médicos.");
                        }
                        if (residence.Inside) Game.DisplayNotification("Saia da residência para trocar de médico ou de imóvel.");
                        else
                        {
                            ClosePlayerInteraction();
                            menu.Visible = false;
                            doctors.Toggle();
                        }
                    }

                    PatientCase interactionCase = current;
                    foreach (PatientCase admission in cases.ToArray())
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
                            if (playerAvailable || NpcRoute.BackgroundSimulation)
                            {
                                if (activeExam.Patient == current)
                                {
                                    procedure?.ReleaseNurse();
                                    procedure = null;
                                    foreach (ExamOrder order in activeOrders)
                                    {
                                        order.Cancel();
                                        RefreshExamStatus(current);
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
                                if (current.ConsultationRequestedByPlayer) Game.DisplayNotification(error.Message);
                                Game.LogTrivial("[Lumen Santé] Falha na chegada ao consultório: " + error.Message);
                            }
                        }


                        if (current.Location == PatientLocation.WalkingToInpatientBed && !current.Deceased && current.Ped.IsAlive)
                        {
                            UpdateInpatientTransfer();
                        }

                        if (current.SeverityClockRunning && (current.Location != PatientLocation.WaitingReception || current.AwaitingBed))
                        {
                            long now = GameClock.NowMilliseconds;
                            bool stageChanged = now >= current.NextSeverityAt;
                            if (stageChanged || now >= current.NextPulseRefresh)
                            {
                                if (stageChanged)
                                {
                                    int previousRisk = current.RiskLevel;
                                    current.RiskLevel = current.Disease.Evolve(current.RiskLevel,
                                        current.Orders.TreatmentDiseaseId == current.Disease.Id,
                                        current.Orders.ActiveMedications(now), random);
                                    if (current.RiskLevel == 0)
                                    {
                                        current.SeverityClockRunning = false;
                                        current.Orders.MarkRecovered();
                                        Game.LogTrivial("[Lumen Santé] " + current.Name + " se recuperou.");
                                        RefreshPatientList();
                                    }
                                    else if (previousRisk != current.RiskLevel && current.SeverityIndex < current.Disease.TerminalIndex)
                                    {
                                        string change = current.RiskLevel < previousRisk ? "melhorou" : "piorou";
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
                                    current.Vitals = stage.SampleVitals(random, current.RiskLevel == 0);
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
                    workers.Update();
                    foreach (PatientCase deceased in cases.Where(patient => patient?.Deceased == true && patient.CoronersCalled &&
                        GameClock.NowMilliseconds >= patient.CoronerPickupAt).ToArray())
                    {
                        if (activeExam.Patient == deceased || visitedPatient == deceased) continue;
                        workers.CancelPatient(deceased);
                        deceased.OutsideHospital = true;
                        ClearAdmission(deceased);
                        dischargedCases.Add(deceased);
                        RefreshPatientList();
                        Game.LogTrivial("[Lumen Santé] Coroners liberaram o espaço de " + deceased.Name + ".");
                    }
                    UpdateExamWork();
                    UpdateDischargedPatients(player, playerAvailable);
                    UpdateAgenda();
                    waitingRoom.Update(hospitalSceneReady);
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
                            if (!IsAccessible(admission) || admission.Ped == null || !admission.Ped.Exists())
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
                    residence.Update(player, doctors.ActiveId, doctors.Active?.ResidenceId, interactionPressed,
                        shiftPlayer == null && doctors.CanStartShift(player) && !doctors.IsOpen && !menu.Visible && !Game.Console.IsOpen);

                    if (!atReception)
                    {
                        menu.Visible = false;
                    }
                    MenuPalette.SetEnabled(callPatient, CanCallPatient(), ActionTone.Accent);
                    if (menu.Visible || atReception)
                    {
                        for (int index = 0; index < roomStatusItems.Length; index++)
                        {
                            roomStatusItems[index].RightLabel = consultations[index] != null ? "Ocupada" :
                                workers.IsComputerReserved(index) ? "Reservada" : "Livre";
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

                    if (!doctors.IsOpen && !menu.Visible && !patientMenu.IsOpen && !deceasedMenu.Visible &&
                        !hospitalComputer.IsOpen &&
                        (atReception || atDeceasedPatient || atComputer || atTable || atBed))
                    {
                        string hint = atDeceasedPatient || atTable || atBed ? $"Pressione E para interagir com {nearbyCase.Name}."
                            : atComputer ? "Pressione E para sentar na cadeira."
                            : doctorProfiles.Default == null ? "Pressione F9 para configurar seu médico."
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
                                MenuPalette.SetEnabled(coronersAction, !current.CoronersCalled, ActionTone.Accent);
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
                                patientMenu.OpenInpatient(current.Orders.Room, InpatientRooms[current.InpatientRoomIndex].Unit);
                            }
                            else if (atComputer)
                            {
                                workers.YieldComputer(nearbyComputer);
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
                            else if (doctorProfiles.Default != null)
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
                }
            }
            finally
            {
                ReleaseStartupFade();
                residence.Dispose();
                try { phoneShortcut.Dispose(); }
                catch (Exception error) { Game.LogTrivial("[Lumen Santé] Falha ao remover atalho do celular: " + error.Message); }
                ReleaseHospitalBrowser();
                waitingRoom.PatientReady -= NotifyScheduledArrival;
                workers.Clear();
                NpcRoute.BackgroundSimulation = false;
                UpdateDoctorAnimation(null, null, null);
                menu.Visible = false;
                doctors.Close();
                hospitalComputer.Close();
                SaveAgenda();
                ClearAllAdmissions();
                if (shiftBlip.Exists())
                {
                    shiftBlip.Delete();
                }
            }
        }
    }
}
