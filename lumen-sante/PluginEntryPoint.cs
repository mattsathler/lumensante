using System.Threading;
using System.Linq;
using System;
using Rage;
using Rage.Attributes;
using lumen_sante.Gameplay;

[assembly: Plugin("Lumen Santé", Description = "Plantão no Central Los Santos Medical Center.")]

namespace lumen_sante
{
    public static class PluginEntryPoint
    {
        private static int shiftToggleRequests;
        private static int seatCalibrationRequested;
        private static int npcCleanupRequested;
        private const float NpcCleanupRadius = 300f;

        [ConsoleCommand(Name = "LumenSantePlantao", Description = "Inicia ou encerra o plantão do Lumen Santé.")]
        public static void Command_LumenSantePlantao()
        {
            Interlocked.Increment(ref shiftToggleRequests);
        }

        [ConsoleCommand(Name = "LumenSanteAssento", Description = "Abre ou fecha a calibração de um NPC sentado.")]
        public static void Command_LumenSanteAssento()
        {
            Interlocked.Exchange(ref seatCalibrationRequested, 1);
        }

        [ConsoleCommand(Name = "LumenSanteLimparNPCs", Description = "Remove todos os NPCs em um raio de 300 metros do jogador.")]
        public static void Command_LumenSanteLimparNPCs()
        {
            Interlocked.Exchange(ref npcCleanupRequested, 1);
        }

        public static void Main()
        {
            Game.AddConsoleCommands();
            Game.LogTrivial("[Lumen Santé] Plugin carregado. Comando: LumenSantePlantao.");
            Game.LogTrivial("[Lumen Santé] Pasta da instalação: " + PluginPaths.InstallationDirectory);
            Game.LogTrivial("[Lumen Santé] Catálogo carregado: " + Clinical.DiseaseCatalog.Count + " doenças.");
            foreach (string message in Clinical.DiseaseCatalog.LoadDiagnostics)
                Game.LogTrivial("[Lumen Santé] Doenças: " + message);
            if (Clinical.DiseaseCatalog.Count == 0)
            {
                Game.DisplayNotification("Lumen Santé: adicione doenças válidas à pasta LumenSante/Diseases e recarregue o plugin.");
                return;
            }
            var receptionist = new HospitalReceptionist(HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip);
            var secondReceptionist = new HospitalReceptionist(HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip);
            var routes = new HospitalStaffRoutes();
            GameClock.Initialize();
            AmbientPopulation population = routes.CreatePopulation(PopulationSettings.Load());
            var waitingRoom = new WaitingRoom(routes);
            var calibration = new SeatCalibration(new[] { receptionist, secondReceptionist }
                .Concat(population.Actors).Concat(waitingRoom.Occupants));
            try
            {
                receptionist.Create(new Vector3(356.373f, -1398.750f, 32.005f), 54.6f);
                secondReceptionist.Create(new Vector3(355.015f, -1400.685f, 32.055f), 27.3f);
                population.Populate();
                waitingRoom.Populate();
                HospitalShift.Run(
                    routes,
                    waitingRoom,
                    population,
                    () => (Interlocked.Exchange(ref shiftToggleRequests, 0) & 1) != 0,
                    (player, available) =>
                    {
                        if (Interlocked.Exchange(ref npcCleanupRequested, 0) != 0 && ClearNearbyNpcs(player) > 0)
                        {
                            population.ScheduleArrival();
                            waitingRoom.ScheduleArrival();
                        }
                        calibration.Update(player, available, Interlocked.Exchange(ref seatCalibrationRequested, 0) != 0);
                        population.Update(available && !calibration.IsActive);
                    });
            }
            finally
            {
                try
                {
                    calibration.Close();
                }
                finally
                {
                    receptionist.Clear();
                    secondReceptionist.Clear();
                    waitingRoom.Clear();
                    population.Clear();
                }
            }
        }

        private static int ClearNearbyNpcs(Ped player)
        {
            if (player == null || !player.Exists())
            {
                Game.DisplayNotification("Personagem indisponível para a limpeza de NPCs.");
                return 0;
            }

            Vector3 center = player.Position;
            int removed = 0;
            int failures = 0;
            foreach (Ped ped in World.GetAllPeds())
            {
                if (ped == null || ped == player || !ped.Exists() ||
                    (ped.Position - center).LengthSquared() > NpcCleanupRadius * NpcCleanupRadius)
                {
                    continue;
                }

                try
                {
                    ped.Delete();
                    removed++;
                }
                catch (Exception error)
                {
                    failures++;
                    Game.LogTrivial("[Lumen Santé] Falha ao remover NPC: " + error.Message);
                }
            }

            string result = $"Limpeza em {NpcCleanupRadius:0} m: {removed} NPCs removidos.";
            if (failures > 0)
            {
                result += $" Falhas: {failures}.";
            }
            Game.DisplayNotification(result);
            Game.LogTrivial("[Lumen Santé] " + result);
            return removed;
        }
    }
}
