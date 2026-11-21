using System;
using System.Linq;
using Rage;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal static class HospitalUpperFloor
    {
        private static readonly Point FirstLanding = new Point("Primeiro vão da escada",
            new Vector3(344.190f, -1417.175f, 34.609f), 217.9f, passingDistance: 0.45f);
        private static readonly Point LandingCorner = new Point("Primeiro vão, segunda esquina",
            new Vector3(346.422f, -1414.841f, 34.612f), 50.4f, passingDistance: 0.45f);
        private static readonly Point StairTop = new Point("Pé da escada no segundo andar",
            new Vector3(341.854f, -1410.731f, 36.517f), 47.9f, passingDistance: 0.45f);
        private static readonly Point CanteenReception = new Point("Recepção da cantina",
            new Vector3(345.098f, -1405.945f, 36.516f), 318.1f);
        private static readonly Point CanteenReceptionTwo = new Point("Recepção da cantina II",
            new Vector3(347.359f, -1403.481f, 36.516f), 320.3f);
        private static readonly Point WardA = new Point("Ward A do segundo andar",
            new Vector3(349.263f, -1401.853f, 36.516f), 220.5f);
        private static readonly Point BoxesJunctionOne = new Point("Bifurcação I dos boxes",
            new Vector3(356.296f, -1407.469f, 36.516f), 197.6f);
        private static readonly Point RoomOneDoor = new Point("Porta Room I",
            new Vector3(359.522f, -1410.102f, 36.516f), 206.0f, isDoor: true);
        private static readonly Point RoomTwoDoor = new Point("Porta Room II",
            new Vector3(363.528f, -1413.817f, 36.516f), 230.0f, isDoor: true);
        private static readonly Point RoomThreeDoor = new Point("Porta Room III",
            new Vector3(367.994f, -1417.577f, 36.516f), 228.6f, isDoor: true);
        private static readonly Point BoxesJunctionTwo = new Point("Bifurcação II dos boxes",
            new Vector3(369.908f, -1419.224f, 36.516f), 225.9f);
        private static readonly Point WardB = new Point("Ward B do segundo andar",
            new Vector3(375.827f, -1411.906f, 36.516f), 310.0f);
        private static readonly Point BoxesJunctionThree = new Point("Bifurcação III dos boxes",
            new Vector3(378.344f, -1408.410f, 36.516f), 321.6f);
        private static readonly Point RoomFourDoor = new Point("Porta Room IV",
            new Vector3(369.996f, -1402.104f, 36.516f), 37.8f, isDoor: true);
        private static readonly (Point Door, Point Center)[] InpatientAccess =
        {
            (RoomOneDoor, new Point("Centro Room I", new Vector3(355.652f, -1414.844f, 36.516f), 140.8f)),
            (RoomTwoDoor, new Point("Centro Room II", new Vector3(360.643f, -1418.881f, 36.516f), 145.8f)),
            (RoomThreeDoor, new Point("Centro Room III", new Vector3(365.263f, -1422.010f, 36.516f), 133.7f)),
            (RoomFourDoor, new Point("Centro Room IV", new Vector3(372.902f, -1397.994f, 36.516f), 317.6f))
        };
        private static readonly Point OfficeTwoDoor = new Point("Porta Office II",
            new Vector3(366.954f, -1398.940f, 36.516f), 71.3f, isDoor: true);
        private static readonly Point OfficeTwoCenter = new Point("Centro do Office II",
            new Vector3(365.015f, -1401.961f, 36.516f), 148.6f);
        internal static readonly Point OfficeTwoStaffSeat = Point.Seated("Assento de funcionários do Office II",
            new Vector3(363.401f, -1404.274f, 36.016f), 322.6f,
            new[] { (HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip, 6000, true) });
        private static readonly Point BoxesJunctionFour = new Point("Bifurcação IV dos boxes",
            new Vector3(364.933f, -1397.326f, 36.516f), 11.0f);
        private static readonly Point Canteen = new Point("Cantina",
            new Vector3(358.081f, -1391.967f, 36.516f), 7.1f);

        private static readonly (Point Access, Vector3 Position, float Heading)[] PatientSeats =
        {
            (CanteenReception, new Vector3(344.407f, -1403.360f, 35.966f), 135.4f),
            (CanteenReception, new Vector3(343.907f, -1403.010f, 35.966f), 144.4f),
            (CanteenReception, new Vector3(343.357f, -1402.510f, 35.966f), 144.4f),
            (CanteenReception, new Vector3(342.957f, -1402.109f, 35.966f), 144.4f),
            (CanteenReception, new Vector3(342.557f, -1401.759f, 35.966f), 144.4f),
            (CanteenReceptionTwo, new Vector3(346.506f, -1401.659f, 35.966f), 144.4f),
            (CanteenReceptionTwo, new Vector3(346.106f, -1401.258f, 35.966f), 144.4f),
            (CanteenReceptionTwo, new Vector3(345.107f, -1400.458f, 35.966f), 144.4f),
            (CanteenReceptionTwo, new Vector3(344.707f, -1400.157f, 35.966f), 144.4f),
            (BoxesJunctionOne, new Vector3(355.715f, -1410.162f, 35.998f), 325.7f),
            (BoxesJunctionOne, new Vector3(356.115f, -1410.563f, 35.998f), 325.7f),
            (RoomThreeDoor, new Vector3(364.429f, -1417.399f, 35.966f), 330.2f),
            (RoomThreeDoor, new Vector3(364.829f, -1417.750f, 35.966f), 330.2f),
            (RoomThreeDoor, new Vector3(365.229f, -1418.100f, 35.966f), 330.2f),
            (BoxesJunctionTwo, new Vector3(368.905f, -1421.389f, 36.016f), 328.6f),
            (OfficeTwoCenter, new Vector3(364.687f, -1402.409f, 36.016f), 137.7f)
        };

        private static readonly (Point Access, Vector3 Position, float Heading)[] PatientIdles =
        {
            (WardA, new Vector3(348.431f, -1397.629f, 36.520f), 38.4f),
            (WardA, new Vector3(350.731f, -1397.100f, 36.520f), 230.3f),
            (Canteen, new Vector3(351.542f, -1393.180f, 36.520f), 1.3f),
            (Canteen, new Vector3(352.623f, -1392.136f, 36.520f), 64.3f),
            (CanteenReceptionTwo, new Vector3(347.809f, -1404.204f, 36.516f), 44.1f),
            (WardA, new Vector3(354.226f, -1398.080f, 36.516f), 43.4f)
        };

        private static readonly (Point Access, Vector3 Position, float Heading)[] GeneralIdles =
        {
            (Canteen, new Vector3(359.974f, -1390.333f, 36.516f), 139.1f),
            (BoxesJunctionFour, new Vector3(361.107f, -1396.867f, 36.516f), 319.0f)
        };

        internal static (Point[] Patient, Point[] General) Connect(HospitalPath path, Point stairBottom,
            Func<string, Vector3, float, Point> createSeat,
            (string Dictionary, string Clip, int Duration, bool Loop)[] standingIdles)
        {
            var waypoints = new[] { stairBottom, FirstLanding, LandingCorner, StairTop, CanteenReception,
                CanteenReceptionTwo, WardA, BoxesJunctionOne, RoomOneDoor, RoomTwoDoor, RoomThreeDoor,
                BoxesJunctionTwo, WardB, BoxesJunctionThree, RoomFourDoor, OfficeTwoDoor, BoxesJunctionFour, Canteen };
            for (int index = 1; index < waypoints.Length; index++)
            {
                path.ConnectBothWays(waypoints[index - 1], waypoints[index]);
            }
            path.ConnectBothWays(OfficeTwoDoor, OfficeTwoCenter);
            path.ConnectBothWays(OfficeTwoCenter, OfficeTwoStaffSeat);

            Point CreateIdle(string name, Vector3 position, float heading) =>
                new Point(name, position, heading, animationOptions: standingIdles);

            Point[] seats = ConnectAnchors(path, PatientSeats, "Assento de pacientes do segundo andar", createSeat);
            Point[] patientIdles = ConnectAnchors(path, PatientIdles, "Espera de pacientes do segundo andar", CreateIdle);
            Point[] generalIdles = ConnectAnchors(path, GeneralIdles, "Espera geral do segundo andar", CreateIdle);
            return (seats.Concat(patientIdles).ToArray(), generalIdles);
        }

        private static Point[] ConnectAnchors(HospitalPath path, (Point Access, Vector3 Position, float Heading)[] anchors,
            string label, Func<string, Vector3, float, Point> createAnchor)
        {
            var points = new Point[anchors.Length];
            for (int index = 0; index < anchors.Length; index++)
            {
                var anchor = anchors[index];
                points[index] = createAnchor(label + " " + (index + 1), anchor.Position, anchor.Heading);
                path.ConnectBothWays(anchor.Access, points[index]);
            }
            return points;
        }

        internal static Point AddInpatientRoomAccess(HospitalPath path, int index)
        {
            var room = InpatientAccess[index];
            path.ConnectBothWays(room.Door, room.Center);
            return room.Center;
        }
    }
}
