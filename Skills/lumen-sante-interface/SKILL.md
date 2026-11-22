---
name: lumen-sante-interface
description: Altera telas, abas, ícones e entrada de texto do computador hospitalar e dos menus do Lumen Santé.
---

# Interface do hospital

Caminhos relativos à raiz do repositório.

- `Interface/BedManagementPanel.cs`, dentro de `lumen-sante`, contém abas, geometria, clique, rolagem e desenho do computador. Pesquise `RecordTab`, `RecordTabs`, `RecordIcons`, `SidebarIcons`, `HandleContentClick`, `Draw` e `Frame` conforme a alteração.
- `Interface/HospitalComputer.cs` controla cadeira, animação, abertura e eventos encaminhados ao plantão.
- `Interface/PatientMenu.cs` contém o menu de atendimento. `Interface/TerminalKeyboard.cs` trata entrada de texto do terminal.

Para ícones, procure o nome na lista correspondente e confira `LoadIcon`. A lista `RecordIcons` acompanha a ordem de `RecordTabs` e `RecordTab`. Use o nome do PNG sem extensão. `Assets/*.png` já é incorporado pelo `.csproj`, com nome de recurso `lumen_sante.Assets.<nome>.png`. Confira o tratamento de cor em `LoadIcon` quando a aparência for relevante.

Para novas ações, siga o evento desde a interface até o consumidor em `Gameplay/HospitalShift.cs`. Para alterações de geometria, confira desenho e área de clique, incluindo a transformação de escala. Leia os trechos envolvidos antes de editar.

Compile usando `../lumen-sante-build/scripts/Build-Plugin.ps1`. Para recursos novos, confira a presença na DLL. Informe quando a aparência ou interação ainda depender de verificação no jogo.
