---
name: lumen-sante-diseases
description: Adiciona ou revisa doenças jogáveis no plugin Lumen Santé. Use para modelar estágios de gravidade, sinais vitais, perguntas e respostas, e raridade de casos no catálogo clínico.
---

# Adicionar doenças ao Lumen Santé

A partir da raiz do projeto, leia `lumen-sante/Clinical/DiseaseDefinition.cs`, `DiseaseStage.cs`, `DiseaseCatalog.cs` e uma definição existente, como `LungCancerCase.cs`. Use `Docs/jogabilidade.md` para conferir o fluxo e os recursos já implementados.

1. Crie um arquivo próprio em `lumen-sante/Clinical` para a definição da doença. Use um ID único, peso inteiro positivo e duração de estágio em milissegundos. O peso é relativo ao total do catálogo. No caso de peso 5 em um total de 50, a chance é 10%.
2. Defina dez estágios de gravidade de jogo. Nos estágios 1 a 9, declare faixas inclusivas para frequência cardíaca, pressão sistólica e diastólica, frequência respiratória e saturação de oxigênio. O estágio 10 tem sinais zerados e representa o óbito. Baseie sintomas plausíveis em fontes clínicas primárias ou oficiais e registre a fonte na documentação quando consultar uma. Use tempo e vitais como parâmetros da simulação.
3. Declare as perguntas e seus bancos de respostas junto à definição da doença. Cada pergunta precisa de pelo menos 20 frases distintas por estágio de 1 a 9. Varie a formulação e os detalhes compatíveis com o estágio. Contar apenas mudanças de pontuação ou saudações não produz a variedade esperada. Reaproveite respostas entre doenças quando os sintomas se sobrepuserem. Preserve a coerência entre respostas que o mesmo paciente pode dar no mesmo estágio. Deixe o diagnóstico oculto na interface.
4. Registre a definição em `DiseaseCatalog.Cases` e adicione o arquivo a um elemento `Compile` em `lumen-sante/lumen-sante.csproj`. A admissão e a ficha consomem o catálogo e a definição, portanto novos casos normalmente exigem mudanças só na camada clínica e no projeto. Integre exames ou tratamentos quando estiverem no escopo solicitado.
5. Recompile `lumen-sante.slnx` com MSBuild para .NET Framework 4.8. Execute `scripts/Validate-DiseaseCatalog.ps1` nesta skill para validar a DLL gerada. Informe o ID escolhido, o peso relativo e o resultado da compilação. Registre à parte qualquer comportamento que dependa de teste no GTA V.
