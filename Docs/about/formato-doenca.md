# Formato de doença JSON — versão 1

Instale cada doença como um arquivo `.json` em UTF-8 na pasta `Plugins/LumenSante/Diseases/` da instalação do GTA V. O plugin resolve a pasta a partir do executável do jogo, pois o RAGE Plugin Hook executa uma cópia temporária da DLL. Fora do jogo, os validadores usam `LumenSante/Diseases/` junto à DLL. Recarregue o plugin para aplicar inclusões, alterações ou remoções. O log do RAGE Plugin Hook informa a pasta da instalação, a quantidade carregada e os arquivos aceitos ou rejeitados.

O catálogo padrão atual tem dez casos, mantidos em arquivos editáveis em `lumen-sante/Diseases/`. A compilação os copia para `bin/<Configuration>/LumenSante/Diseases/`. Distribua a DLL acompanhada dessa pasta. Todos os casos são lidos exclusivamente dessa pasta na instalação. Edite o próprio arquivo de uma doença padrão para personalizar seus dados, inclusive ID, nome, peso, falas e tratamentos. Remover o arquivo retira a doença na próxima carga. Arquivos válidos com IDs repetidos são todos rejeitados para aquele ID, e as demais doenças continuam disponíveis.

Reúna os JSONs padrão e personalizados em `LumenSante/Diseases/`. A cópia da compilação usa `PreserveNewest`, e a carga do plugin apenas lê os arquivos. Se a pasta estiver ausente, vazia ou sem casos válidos, o plugin informa o problema e encerra a inicialização para que o catálogo possa ser corrigido.

Use [o exemplo completo](exemplos/doenca.json) como ponto de partida. Ele reutiliza o caso de rinite com ID `example-rhinitis`, nome próprio e peso 1. Colocá-lo na pasta externa cria uma doença adicional. Altere os dados para o caso que deseja criar e preserve a coerência dos sintomas.

## Campos obrigatórios

Os nomes dos campos diferenciam maiúsculas e minúsculas. Campos desconhecidos são rejeitados para identificar erros de digitação. Números inteiros devem ser escritos sem parte decimal.

| Campo | Tipo e regra |
| --- | --- |
| `SchemaVersion` | Inteiro `1`. Versões diferentes são rejeitadas. |
| `Id` | Texto único com letras minúsculas ASCII, números e hífens entre partes, como `viral-pharyngitis`. |
| `Name` | Nome não vazio. |
| `SelectionWeight` | Inteiro positivo. A chance de seleção é seu peso dividido pela soma dos pesos do catálogo carregado. |
| `Complexity` | Opcional: `simple`, `investigative` ou `complex`. A ausência equivale a `investigative`. Classifica a investigação e o atendimento, independentemente de gravidade ou necessidade de tratamento específico; não altera o sorteio sem ajuste de `SelectionWeight`. |
| `NaturalRecoveryIntervals` | Opcional, inteiro positivo. Depois dessa quantidade de intervalos clínicos consecutivos sem mudança de estágio, a atividade melhora um estágio. Uma mudança reinicia a contagem; sem o campo não há essa recuperação natural. A contagem é preservada no prontuário. |
| `DiagnosticExclusions` | Opcional. Lista de regras de exclusão; cada regra é uma lista de condições com `Parameter`, `Min` e `Max` inclusivos. Todas as condições da mesma regra precisam corresponder a medições disponíveis; qualquer regra completa pode descartar a hipótese. Valores desconhecidos não descartam. Podem ser usados os parâmetros do catálogo numérico, como sinais vitais, `leukocytes`, `hemoglobin`, `platelets`, `sodium`, `creatinine`, `urine-leukocytes`, `urine-erythrocytes`, `crp`, `bun`, `glucose`, `urine-culture-count`, `urine-pbg`, `arterial-pao2`, `arterial-paco2`, `arterial-ph` e `bicarbonate`. |
| `LeukocyteRanges` | Opcional. Nove pares `[mínimo, máximo]` de leucócitos por µL, correspondentes aos estágios vivos. O hemograma registra a medição atual; se a doença não definir faixas, usa o intervalo geral de referência. Os valores piloto são parâmetros de jogo e exigem revisão de balanceamento. |
| `ParameterRanges` | Opcional. Objeto cujas chaves são os IDs laboratoriais compartilhados, exceto `leukocytes`, que usa `LeukocyteRanges`; cada chave contém nove pares `[mínimo, máximo]` para os estágios vivos. Parâmetros omitidos usam a faixa de referência do jogo. Os valores e as regras de exclusão representam apresentações configuradas para o jogo e precisam de revisão clínica e de balanceamento. |
| `Tags` | Lista não vazia de tags únicas, no mesmo formato do ID. Use tags compartilhadas, como `respiratoria`, `nasal`, `digestiva`, `infecciosa` e `dor`, para permitir a futura seleção de hipóteses semelhantes no Workers. |
| `Symptoms` | Opcional: lista de IDs globais `pain`, `nausea`, `breathlessness` e `nasal-congestion`. A ausência preserva a inferência legada por tags; uma lista vazia não ativa sintomas numéricos. Dor, náusea e congestão acompanham a atividade e podem ser aliviadas por medicamentos; falta de ar acompanha a saturação. |
| `MinimumStageDurationMilliseconds` | Inteiro positivo em milissegundos do jogo. |
| `MaximumStageDurationMilliseconds` | Inteiro maior ou igual ao mínimo. As durações devem ser inferiores a `2147483647`. |
| `WorseningRisk` | Número de 0 a 1 que define o risco base de piora. |
| `TreatmentSuccessRate` | Número de 0 a 1 que define o sucesso base do tratamento específico. |
| `RequiresSpecificTreatment` | Booleano. |
| `TreatmentName` | Nome não vazio quando o tratamento específico é obrigatório. Use `null` quando não houver tratamento específico. |
| `MedicationEffects` | Lista de efeitos, com uma entrada no máximo por classe. Pode ser vazia. |
| `Stages` | Lista de dez estágios em ordem de gravidade. |
| `Questions` | Lista não vazia de perguntas. |
| `Exams` | Opcional, legado. Objeto com IDs de exames e laudos textuais por estágio. É aceito para compatibilidade com JSONs existentes; para medições estruturadas, use `ParameterRanges` e os parâmetros reconhecidos pelo catálogo. |

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

Os dez JSONs padrão usam valores numéricos para os achados relevantes ao catálogo clínico. As medições são capturadas no momento da conclusão e guardam valor e referência no histórico; exames repetidos recebem sequência persistida para que regras diagnósticas usem o resultado mais recente, mesmo quando mais de um exame mede o mesmo parâmetro. Laudos textuais de definições externas continuam aceitos para compatibilidade. Um resultado numérico ausente não descarta uma hipótese.

## Validação

Compile com `Skills/lumen-sante-build/scripts/Build-Plugin.ps1`. Execute `Validate-DiseaseCatalog.ps1`, `Validate-MedicationCases.ps1`, `Validate-TemperatureCases.ps1` e `Validate-ModularDiseases.ps1`, em `Skills/lumen-sante-diseases/scripts`, em processos Windows PowerShell 5.1 após a compilação.

O validador modular verifica a ausência de doenças incorporadas à DLL, os dez arquivos padrão, sua personalização e remoção, a carga de um caso externo, pastas vazias ou ausentes, laudos por estágio com `null`, falas, vitais, referências inválidas e colisões de IDs. No GTA V, confira adicionalmente a admissão, os menus, a evolução e a aplicação das condutas do caso personalizado.
