---
name: lumen-sante-gameplay
description: Altera plantões, pacientes no mundo, rotas, NPCs, internação e leitos no Lumen Santé.
---

# Fluxos de gameplay

Localize o fluxo em `lumen-sante/Gameplay` por responsabilidade.

| Área | Arquivos iniciais |
|---|---|
| Entrada e plantão | `PluginEntryPoint.cs` na raiz do projeto, `HospitalShift.cs` |
| Espera e recepção | `WaitingRoom.cs`, `HospitalReceptionist.cs` |
| Caminhada e escolta | `NpcRoute.cs`, `NpcWalking.cs`, `PatientEscort.cs`, `HospitalPath.cs` |
| Equipe e exames | `HospitalStaffRoutes.cs`, `HospitalExamRoutes.cs`, `ExamProcedure.cs` |
| Leitos e posições | `BedAnimations.cs`, `HospitalUpperFloor.cs`, `SeatCalibration.cs` |
| População | `AmbientPopulation.cs`, `AmbientPatrol.cs`, `PopulationSettings.cs` |
| Tempo da simulação | `GameClock.cs` |

Em `HospitalShift.cs`, pesquise o evento ou estado envolvido antes de ler o trecho. `Run` concentra a coordenação e funções locais. `DiagnosticRooms` e `InpatientRooms` definem estações. Siga as transições de localização, ocupação e disponibilidade do paciente para conferir o fluxo completo afetado.

Ao alterar criação, transferência ou remoção de entidades, confira os caminhos de encerramento e limpeza dos recursos envolvidos. Reaproveite as rotas e animações existentes quando compatíveis. Preserve a distinção entre tempo do jogo e tempo real usando a representação já adotada no fluxo.

Compile com `../lumen-sante-build/scripts/Build-Plugin.ps1`. Registre quais resultados exigem execução no GTA V, como trajetória, animação, colisão e interação com o jogador.
