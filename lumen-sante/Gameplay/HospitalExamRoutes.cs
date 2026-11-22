using System;
using Rage;
using lumen_sante.Clinical;
using lumen_sante.Interface;
using Point = lumen_sante.Gameplay.HospitalPath.Point;

namespace lumen_sante.Gameplay
{
    internal sealed class HospitalExamRoutes
    {
        internal sealed class Station
        {
            internal Station(Point access, Vector3 position, float heading, string dictionary, string clip, bool elevated)
            {
                Position = position;
                Heading = heading;
                Dictionary = dictionary;
                Clip = clip;
                Elevated = elevated;
                Vector3 atFloor = new Vector3(position.X, position.Y, access.Position.Z);
                Vector3 towardAccess = access.Position - atFloor;
                float distance = (float)Math.Sqrt(towardAccess.LengthSquared());
                Vector3 approach = elevated && distance > 0f
                    ? atFloor + towardAccess * Math.Min(1f, 0.65f / distance) : position;
                Approach = new Point("Posto de exame a partir de " + access.Name, approach, heading, passingDistance: 1f);
            }

            internal Point Approach { get; }
            internal Vector3 Position { get; }
            internal float Heading { get; }
            internal string Dictionary { get; }
            internal string Clip { get; }
            internal bool Elevated { get; }
        }

        private static readonly Point ExamDoor = new Point("Porta da sala de exames",
            new Vector3(378.919f, -1410.975f, 32.505f), 222.9f, isDoor: true);
        private static readonly Point ExamHall = new Point("Acesso às salas de exames",
            new Vector3(383.387f, -1415.238f, 32.505f), 236.1f, passingDistance: 1.3f);
        private static readonly Point MriCenter = new Point("Centro da sala de MRI",
            new Vector3(379.003f, -1419.033f, 32.505f), 321.3f);
        private static readonly Point MriStaffRoom = new Point("Sala do funcionário da MRI",
            new Vector3(381.519f, -1420.748f, 32.505f), 236.1f, passingDistance: 1.3f);
        private static readonly Point XRayCenter = new Point("Centro da sala de Raio X",
            new Vector3(386.976f, -1410.059f, 32.505f), 139.4f);
        private static readonly Point XRayStaffRoom = new Point("Sala do funcionário do Raio X",
            new Vector3(388.768f, -1412.180f, 32.505f), 29.7f, passingDistance: 1.3f);
        private static readonly Point LabHall = new Point("Ward B, corredor do laboratório",
            new Vector3(379.354f, -1397.002f, 32.505f), 313.8f);
        private static readonly Point LabDoor = new Point("Porta do laboratório",
            new Vector3(387.686f, -1404.324f, 32.506f), 213.1f, isDoor: true);

        private readonly Station mriStaff = Computer(MriStaffRoom, new Vector3(382.263f, -1419.044f, 32.105f), 33.2f);
        private readonly Station mriPatient = Table(MriCenter, new Vector3(378.795f, -1416.209f, 33.349f), 228.7f);
        private readonly Station xRayStaff = Computer(XRayStaffRoom, new Vector3(387.343f, -1413.120f, 32.155f), 36.4f);
        private readonly Station xRayPatient = Table(XRayCenter, new Vector3(383.950f, -1410.430f, 33.127f), 229.8f);
        private readonly Station laboratory = new Station(LabDoor, new Vector3(386.635f, -1407.183f, 32.505f),
            233.8f, "missfbi5ig_20b", "do_experiment_idle_scientist", elevated: false);

        internal HospitalExamRoutes(HospitalPath path, Point examAccess, Point labAccess)
        {
            path.ConnectBothWays(examAccess, ExamDoor);
            path.ConnectBothWays(ExamDoor, ExamHall);
            path.ConnectBothWays(ExamHall, MriCenter);
            path.ConnectBothWays(MriCenter, MriStaffRoom);
            path.ConnectBothWays(MriCenter, mriPatient.Approach);
            path.ConnectBothWays(MriStaffRoom, mriStaff.Approach);
            path.ConnectBothWays(ExamHall, XRayCenter);
            path.ConnectBothWays(XRayCenter, XRayStaffRoom);
            path.ConnectBothWays(XRayCenter, xRayPatient.Approach);
            path.ConnectBothWays(XRayStaffRoom, xRayStaff.Approach);
            path.ConnectBothWays(labAccess, LabHall);
            path.ConnectBothWays(LabHall, LabDoor);
            path.ConnectBothWays(LabDoor, laboratory.Approach);
        }

        internal (Station Staff, Station Patient) For(ExamKind kind)
        {
            switch (kind)
            {
                case ExamKind.Mri: return (mriStaff, mriPatient);
                case ExamKind.XRay: return (xRayStaff, xRayPatient);
                case ExamKind.Laboratory: return (laboratory, null);
                default: return (null, null);
            }
        }

        private static Station Computer(Point access, Vector3 position, float heading) =>
            new Station(access, position, heading, HospitalComputer.ComputerAnimation, HospitalComputer.ComputerClip, elevated: true);

        private static Station Table(Point access, Vector3 position, float heading) =>
            new Station(access, position, heading, BedAnimations.Lounger.Dictionary, BedAnimations.Lounger.Clip, elevated: true);
    }
}
