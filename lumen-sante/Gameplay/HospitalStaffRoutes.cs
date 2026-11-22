using System;
using System.Linq;
using Rage;
using lumen_sante.Interface;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class HospitalStaffRoutes
    {
        private static readonly (string Dictionary, string Clip, int Duration, bool Loop)[] StandingIdles =
        {
            ("amb@world_human_hang_out_street@male_a@base", "base", 6000, true),
            ("amb@world_human_hang_out_street@female_arms_crossed@base", "base", 6000, true),
            ("amb@world_human_stand_impatient@male@no_sign@base", "base", 6000, true),
            ("amb@world_human_hang_out_street@female_hold_arm@idle_a", "idle_a", 6000, true)
        };
        private static readonly (string Dictionary, string Clip, int Duration, bool Loop) Lean =
            ("amb@world_human_leaning@male@wall@back@hands_together@base", "base", 6000, true);
        private static readonly (string Dictionary, string Clip, int Duration, bool Loop)[] LeanAndStandingIdles =
            new[] { Lean }.Concat(StandingIdles).ToArray();

        internal static readonly Point Reception = new Point("Recepção", new Vector3(348.340f, -1401.184f, 32.505f), 225.1f);
        internal static readonly Point PatientSpawn = new Point("Entrada externa do hospital",
            new Vector3(339.228f, -1394.681f, 32.511f), 230f);
        internal static readonly Point HospitalDoor = new Point("Porta do hospital",
            new Vector3(343.482f, -1398.633f, 32.511f), 223.6f, isDoor: true);
        private static readonly Point HospitalDoorInside = new Point("Passagem após a porta do hospital",
            new Vector3(347.935f, -1402.048f, 32.505f), 284.6f, isDoor: true);
        private static readonly Point GroundFloorElevator = new Point("Elevador térreo",
            new Vector3(343.415f, -1405.691f, 32.505f), 298.8f);
        private static readonly Point StairwayOne = new Point("Escadaria I",
            new Vector3(339.118f, -1411.045f, 32.505f), 137.5f);
        private static readonly Point StairBottom = new Point("Pé da escada",
            new Vector3(339.947f, -1413.553f, 32.505f), 223.3f, passingDistance: 0.45f);
        private static readonly Point Junction = new Point("Bifurcação", new Vector3(355.882f, -1407.206f, 32.505f), 49.7f);
        private static readonly Point TriageDoor = new Point("Porta da Triagem", new Vector3(360.367f, -1411.297f, 32.505f), 188.0f, isDoor: true);
        private static readonly Point DiagnosticOneHallTurn = new Point("Curva de saída do Diagnóstico II",
            new Vector3(362.082f, -1412.659f, 32.505f), 137.4f, passingDistance: 0.55f);
        private static readonly Point DiagnosticLowerHallTurn = new Point("Curva inferior do corredor de Diagnóstico",
            new Vector3(358.548f, -1412.670f, 32.505f), 315.2f, passingDistance: 0.55f);
        private static readonly Point TriageCenter = new Point("Centro da Triagem",
            new Vector3(356.631f, -1416.374f, 32.505f), 235.6f);
        private static readonly Point TriageDesk = new Point("Posto da Triagem",
            new Vector3(354.709f, -1417.198f, 32.505f), 42.3f,
            animation: (HospitalComputer.ComputerAnimation, HospitalComputer.ComputerClip, 6000, true),
            seatedAnchor: new Vector3(355.079f, -1417.605f, 32.055f));
        private static readonly Point TriageLean = new Point("Lean de funcionários da Triagem",
            new Vector3(357.198f, -1418.946f, 32.505f), 227.3f, animationOptions: LeanAndStandingIdles);
        private static readonly Point ReceptionFront = new Point("Frente da recepção", new Vector3(353.179f, -1396.523f, 32.505f), 287.6f);
        internal static readonly Point PatientArrivalIdle = new Point("Espera inicial de atendimento",
            new Vector3(355.009f, -1397.545f, 32.505f), 225.2f, animationOptions: StandingIdles);
        private static readonly Point WardAFront = new Point("Frente da Ward A", new Vector3(357.495f, -1390.882f, 32.505f), 294.1f);
        private static readonly Point DoctorAReception = new Point("Recepção do Doctor A",
            new Vector3(359.291f, -1387.033f, 32.505f), 166.3f);
        private static readonly Point DoctorAFront = new Point("Frente do Doctor A", new Vector3(362.391f, -1386.179f, 32.505f), 208.9f, isDoor: true);
        private static readonly Point DoctorACenter = new Point("Centro do Doctor A", new Vector3(367.707f, -1390.513f, 32.505f), 303.2f);
        private static readonly Point DoctorAIdle = new Point("Espera de funcionários do Doctor A",
            new Vector3(371.190f, -1390.163f, 32.505f), 223.7f, animationOptions: StandingIdles);
        private static readonly Point DoctorASeatApproach = new Point("Aproximação da cadeira do Doctor A",
            new Vector3(367.711f, -1387.168f, 32.505f), 140.3f,
            animation: (HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip, 6000, true),
            seatedAnchor: new Vector3(368.062f, -1386.745f, 31.955f));
        private static readonly Point WardACorridor = new Point("Corredor Ward A", new Vector3(364.697f, -1397.206f, 32.505f), 221.6f);
        private static readonly Point WardBCorridorDoor = new Point("Porta Corredor Ward B",
            new Vector3(373.346f, -1404.661f, 32.505f), 225.5f, isDoor: true);
        private static readonly Point WardCCorridorDoor = new Point("Porta Corredor Ward C",
            new Vector3(377.406f, -1410.111f, 32.505f), 227.1f, isDoor: true);
        private static readonly Point RecoveryCorridorDoor = new Point("Porta Corredor Recovery Room",
            new Vector3(373.990f, -1413.880f, 32.505f), 161.8f, isDoor: true);
        private static readonly Point RecoveryCenter = new Point("Centro da Recovery Room",
            new Vector3(367.337f, -1408.547f, 32.505f), 320.7f);
        private static readonly Point RecoveryAnchor = new Point("Anchor da Recovery Room",
            new Vector3(364.742f, -1408.753f, 32.505f), 50.8f, animationOptions: LeanAndStandingIdles);
        private static readonly Point WardDCorridorDoor = new Point("Porta Corredor Ward D",
            new Vector3(370.618f, -1417.772f, 32.505f), 118.9f, isDoor: true);
        private static readonly Point DiagnosticOne = new Point("Corredor Diagnóstico II",
            HospitalShift.DiagnosticOneCorridor.Position, HospitalShift.DiagnosticOneCorridor.Heading, passingDistance: 0.55f);
        private static readonly Point DiagnosticTwo = new Point("Corredor Diagnóstico III",
            HospitalShift.DiagnosticTwoCorridor.Position, HospitalShift.DiagnosticTwoCorridor.Heading);
        private static readonly Point[] DiagnosticCorridors = { DiagnosticOne, DiagnosticTwo };
        private static readonly Point General = new Point("Acesso aos diagnósticos",
            HospitalShift.ConsultationRouteEntrance.Position, HospitalShift.ConsultationRouteEntrance.Heading);
        private static readonly Point SodaMachineOne = new Point("Máquina de Refrigerante I",
            new Vector3(342.256f, -1404.573f, 32.505f), 355.5f, animationOptions: StandingIdles);
        private static readonly Point SodaMachineTwo = new Point("Máquina de Refrigerante II",
            new Vector3(343.583f, -1402.846f, 32.505f), 135.5f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleOne = new Point("Espera de funcionários I",
            new Vector3(347.250f, -1404.635f, 32.505f), 45.7f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleTwo = new Point("Espera de funcionários II",
            new Vector3(352.590f, -1400.428f, 32.505f), 224.1f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleThree = new Point("Espera de funcionários III",
            new Vector3(354.490f, -1414.101f, 32.505f), 101.7f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleFour = new Point("Espera de funcionários IV",
            new Vector3(360.041f, -1390.688f, 32.505f), 107.3f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleFive = new Point("Espera de funcionários V",
            new Vector3(366.439f, -1406.289f, 32.505f), 319.0f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleSix = new Point("Espera de funcionários VI",
            new Vector3(367.613f, -1410.069f, 32.505f), 135.5f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleSeven = new Point("Espera de funcionários VII",
            new Vector3(371.630f, -1410.720f, 32.505f), 319.1f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleEight = new Point("Espera de funcionários VIII",
            new Vector3(377.298f, -1412.620f, 32.505f), 57.0f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleNine = new Point("Espera de funcionários IX",
            new Vector3(373.814f, -1407.338f, 32.505f), 328.5f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleTen = new Point("Espera de funcionários X",
            new Vector3(367.532f, -1401.951f, 32.505f), 234.2f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleEleven = new Point("Espera de funcionários XI",
            new Vector3(361.974f, -1396.411f, 32.505f), 79.7f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleTwelve = new Point("Espera de funcionários XII",
            new Vector3(360.929f, -1395.867f, 32.505f), 168.0f, animationOptions: StandingIdles);
        private static readonly Point StaffIdleThirteen = new Point("Espera de funcionários XIII",
            new Vector3(356.473f, -1401.265f, 32.505f), 223.8f, animationOptions: StandingIdles);
        private static readonly Point StaffLean = new Point("Lean de funcionários",
            new Vector3(358.547f, -1399.434f, 32.505f), 227.7f, animationOptions: LeanAndStandingIdles);
        private static readonly Point StaffLoungeCenter = new Point("Centro do Descanso Médico",
            new Vector3(353.520f, -1412.827f, 32.505f), 117.5f);
        private static readonly Point StaffLoungeSeat = new Point("Cadeira do Descanso Médico",
            new Vector3(351.575f, -1414.910f, 32.505f), 286.1f,
            animation: (HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip, 10000, true),
            seatedAnchor: new Vector3(351.047f, -1415.062f, 31.955f));
        private static readonly Point GeneralIdleOne = new Point("Espera geral I",
            new Vector3(354.987f, -1389.733f, 32.505f), 1.5f, animationOptions: StandingIdles);
        private static readonly Point GeneralIdleTwo = new Point("Espera geral II",
            new Vector3(356.303f, -1388.043f, 32.505f), 88.2f, animationOptions: StandingIdles);
        private static readonly (string Dictionary, string Clip, int Duration, bool Loop)[] PatientSeatAnimations =
            WaitingRoom.SeatedPoses.Select(pose => (pose.Dictionary, pose.Clip, 6000, true)).ToArray();
        private static readonly Point PatientSeatOne = PatientSeat("Assento de pacientes I",
            new Vector3(359.802f, -1406.081f, 31.955f), 48.3f);
        private static readonly Point PatientSeatTwo = PatientSeat("Assento de pacientes II",
            new Vector3(363.613f, -1401.591f, 31.955f), 46.6f);
        private static readonly Point PatientSeatThree = PatientSeat("Assento de pacientes III",
            new Vector3(370.567f, -1421.237f, 31.955f), 56.3f);
        private static readonly Point PatientSeatFour = PatientSeat("Assento de pacientes IV",
            new Vector3(360.779f, -1414.400f, 31.955f), 326.2f);
        private static readonly Point PatientSeatFive = PatientSeat("Assento de pacientes V",
            new Vector3(357.292f, -1411.317f, 31.987f), 321.8f);
        private static readonly Point PatientSeatSix = PatientSeat("Assento de pacientes VI",
            new Vector3(359.624f, -1408.367f, 31.955f), 141.1f);
        private static readonly Point PatientSeatSeven = PatientSeat("Assento de pacientes VII",
            new Vector3(361.650f, -1384.607f, 31.955f), 229.6f);
        private static readonly Point PatientSeatEight = PatientSeat("Assento de pacientes VIII",
            new Vector3(365.636f, -1387.561f, 31.955f), 46.9f);
        private static readonly Point PatientSeatNine = PatientSeat("Assento de pacientes IX",
            new Vector3(361.935f, -1389.761f, 31.941f), 320.7f);
        private static readonly Point PatientSeatTen = PatientSeat("Assento de pacientes X",
            new Vector3(357.121f, -1383.918f, 31.955f), 231.0f);
        private static readonly Point PatientIdle = new Point("Espera em pé de pacientes",
            new Vector3(355.464f, -1396.974f, 32.505f), 227.6f, animationOptions: StandingIdles);

        private readonly HospitalPath path = new HospitalPath();
        private readonly (Point[] Patient, Point[] General) upstairsAnchors;
        internal HospitalPath Path => path;
        internal HospitalExamRoutes ExamRoutes { get; }

        internal HospitalStaffRoutes()
        {
            ConnectBothWays(PatientSpawn, HospitalDoor);
            ConnectBothWays(HospitalDoor, HospitalDoorInside);
            ConnectBothWays(HospitalDoorInside, Reception);
            ConnectBothWays(ReceptionFront, PatientArrivalIdle);
            ConnectBothWays(TriageDoor, TriageCenter);
            ConnectBothWays(TriageCenter, TriageDesk);
            ConnectBothWays(TriageCenter, TriageLean);
            ConnectBothWays(TriageDoor, Junction);
            ConnectBothWays(DiagnosticOne, DiagnosticOneHallTurn);
            ConnectBothWays(DiagnosticOneHallTurn, DiagnosticLowerHallTurn);
            ConnectBothWays(DiagnosticLowerHallTurn, TriageCenter);
            ConnectBothWays(Junction, Reception);
            ConnectBothWays(Reception, GroundFloorElevator);
            ConnectBothWays(GroundFloorElevator, StairwayOne);
            ConnectBothWays(StairwayOne, StairBottom);
            upstairsAnchors = HospitalUpperFloor.Connect(path, StairBottom, PatientSeat, StandingIdles);
            ConnectBothWays(Reception, ReceptionFront);
            ConnectBothWays(ReceptionFront, PatientIdle);
            ConnectBothWays(ReceptionFront, WardAFront);
            ConnectBothWays(WardAFront, DoctorAReception);
            ConnectBothWays(DoctorAReception, DoctorAFront);
            ConnectBothWays(DoctorAFront, DoctorACenter);
            ConnectBothWays(DoctorACenter, DoctorASeatApproach);
            ConnectBothWays(DoctorACenter, DoctorAIdle);
            ConnectBothWays(WardAFront, WardACorridor);
            ConnectBothWays(WardACorridor, WardBCorridorDoor);
            ConnectBothWays(WardBCorridorDoor, WardCCorridorDoor);
            ConnectBothWays(WardCCorridorDoor, RecoveryCorridorDoor);
            ConnectBothWays(RecoveryCorridorDoor, RecoveryCenter);
            ConnectBothWays(RecoveryCenter, RecoveryAnchor);
            ConnectBothWays(RecoveryCorridorDoor, WardDCorridorDoor);
            ConnectBothWays(WardDCorridorDoor, DiagnosticTwo);
            ConnectBothWays(DiagnosticTwo, General);
            ConnectBothWays(General, Reception);
            ConnectBothWays(Reception, SodaMachineOne);
            ConnectBothWays(Reception, SodaMachineTwo);
            ConnectBothWays(Reception, StaffIdleOne);
            ConnectBothWays(Reception, StaffIdleTwo);
            ConnectBothWays(Junction, StaffLoungeCenter);
            ConnectBothWays(StaffLoungeCenter, StaffIdleThree);
            ConnectBothWays(StaffLoungeCenter, StaffLoungeSeat);
            ConnectBothWays(WardAFront, StaffIdleFour);
            ConnectBothWays(RecoveryCenter, StaffIdleFive);
            ConnectBothWays(RecoveryCenter, StaffIdleSix);
            ConnectBothWays(RecoveryCenter, StaffIdleSeven);
            ConnectBothWays(WardCCorridorDoor, StaffIdleEight);
            ConnectBothWays(WardBCorridorDoor, StaffIdleNine);
            ConnectBothWays(WardACorridor, StaffIdleTen);
            ConnectBothWays(WardACorridor, StaffIdleEleven);
            ConnectBothWays(WardACorridor, StaffIdleTwelve);
            ConnectBothWays(ReceptionFront, StaffIdleThirteen);
            ConnectBothWays(WardACorridor, StaffLean);
            ConnectBothWays(WardAFront, GeneralIdleOne);
            ConnectBothWays(WardAFront, GeneralIdleTwo);
            ConnectBothWays(Junction, PatientSeatOne);
            ConnectBothWays(WardACorridor, PatientSeatTwo);
            ConnectBothWays(DiagnosticTwo, PatientSeatThree);
            ConnectBothWays(TriageDoor, PatientSeatFour);
            ConnectBothWays(TriageDoor, PatientSeatFive);
            ConnectBothWays(TriageDoor, PatientSeatSix);
            ConnectBothWays(DoctorAFront, PatientSeatSeven);
            ConnectBothWays(DoctorACenter, PatientSeatEight);
            ConnectBothWays(DoctorAReception, PatientSeatNine);
            ConnectBothWays(DoctorAReception, PatientSeatTen);
            ExamRoutes = new HospitalExamRoutes(path, WardCCorridorDoor, WardBCorridorDoor);
        }

        internal Point AddConsultationChairAccess(int index, Vector3 position, float heading,
            (Vector3 Position, float Heading) inside)
        {
            var door = new Point("Acesso da cadeira " + index, inside.Position, inside.Heading);
            double radians = heading * Math.PI / 180;
            var approach = new Vector3(position.X - (float)Math.Sin(radians) * 0.65f,
                position.Y + (float)Math.Cos(radians) * 0.65f, inside.Position.Z);
            var chair = new Point("Cadeira da consulta " + index, approach, heading);
            ConnectBothWays(DiagnosticCorridors[index], door);
            ConnectBothWays(door, chair);
            return chair;
        }

        internal Point AddWaitingBench(int index, Vector3 position, float heading)
        {
            double radians = heading * Math.PI / 180;
            var approach = new Vector3(position.X - (float)Math.Sin(radians) * 0.65f,
                position.Y + (float)Math.Cos(radians) * 0.65f, Reception.Position.Z);
            var bench = new Point("Aproximação do banco " + (index + 1), approach, heading);
            Point access = (approach - Reception.Position).LengthSquared() < (approach - ReceptionFront.Position).LengthSquared()
                ? Reception : ReceptionFront;
            ConnectBothWays(access, bench);
            return bench;
        }

        internal Point AddDiagnosticTransferAccess(int roomIndex, string name, Vector3 table, Vector3 tableExit, float heading,
            (Vector3 Position, float Heading) inside)
        {
            var start = new Point("Maca da " + name, new Vector3(table.X, table.Y, tableExit.Z), heading);
            var exit = new Point("Aproximação da maca da " + name, tableExit, heading, passingDistance: 0.5f);
            var roomExit = new Point("Saída da " + name, inside.Position, inside.Heading, passingDistance: 0.55f);
            ConnectBothWays(start, exit);
            ConnectBothWays(exit, roomExit);
            ConnectBothWays(roomExit, DiagnosticCorridors[roomIndex]);
            return start;
        }

        internal AmbientPopulation CreatePopulation(PopulationSettings settings)
        {
            var publicAnchors = new[] { SodaMachineOne, SodaMachineTwo, GeneralIdleOne, GeneralIdleTwo }
                .Concat(upstairsAnchors.General).ToArray();
            var staffAnchors = new[] { TriageDesk, TriageLean, DoctorASeatApproach, RecoveryAnchor, StaffLoungeSeat,
                StaffIdleOne, StaffIdleTwo, StaffIdleThree, StaffIdleFour, StaffIdleFive, StaffIdleSix, StaffIdleSeven,
                StaffIdleEight, StaffIdleNine, StaffIdleTen, StaffIdleEleven, StaffIdleTwelve, StaffIdleThirteen,
                StaffLean, DoctorAIdle, HospitalUpperFloor.OfficeTwoStaffSeat };
            Point[] staffDestinations = staffAnchors.Concat(publicAnchors).ToArray();
            Point[] patientAnchors = new[] { PatientSeatOne, PatientSeatTwo, PatientSeatThree,
                PatientSeatFour, PatientSeatFive, PatientSeatSix, PatientSeatSeven, PatientSeatEight, PatientSeatNine,
                PatientSeatTen, PatientIdle }.Concat(upstairsAnchors.Patient).ToArray();
            Point[] patientDestinations = patientAnchors.Concat(publicAnchors).ToArray();

            var staffStarts = new[] { DoctorASeatApproach, HospitalUpperFloor.OfficeTwoStaffSeat,
                TriageDesk, TriageLean, StaffIdleOne, StaffIdleTwo, StaffIdleFour, DoctorAIdle };
            var patientStarts = new[] { SodaMachineOne, SodaMachineTwo, GeneralIdleOne, GeneralIdleTwo,
                PatientSeatOne, PatientSeatFive }.Concat(upstairsAnchors.Patient.Where((point, index) => index % 4 == 0));
            return new AmbientPopulation(path, settings, staffDestinations, patientDestinations,
                staffStarts.Concat(staffAnchors).Distinct().ToArray(),
                patientStarts.Concat(patientAnchors).Distinct().ToArray());
        }

        private static Point PatientSeat(string name, Vector3 position, float heading) =>
            Point.Seated(name, position, heading, PatientSeatAnimations);

        private void ConnectBothWays(Point first, Point second) => path.ConnectBothWays(first, second);
    }
}
