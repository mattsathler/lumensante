---
name: lumen-sante-build
description: Compila o plugin Lumen Santé com MSBuild e seleciona verificações de DLL, catálogo, medicamentos e temperatura.
---

# Compilar e verificar

Execute na raiz `./Skills/lumen-sante-build/scripts/Build-Plugin.ps1`. Aceita `-Configuration Release`, com Debug como padrão, e `-MSBuildPath` para informar uma instalação específica. Descobre MSBuild pelo PATH ou pelo vswhere.

O alvo é .NET Framework 4.8. As referências locais são `Dependencies/RagePluginHookSDK.dll` e `Dependencies/RAGENativeUI.dll`. O resultado fica em `lumen-sante/bin/<Configuration>/lumen-sante.dll`.

Selecione verificações conforme a mudança. Os scripts existentes aceitam `-AssemblyPath` e ficam em `Skills/lumen-sante-diseases/scripts`.

- Catálogo, estágios ou novas doenças — `Validate-DiseaseCatalog.ps1`.
- Efeitos ou evolução com medicamentos — `Validate-MedicationCases.ps1`.
- Temperaturas — `Validate-TemperatureCases.ps1`.
- JSON de doenças e descoberta externa — `Validate-ModularDiseases.ps1`.
- PNG ou JSON incorporado — confira `GetManifestResourceNames()` da DLL em um processo PowerShell separado, evitando reutilizar uma assembly carregada antes da recompilação.

Execute os validadores clínicos com Windows PowerShell 5.1, compatível com a DLL .NET Framework. Passe explicitamente a DLL Release se essa foi a configuração compilada. Resuma falhas com a mensagem relevante e informe o que foi validado. Compilação confirma geração da DLL. Comportamentos de renderização e integração com o jogo exigem execução no GTA V.
