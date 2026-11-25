# Doenças de internação

> **Histórico:** este documento descreve um catálogo anterior de 50 doenças, retirado do projeto. Para o catálogo padrão atual de dez casos, consulte [Catálogo clínico](catalogo-clinico.md).

Foram adicionadas 50 doenças e condições agudas, mantendo os 12 casos anteriores. Cada novo JSON exige tratamento específico, contém dez estágios, duas perguntas com vinte respostas por estágio vivo e laudos associados aos exames disponíveis. São 18.000 respostas nos cinquenta arquivos. Os arquivos estão em `lumen-sante/Diseases` e o build os copia para a pasta externa do plugin.

## Seleção e frequência

Os primeiros 45 casos formam o grupo de maior frequência da simulação. Os últimos cinco são o grupo raro. A seleção reúne quadros de clínica médica, urgência e cirurgia, incluindo apresentações graves de doenças habitualmente tratadas fora do hospital. O conjunto é uma curadoria para o hospital do jogo. A ordem e os pesos representam o balanceamento do catálogo, sem estimativa epidemiológica de um país ou de uma população.

Dez quadros recebem peso 12, outros 35 recebem peso 8 e os cinco raros recebem peso 1. O peso somado das novas entradas é 405. Com os arquivos presentes nesta revisão, o catálogo tem 62 casos e peso total 510. A chance de cada caso é seu peso dividido pela soma dos pesos carregados. Editar ou remover doenças altera essas chances.

| Nº | Caso | ID | Peso | Chance no catálogo atual | Base clínica |
| --- | --- | --- | --- | --- | --- |
| 1 | Pneumonia adquirida na comunidade | `community-pneumonia` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/pneumonia/) |
| 2 | DPOC em exacerbação grave | `copd-exacerbation` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/chronic-obstructive-pulmonary-disease-copd/) |
| 3 | Insuficiência cardíaca descompensada | `decompensated-heart-failure` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/heart-failure/) |
| 4 | Infarto agudo do miocárdio | `acute-myocardial-infarction` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/heart-attack/) |
| 5 | Angina instável | `unstable-angina` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/angina/) |
| 6 | Fibrilação atrial com resposta ventricular rápida | `rapid-atrial-fibrillation` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/atrial-fibrillation/) |
| 7 | Emergência hipertensiva | `hypertensive-emergency` | 8 | 1,57% | [Fonte clínica](https://academic.oup.com/eurheartj/article/45/38/3912/7741010) |
| 8 | Tromboembolismo pulmonar agudo | `acute-pulmonary-embolism` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/pulmonary-embolism/) |
| 9 | Trombose venosa profunda proximal extensa | `extensive-proximal-dvt` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/deep-vein-thrombosis-dvt/) |
| 10 | Acidente vascular cerebral isquêmico | `acute-ischemic-stroke` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/stroke/) |
| 11 | Hemorragia intracerebral espontânea | `intracerebral-hemorrhage` | 8 | 1,57% | [Fonte clínica](https://www.ahajournals.org/doi/10.1161/STR.0000000000000407) |
| 12 | Epilepsia com crises agrupadas | `recurrent-epileptic-seizures` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/epilepsy/) |
| 13 | Meningite bacteriana | `bacterial-meningitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/meningitis/) |
| 14 | Pielonefrite aguda complicada | `complicated-pyelonephritis` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/kidney-infection/) |
| 15 | Cálculo ureteral obstrutivo complicado | `obstructing-ureteral-stone` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/kidney-stones/) |
| 16 | Lesão renal aguda | `acute-kidney-injury` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/acute-kidney-injury/) |
| 17 | Doença renal crônica descompensada | `decompensated-chronic-kidney-disease` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/kidney-disease/) |
| 18 | Cetoacidose diabética | `diabetic-ketoacidosis` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/diabetic-ketoacidosis/) |
| 19 | Estado hiperglicêmico hiperosmolar | `hyperosmolar-hyperglycemic-state` | 8 | 1,57% | [Fonte clínica](https://www.mkuh.nhs.uk/wp-content/uploads/2022/07/NHS-Diabetes-Management-of-Hyperosmolar-HHS.pdf) |
| 20 | Diabetes com hipoglicemia grave recorrente | `severe-diabetic-hypoglycemia` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/low-blood-sugar-hypoglycaemia/) |
| 21 | Hiponatremia sintomática grave | `symptomatic-hyponatremia` | 8 | 1,57% | [Fonte clínica](https://www.ruh.nhs.uk/For_Clinicians/departments_ruh/Older_people_unit/documents/hyponatraemia_algorithm.pdf) |
| 22 | Hipercalemia grave | `severe-hyperkalemia` | 8 | 1,57% | [Fonte clínica](https://guidelines.ukkidney.org/hyperkalaemia/) |
| 23 | Pancreatite aguda | `acute-pancreatitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/acute-pancreatitis/) |
| 24 | Colecistite aguda | `acute-cholecystitis` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/acute-cholecystitis/) |
| 25 | Colangite aguda | `acute-cholangitis` | 8 | 1,57% | [Fonte clínica](https://www.idsociety.org/practice-guideline/intra-abdominal-infections/) |
| 26 | Apendicite aguda | `acute-appendicitis` | 12 | 2,35% | [Fonte clínica](https://www.nhs.uk/conditions/appendicitis/) |
| 27 | Diverticulite aguda complicada | `complicated-diverticulitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/diverticular-disease-and-diverticulitis/) |
| 28 | Obstrução intestinal por aderências | `adhesive-bowel-obstruction` | 8 | 1,57% | [Fonte clínica](https://www.niddk.nih.gov/health-information/digestive-diseases/abdominal-adhesions) |
| 29 | Úlcera péptica com hemorragia digestiva | `bleeding-peptic-ulcer` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/stomach-ulcer/) |
| 30 | Cirrose hepática descompensada | `decompensated-cirrhosis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/cirrhosis/) |
| 31 | Hepatite associada ao álcool grave | `severe-alcohol-associated-hepatitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/alcohol-related-liver-disease-arld/) |
| 32 | Hepatite B aguda grave | `severe-acute-hepatitis-b` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/hepatitis-b/) |
| 33 | Doença de Crohn em atividade grave | `crohn-severe-flare` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/crohns-disease/) |
| 34 | Retocolite ulcerativa aguda grave | `acute-severe-ulcerative-colitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/ulcerative-colitis/) |
| 35 | Colite grave por Clostridioides difficile | `severe-c-difficile-colitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/c-difficile/) |
| 36 | Celulite bacteriana extensa | `severe-cellulitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/cellulitis/) |
| 37 | Artrite séptica do joelho | `septic-knee-arthritis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/septic-arthritis/) |
| 38 | Osteomielite da tíbia | `tibial-osteomyelitis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/osteomyelitis/) |
| 39 | Pé diabético com infecção profunda | `infected-diabetic-foot` | 8 | 1,57% | [Fonte clínica](https://www.nice.org.uk/guidance/ng19/chapter/recommendations) |
| 40 | Anemia ferropriva grave | `severe-iron-deficiency-anemia` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/iron-deficiency-anaemia/) |
| 41 | Anemia megaloblástica por deficiência de B12 | `severe-b12-deficiency-anemia` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/vitamin-b12-or-folate-deficiency-anaemia/) |
| 42 | Doença falciforme em crise vaso-oclusiva | `sickle-cell-vaso-occlusive-crisis` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/sickle-cell-disease/) |
| 43 | Asma em exacerbação grave | `severe-asthma-exacerbation` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/asthma/asthma-attack/) |
| 44 | Pericardite com derrame pericárdico | `pericarditis-with-effusion` | 8 | 1,57% | [Fonte clínica](https://www.nhs.uk/conditions/pericarditis/) |
| 45 | Pneumotórax espontâneo com repercussão | `spontaneous-pneumothorax` | 8 | 1,57% | [Fonte clínica](https://www.nhlbi.nih.gov/health/pleural-disorders) |
| 46 | Síndrome de Guillain-Barré | `guillain-barre-syndrome` | 1 | 0,20% | [Fonte clínica](https://www.nhs.uk/conditions/guillain-barre-syndrome/) |
| 47 | Miastenia gravis em crise | `myasthenic-crisis` | 1 | 0,20% | [Fonte clínica](https://www.nhs.uk/conditions/myasthenia-gravis/) |
| 48 | Púrpura trombocitopênica trombótica imune | `immune-ttp` | 1 | 0,20% | [Fonte clínica](https://pmc.ncbi.nlm.nih.gov/articles/PMC8146131/) |
| 49 | Porfiria aguda intermitente em crise | `acute-intermittent-porphyria` | 1 | 0,20% | [Fonte clínica](https://www.niddk.nih.gov/health-information/liver-disease/porphyria) |
| 50 | Granulomatose com poliangiite com síndrome pulmão-rim | `gpa-pulmonary-renal` | 1 | 0,20% | [Fonte clínica](https://www.nhs.uk/conditions/granulomatosis-with-polyangiitis/) |

## Representação clínica

As fontes por caso orientaram sintomas, relações entre achados, exames e a finalidade das condutas. As falas são ficcionais e compartilham descrições quando as manifestações se sobrepõem. Na conversa inicial, cada resposta combina a queixa da faixa de gravidade, um detalhe do caso e uma limitação funcional. A segunda pergunta combina detalhes clínicos compatíveis. Essa reutilização mantém a semelhança entre hipóteses e permite revisar o conteúdo em cada JSON. A resposta permanece fixada pelo mecanismo existente por paciente, pergunta e estágio.

Os nove estágios vivos representam agravamento do episódio hospitalar. Os intervalos de sinais vitais são cenários plausíveis para a simulação. As faixas, os valores exemplificativos dos laudos, a progressão uniforme em dez estágios e as probabilidades de melhora ou piora são escolhas de jogo. As fontes clínicas não estabelecem essa escala nem essas probabilidades. O estágio 10 representa óbito e o risco 0 representa estabilização e recuperação do episódio agudo. Em doenças crônicas, esse desfecho corresponde ao controle da descompensação, com necessidade de seguimento da doença de base.

Casos com deterioração urgente usam avaliações entre 3.600.000 e 7.200.000 ms do jogo. Os demais usam 7.200.000 a 14.400.000 ms. O risco base de piora é 0,45 ou 0,30 e o parâmetro de sucesso do tratamento específico é 0,55, sujeitos às fórmulas existentes de gravidade. Esses tempos comprimem a evolução para a partida.

Todos usam `RequiresSpecificTreatment: true`. Antibióticos, analgesia, broncodilatadores, corticoides e anti-inflamatórios disponíveis podem melhorar parcialmente os casos compatíveis. A recuperação completa exige o tratamento específico correto. Os nomes das condutas representam pacotes assistenciais abstratos, incluindo procedimentos e suporte. A implementação atual executa a mecânica de seleção e evolução, com animações e ações já existentes.

## Exames e limites da representação

Cada caso possui laudos próprios por gravidade e pelo menos um exame disponível. Exames adicionais foram incluídos quando ajudam a diferenciar hipóteses. O laudo do estágio 10 fica explicitamente inconclusivo. Exames omitidos seguem o comportamento de resultado inconclusivo do leitor.

Algumas confirmações precisam de modalidades ausentes do catálogo atual. Os laudos explicam a necessidade de ultrassom venoso na trombose, imagem vascular na embolia, líquor na meningite, endoscopia no sangramento digestivo, EEG nas crises, sorologia na hepatite B, teste fecal em C. difficile, dosagem de B12, ADAMTS13 na PTT, porfobilinogênio na porfiria e ANCA ou biópsia na vasculite. Os exames disponíveis fornecem pistas e achados de apoio. A RM pode demonstrar obstrução urinária, mas a visualização do cálculo tem limitação explicitada no laudo.

A internação usa o fluxo existente de admissão e disponibilidade de leitos. Com uma hipótese que exige tratamento específico, os Doctors tentam internar o paciente após a consulta. Erros de hipótese e falta de vaga continuam sujeitos às regras do jogo. As condutas do jogador também passam pelas permissões existentes.

## Verificação

Use os validadores de catálogo, medicamentos, temperatura e modularização da skill de doenças. `Validate-InpatientCases.ps1` confere especificamente os cinquenta IDs novos, a divisão 45/5, pesos, bancos de falas, exames e a recuperação somente com tratamento específico. A execução completa com pacientes, leitos, Workers e interface precisa de validação no GTA V.

Nesta revisão, a compilação Debug e os validadores de catálogo, medicamentos, temperatura, modularização, internação, Workers e autoridade clínica passaram. Os cinquenta casos novos foram exercitados em cem sementes de evolução cada. O teste de Workers passou com os 62 casos em mil sementes por caso. A pasta de saída contém os mesmos 62 JSONs da origem, com conteúdo idêntico nos cinquenta arquivos novos.

As faixas atuais de acerto dos Doctors usam a participação individual no peso total. Com peso total 510 e maior peso individual 20, todos os casos ficam abaixo do limiar de 5% e recebem acerto base de 65%, antes dos bônus de evidência. Os pesos 12, 8 e 1 diferenciam a frequência de aparecimento dos novos casos. A regra de acerto dos Doctors permanece definida na implementação existente.
