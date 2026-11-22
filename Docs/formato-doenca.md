# Formato de doença JSON — versão 1

Instale cada doença como um arquivo `.json` em UTF-8 na pasta `LumenSante/Doencas/`, relativa à pasta que contém `lumen-sante.dll`. Recarregue o plugin para aplicar inclusões, alterações ou remoções. O log do RAGE Plugin Hook informa a quantidade carregada e identifica arquivos externos aceitos ou rejeitados.

Os 12 casos oficiais são JSONs incorporados à DLL durante a compilação, em `lumen-sante/Data/Doencas/`. Eles e os arquivos externos usam o mesmo leitor. Os IDs oficiais ficam reservados. Arquivos externos com IDs repetidos entre si são todos rejeitados para aquele ID. Os demais arquivos válidos continuam disponíveis.

Use [o exemplo completo](exemplos/doenca.json) como ponto de partida. Ele reutiliza o caso de rinite com ID `example-rhinitis`, nome próprio e peso 1. Colocá-lo na pasta externa cria uma doença adicional. Altere os dados para o caso que deseja criar e preserve a coerência dos sintomas.

## Campos obrigatórios

Os nomes dos campos diferenciam maiúsculas e minúsculas. Campos desconhecidos são rejeitados para identificar erros de digitação. Números inteiros devem ser escritos sem parte decimal.

| Campo | Tipo e regra |
| --- | --- |
| `SchemaVersion` | Inteiro `1`. Versões diferentes são rejeitadas. |
| `Id` | Texto único com letras minúsculas ASCII, números e hífens entre partes, como `viral-pharyngitis`. |
| `Name` | Nome não vazio. |
| `SelectionWeight` | Inteiro positivo. A chance de seleção é seu peso dividido pela soma dos pesos do catálogo carregado. |
| `Tags` | Lista não vazia de tags únicas, no mesmo formato do ID. Use tags compartilhadas, como `respiratoria`, `nasal`, `digestiva`, `infecciosa` e `dor`, para permitir a futura seleção de hipóteses semelhantes no Workers. |
| `MinimumStageDurationMilliseconds` | Inteiro positivo em milissegundos do jogo. |
| `MaximumStageDurationMilliseconds` | Inteiro maior ou igual ao mínimo. As durações devem ser inferiores a `2147483647`. |
| `WorseningRisk` | Número de 0 a 1 que define o risco base de piora. |
| `TreatmentSuccessRate` | Número de 0 a 1 que define o sucesso base do tratamento específico. |
| `RequiresSpecificTreatment` | Booleano. |
| `TreatmentName` | Nome não vazio quando o tratamento específico é obrigatório. Use `null` quando não houver tratamento específico. |
| `MedicationEffects` | Lista de efeitos, com uma entrada no máximo por classe. Pode ser vazia. |
| `Stages` | Lista de dez estágios em ordem de gravidade. |
| `Questions` | Lista não vazia de perguntas. |
| `Exams` | Objeto com IDs de exames e seus laudos. Pode ser vazio. |

## Estágios e medicação

Cada estágio exige `Pulse`, `Systolic`, `Diastolic`, `RespiratoryRate`, `OxygenSaturation`, `TemperatureTenths` e `Inspection`.

As faixas têm formato `[mínimo, máximo]`, com limites inclusivos. Nos nove estágios vivos, os sinais são inteiros positivos, a saturação fica entre 1 e 100, e a temperatura entre 300 e 430 décimos de °C. Por exemplo, `[360, 374]` representa 36,0–37,4 °C. O mínimo não pode superar o máximo. O décimo estágio representa óbito e exige `[0, 0]` em todas as faixas. A interface mostra temperatura indisponível nesse estado. A recuperação usa risco 0, separado dos dez estágios, e normaliza a temperatura.

`Inspection` contém três frases não vazias, na ordem estado geral, respiração e pele/lábios.

Cada efeito de medicação exige `Medication`, `ImprovementChance` e `WorseningMultiplier`. Os dois valores numéricos ficam entre 0 e 1. As classes aceitas são `Antibiotic`, `AntiInflammatory`, `Corticosteroid`, `Analgesic`, `Antipyretic`, `Antihistamine`, `Antiemetic`, `Bronchodilator` e `Antacid`.

O plugin aplica as mesmas fórmulas de gravidade, melhora e piora usadas antes da migração. Medicamentos incompatíveis não concedem melhora. Casos que exigem tratamento específico preservam o limite mínimo de risco 1 quando recebem apenas medicação superficial.

## Conversa

Cada pergunta exige `Prompt` e `AnswersByStage`. A primeira deve ter exatamente o texto `Como você está se sentindo?`. As demais perguntas são liberadas depois da primeira resposta.

`AnswersByStage` tem nove listas, uma para cada estágio vivo. Cada lista exige pelo menos 20 frases distintas, comparadas sem diferenciar maiúsculas e minúsculas. As perguntas também devem ser distintas. O plugin fixa a resposta sorteada por paciente, pergunta e estágio. Ao recuperar, usa a resposta comum de recuperação.

## Exames

Cada propriedade de `Exams` corresponde a um ID do catálogo de exames. Seu valor pode ser `null` ou um objeto com os campos obrigatórios `Default` e `ByStage`.

```json
{
  "x-ray": {
    "Default": "Sem opacidade pulmonar focal identificada.",
    "ByStage": {
      "2": null,
      "6": "Opacidades pulmonares difusas."
    }
  },
  "mri": null
}
```

O plugin usa o estágio registrado na solicitação do exame, de 1 a 10. Uma substituição explícita tem prioridade sobre `Default`, inclusive quando for `null`. IDs omitidos e valores `null` produzem **Exame inconclusivo**. Frases vazias são inválidas. A leitura do laudo na interface continua disponível apenas após a conclusão do pedido.

Os 51 IDs aceitos nesta versão são `mri`, `mri-brain`, `mri-spine`, `x-ray`, `x-ray-ribs`, `x-ray-pelvis`, `x-ray-limbs`, `cardiac`, `hematology`, `renal`, `hepatic`, `electrolytes`, `blood-gas`, `coagulation`, `glucose`, `crp`, `urinalysis`, `mri-abdomen`, `mri-pelvis`, `mri-liver`, `mri-kidneys`, `mri-shoulder`, `mri-knee`, `mri-hip`, `mri-cervical`, `mri-lumbar`, `x-ray-skull`, `x-ray-cervical`, `x-ray-thoracic`, `x-ray-lumbar`, `x-ray-shoulder`, `x-ray-knee`, `x-ray-ankle`, `calcium`, `magnesium`, `phosphate`, `urea`, `creatinine`, `albumin`, `bilirubin`, `tsh`, `free-t4`, `ferritin`, `iron`, `lipid-profile`, `hba1c`, `lactate`, `troponin`, `bnp`, `urine-culture`, `blood-culture`. O leitor rejeita referências desconhecidas.

Na migração, foram preservados os laudos dos ramos específicos de cada doença em `ExamFindings`. Os laudos genéricos herdados do catálogo de exames passaram a `null`. Nos cinco casos que usavam apenas resultados genéricos, todos os exames passaram a inconclusivos. Resultados normais explicitamente configurados para a doença foram preservados. As comparações futuras do Workers podem usar a presença e a igualdade das frases, sem criar um booleano redundante no arquivo.

## Validação

Compile com `Skills/lumen-sante-build/scripts/Build-Plugin.ps1`. Execute `Validate-DiseaseCatalog.ps1`, `Validate-MedicationCases.ps1`, `Validate-TemperatureCases.ps1` e `Validate-ModularDiseases.ps1`, em `Skills/lumen-sante-diseases/scripts`, em processos Windows PowerShell 5.1 após a compilação.

O validador modular verifica os recursos oficiais, a carga e remoção de um 13º caso externo, laudos por estágio com `null`, falas, vitais, referências inválidas e colisões de IDs. No GTA V, confira adicionalmente a admissão, os menus, a evolução e a aplicação das condutas do caso externo.
