using System.Threading;
using System.Linq;
using System;
using Rage;
using Rage.Native;
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
        private static int residenceRequested;
        private const float NpcCleanupRadius = 300f;

        [ConsoleCommand(Name = "LumenSanteCasa", Description = "Testa a entrada e saída da residência do médico.")]
        public static void Command_LumenSanteCasa()
        {
            Interlocked.Exchange(ref residenceRequested, 1);
        }

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
            // Only a load observed during the game loading screen qualifies for home startup.
            // A manual load/reload during play must not move the player.
            bool startedDuringLoading = Game.IsLoading;
            while (Game.IsLoading) GameFiber.Yield();
            Game.AddConsoleCommands();
            Game.LogTrivial("[Lumen Santé] Plugin carregado. Comando: LumenSantePlantao.");
            Game.LogTrivial("[Lumen Santé] Carregamento inicial do jogo detectado: " + startedDuringLoading + ".");
            Game.LogTrivial("[Lumen Santé] Pasta da instalação: " + PluginPaths.InstallationDirectory);
            Game.LogTrivial("[Lumen Santé] Catálogo carregado: " + Clinical.DiseaseCatalog.Count + " doenças.");
            foreach (string message in Clinical.DiseaseCatalog.LoadDiagnostics)
                Game.LogTrivial("[Lumen Santé] Doenças: " + message);
            if (Clinical.DiseaseCatalog.Count == 0)
            {
                Game.DisplayNotification("Lumen Santé: adicione doenças válidas à pasta LumenSante/Diseases e recarregue o plugin.");
                return;
            }
            var staffNames = new Random();
            var receptionist = new HospitalReceptionist(HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip,
                employeeName: PatientNames.Pick(staffNames, true));
            var secondReceptionist = new HospitalReceptionist(HospitalShift.PatientChairAnimation, HospitalShift.PatientChairClip,
                employeeName: PatientNames.Pick(staffNames, true));
            var routes = new HospitalStaffRoutes();
            GameClock.Initialize();
            AmbientPopulation population = routes.CreatePopulation(PopulationSettings.Load());
            var waitingRoom = new WaitingRoom(routes);
            var calibration = new SeatCalibration(new[] { receptionist, secondReceptionist }
                .Concat(population.Actors).Concat(waitingRoom.Occupants));
            int hospitalInterior = 0;
            long sceneReadySince = -1;
            var sceneClock = System.Diagnostics.Stopwatch.StartNew();
            bool hospitalInteriorPinned = false;
            bool hospitalInteriorWasDisabled = false;
            bool hospitalInteriorWasCapped = false;
            bool hospitalPopulationCreated = false;
            bool IsHospitalAlreadyLoaded()
            {
                try
                {
                    Ped player = Game.LocalPlayer.Character;
                    if (player == null || !player.Exists() || !player.IsAlive) return false;
                    var reception = HospitalStaffRoutes.Reception.Position;
                    hospitalInterior = NativeFunction.Natives.GET_INTERIOR_AT_COORDS<int>(reception.X, reception.Y, reception.Z);
                    return hospitalInterior != 0 &&
                        NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>(player) == hospitalInterior &&
                        NativeFunction.Natives.IS_INTERIOR_READY<bool>(hospitalInterior) &&
                        NativeFunction.Natives.HAS_COLLISION_LOADED_AROUND_ENTITY<bool>(player);
                }
                catch (Exception error)
                {
                    Game.LogTrivial("[Lumen Santé] Não foi possível verificar o interior no startup: " + error.Message);
                    return false;
                }
            }
            bool LoadHospitalInterior()
            {
                var reception = HospitalStaffRoutes.Reception.Position;
                var spawn = HospitalStaffRoutes.PatientSpawn.Position;
                var timeout = System.Diagnostics.Stopwatch.StartNew();
                bool ready = false;
                bool sceneStarted = false;
                bool originalInteriorStateCaptured = false;
                try
                {
                    NativeFunction.Natives.SET_FOCUS_POS_AND_VEL(reception.X, reception.Y, reception.Z, 0f, 0f, 0f);
                    sceneStarted = NativeFunction.Natives.NEW_LOAD_SCENE_START_SPHERE<bool>(
                        reception.X, reception.Y, reception.Z, 100f, 0);
                    NativeFunction.Natives.REQUEST_COLLISION_AT_COORD(reception.X, reception.Y, reception.Z);
                    NativeFunction.Natives.REQUEST_COLLISION_AT_COORD(spawn.X, spawn.Y, spawn.Z);
                    while (timeout.ElapsedMilliseconds < 5000)
                    {
                        if (hospitalInterior == 0)
                            hospitalInterior = NativeFunction.Natives.GET_INTERIOR_AT_COORDS<int>(reception.X, reception.Y, reception.Z);
                        if (hospitalInterior != 0)
                        {
                            if (!hospitalInteriorPinned)
                            {
                                hospitalInteriorWasDisabled = NativeFunction.Natives.IS_INTERIOR_DISABLED<bool>(hospitalInterior);
                                hospitalInteriorWasCapped = NativeFunction.Natives.IS_INTERIOR_CAPPED<bool>(hospitalInterior);
                                originalInteriorStateCaptured = true;
                                NativeFunction.Natives.PIN_INTERIOR_IN_MEMORY(hospitalInterior);
                                hospitalInteriorPinned = true;
                            }
                            bool disabled = NativeFunction.Natives.IS_INTERIOR_DISABLED<bool>(hospitalInterior);
                            bool capped = NativeFunction.Natives.IS_INTERIOR_CAPPED<bool>(hospitalInterior);
                            if (disabled || capped)
                            {
                                NativeFunction.Natives.SET_INTERIOR_ACTIVE(hospitalInterior, true);
                                if (disabled) NativeFunction.Natives.DISABLE_INTERIOR(hospitalInterior, false);
                                if (capped) NativeFunction.Natives.CAP_INTERIOR(hospitalInterior, false);
                            }
                            NativeFunction.Natives.REFRESH_INTERIOR(hospitalInterior);
                            bool sceneLoaded = !sceneStarted || NativeFunction.CallByHash<bool>(0x01B8247A7A8B9AD1UL);
                            if (sceneLoaded && NativeFunction.Natives.IS_INTERIOR_READY<bool>(hospitalInterior))
                            {
                                ready = true;
                                break;
                            }
                        }
                        GameFiber.Yield();
                    }
                    Game.LogTrivial("[Lumen Santé] Pré-carregamento do interior do hospital: " +
                        (ready ? "pronto; interior=" + hospitalInterior : "limite de 5 s atingida" ) + ".");
                    return ready;
                }
                catch (Exception error)
                {
                    Game.LogTrivial("[Lumen Santé] Pré-carregamento do hospital falhou; a população será criada quando o interior estiver pronto: " + error.Message);
                    return false;
                }
                finally
                {
                    try
                    {
                        if (sceneStarted) NativeFunction.Natives.NEW_LOAD_SCENE_STOP();
                        NativeFunction.Natives.CLEAR_FOCUS();
                    }
                    catch (Exception cleanupError)
                    {
                        Game.LogTrivial("[Lumen Santé] Falha ao encerrar o foco de streaming do hospital: " + cleanupError.Message);
                    }
                    if (!ready && hospitalInteriorPinned)
                    {
                        try
                        {
                            if (originalInteriorStateCaptured && hospitalInteriorWasDisabled)
                                NativeFunction.Natives.DISABLE_INTERIOR(hospitalInterior, true);
                            if (originalInteriorStateCaptured && hospitalInteriorWasCapped)
                                NativeFunction.Natives.CAP_INTERIOR(hospitalInterior, true);
                            NativeFunction.Natives.UNPIN_INTERIOR(hospitalInterior);
                        }
                        catch (Exception cleanupError)
                        {
                            Game.LogTrivial("[Lumen Santé] Falha ao liberar o interior do hospital: " + cleanupError.Message);
                        }
                        hospitalInteriorPinned = false;
                        hospitalInterior = 0;
                    }
                }
            }
            void CreateHospitalPopulation()
            {
                if (hospitalPopulationCreated) return;
                receptionist.Create(new Vector3(356.373f, -1398.750f, 32.005f), 54.6f);
                secondReceptionist.Create(new Vector3(355.015f, -1400.685f, 32.055f), 27.3f);
                population.Populate();
                waitingRoom.Populate();
                hospitalPopulationCreated = true;
                Game.LogTrivial("[Lumen Santé] População do hospital criada com interior e colisão carregados.");
            }
            bool IsHospitalSceneReady(Ped player)
            {
                if (player == null || !player.Exists() || !player.IsAlive) return false;
                if (hospitalInterior == 0)
                {
                    var reception = HospitalStaffRoutes.Reception.Position;
                    hospitalInterior = NativeFunction.Natives.GET_INTERIOR_AT_COORDS<int>(reception.X, reception.Y, reception.Z);
                    if (hospitalInterior == 0) return false;
                }

                bool ready = NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>(player) == hospitalInterior &&
                    NativeFunction.Natives.IS_INTERIOR_READY<bool>(hospitalInterior) &&
                    NativeFunction.Natives.HAS_COLLISION_LOADED_AROUND_ENTITY<bool>(player);
                if (!ready)
                {
                    sceneReadySince = -1;
                    return false;
                }
                if (sceneReadySince < 0) sceneReadySince = sceneClock.ElapsedMilliseconds;
                return sceneClock.ElapsedMilliseconds - sceneReadySince >= 1000;
            }
            bool hospitalInteriorReadyAtStartup = IsHospitalAlreadyLoaded() || LoadHospitalInterior();
            if (!hospitalInteriorReadyAtStartup)
            {
                Game.LogTrivial("[Lumen Santé] Pré-carregamento não confirmou o interior; criando a população inicial e mantendo as rotas pausadas até o cenário ficar pronto.");
            }
            CreateHospitalPopulation();
            try
            {
                HospitalShift.Run(
                    routes,
                    waitingRoom,
                    population,
                    startedDuringLoading,
                    () => (Interlocked.Exchange(ref shiftToggleRequests, 0) & 1) != 0,
                    () => Interlocked.Exchange(ref residenceRequested, 0) != 0,
                    IsHospitalSceneReady,
                    (player, available, sceneReady) =>
                    {
                        if (sceneReady) CreateHospitalPopulation();
                        if (Interlocked.Exchange(ref npcCleanupRequested, 0) != 0 && ClearNearbyNpcs(player) > 0)
                        {
                            population.ScheduleArrival();
                            waitingRoom.ScheduleArrival();
                        }
                        calibration.Update(player, available && sceneReady,
                            Interlocked.Exchange(ref seatCalibrationRequested, 0) != 0);
                        bool nearHospital = available && player != null && player.Exists() &&
                            (player.Position - new Vector3(354.0f, -1397.0f, 32.0f)).LengthSquared() <= 250f * 250f &&
                            NativeFunction.Natives.HAS_COLLISION_LOADED_AROUND_ENTITY<bool>(player);
                        population.Update(sceneReady && !calibration.IsActive,
                            nearHospital && !calibration.IsActive);
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
                    if (hospitalInteriorPinned)
                    {
                        Ped player = Game.LocalPlayer.Character;
                        bool playerInside = player != null && player.Exists() &&
                            NativeFunction.Natives.GET_INTERIOR_FROM_ENTITY<int>(player) == hospitalInterior;
                        if (!playerInside && hospitalInteriorWasDisabled)
                            NativeFunction.Natives.DISABLE_INTERIOR(hospitalInterior, true);
                        if (!playerInside && hospitalInteriorWasCapped)
                            NativeFunction.Natives.CAP_INTERIOR(hospitalInterior, true);
                        NativeFunction.Natives.UNPIN_INTERIOR(hospitalInterior);
                        hospitalInteriorPinned = false;
                    }
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
