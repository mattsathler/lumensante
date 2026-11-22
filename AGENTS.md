# Lumen Santé

Plugin C# para GTA V Legacy e RAGE Plugin Hook, com alvo .NET Framework 4.8.

Consulte a skill local correspondente ao trabalho, lendo apenas as referências necessárias.

- Interface, computador e ícones — `Skills/lumen-sante-interface/SKILL.md`.
- Compilação e validação da DLL — `Skills/lumen-sante-build/SKILL.md`.
- Plantão, NPCs, rotas e leitos — `Skills/lumen-sante-gameplay/SKILL.md`.
- Doenças, medicamentos e sinais vitais — `Skills/lumen-sante-diseases/SKILL.md`.

Use `rg -n` com símbolos e contexto limitado antes de abrir arquivos extensos. `HospitalShift.cs` coordena o plantão, `BedManagementPanel.cs` desenha o computador e `Clinical` contém a simulação clínica. Consulte `Docs` quando precisar das regras de produto, distinguindo propostas de comportamento implementado.

O projeto usa inclusões explícitas de arquivos C# no `.csproj`. PNGs em `Assets` e JSONs em `Data` já entram por curinga como recursos incorporados. A compilação local usa `Skills/lumen-sante-build/scripts/Build-Plugin.ps1`. Testes visuais e integração com NPCs dependem do GTA V.
