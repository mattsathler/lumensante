---
name: lumen-sante-diseases
description: Adiciona ou revisa doenças, estágios, sintomas, medicamentos e sinais vitais da simulação clínica do Lumen Santé.
---

# Simulação clínica

Escolha a leitura conforme a tarefa, dentro de `lumen-sante/Clinical`.

- Nova doença ou revisão completa — leia [regras de novas doenças](references/novas-doencas.md), `Docs/formato-doenca.md`, `DiseaseJsonReader.cs` e um JSON semelhante em `lumen-sante/Diseases`.
- Peso ou seleção — localize o JSON e `DiseaseCatalog.cs`.
- Sinais vitais ou temperatura — leia a definição afetada e a representação em `DiseaseStage.cs`.
- Efeitos de medicamentos e progressão — leia `Medication.cs`, `DiseaseDefinition.Evolve` e a definição afetada. Consulte o consumidor em `Gameplay/HospitalShift.cs` se mudar a aplicação durante o plantão.
- Diálogo — leia o JSON e `ExamOrders.GetSymptomAnswer`, que mantém a resposta por paciente, pergunta e estágio.

Para correções pontuais, preserve as regras existentes fora do escopo alterado. Todas as doenças são JSONs editáveis descobertos em `LumenSante/Diseases` junto à DLL. Os casos padrão ficam em `lumen-sante/Diseases` no projeto e são copiados para a pasta de saída pelo build. Usuários podem editar ou remover qualquer caso. IDs duplicados rejeitam todos os arquivos envolvidos. Novas doenças dispensam registro C# e inclusão individual no `.csproj`. Consulte `Docs/jogabilidade.md` quando a decisão depender do fluxo de produto.

Compile com `../lumen-sante-build/scripts/Build-Plugin.ps1`. Execute os validadores desta pasta conforme o impacto, passando `-AssemblyPath` se necessário.

- `scripts/Validate-DiseaseCatalog.ps1` para catálogo, estágios e doenças novas.
- `scripts/Validate-MedicationCases.ps1` para medicamentos e evolução.
- `scripts/Validate-SymptomTreatment.ps1` para alívio e retorno de sintomas.
- `scripts/Validate-TemperatureCases.ps1` para temperaturas.
- `scripts/Validate-ModularDiseases.ps1` para leitura JSON, doenças externas e erros de validação.
- `scripts/Validate-InpatientCases.ps1` para os dois casos raros de internação e sua exigência de tratamento específico.
- `scripts/Validate-CaseComplexity.ps1` para o catálogo padrão de dez casos e sua distribuição.
- `scripts/Validate-NumericCatalog.ps1` para medições numéricas e consistência dos critérios nas apresentações geradas.

Os scripts usam Windows PowerShell 5.1 e a DLL Debug por padrão. Ao adicionar uma doença, informe ID e peso relativo. Diferencie validação executada de comportamento ainda pendente no GTA V.
