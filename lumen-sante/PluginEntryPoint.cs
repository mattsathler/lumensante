using System.Threading;
using System.Linq;
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

        public static void Main()
        {
            Game.AddConsoleCommands();
            Game.LogTrivial("[Lumen Santé] Plugin carregado. Comando: LumenSantePlantao.");
            var receptionist = new HospitalReceptionist();
            var secondReceptionist = new HospitalReceptionist();
            var routes = new HospitalStaffRoutes();
            AmbientPopulation population = routes.CreatePopulation(PopulationSettings.Load());
            var waitingRoom = new WaitingRoom(routes);
            var calibration = new SeatCalibration(new[] { receptionist, secondReceptionist }
                .Concat(population.Actors).Concat(waitingRoom.Occupants));
            try
            {
                receptionist.Create(new Vector3(356.363f, -1398.659f, 32.105f), 65.6f);
                secondReceptionist.Create(new Vector3(354.967f, -1400.479f, 32.155f), 38.7f);
                population.Populate();
                waitingRoom.Populate();
                HospitalShift.Run(
                    routes,
                    waitingRoom,
                    () => (Interlocked.Exchange(ref shiftToggleRequests, 0) & 1) != 0,
                    (player, available) =>
                    {
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

    }
}
