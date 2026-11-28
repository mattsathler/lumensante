# Catálogo clínico padrão

O catálogo padrão do beta contém oito casos simples e dois casos raros. As doenças continuam sendo arquivos editáveis; quem instalar casos adicionais altera a distribuição real.

| Caso | ID | Complexidade | Peso |
| --- | --- | --- | ---: |
| Síndrome gripal | `flu-like` | Simples | 400 |
| Faringite viral | `viral-pharyngitis` | Simples | 160 |
| Rinossinusite aguda viral | `acute-sinusitis` | Simples | 160 |
| Cistite bacteriana | `bacterial-cystitis` | Simples | 160 |
| Gastroenterite aguda | `acute-gastroenteritis` | Simples | 160 |
| Rinite alérgica | `allergic-rhinitis` | Simples | 160 |
| Distensão muscular | `muscle-strain` | Simples | 160 |
| Cefaleia tensional | `tension-headache` | Simples | 160 |
| Porfiria aguda intermitente em crise | `acute-intermittent-porphyria` | Complexo | 50 |
| Granulomatose com poliangiite, síndrome pulmão-rim | `gpa-pulmonary-renal` | Complexo | 50 |

Os pesos padrão somam 1.620; os casos simples correspondem a aproximadamente 93,8% das escolhas. Complexidade e gravidade são conceitos distintos.

Os oito casos simples possuem recuperação natural configurada. Analgésicos e antieméticos aliviam dor e náusea no estado atual sem reduzir diretamente a atividade da doença; os sintomas podem voltar quando o efeito termina. As doenças declaram explicitamente os sintomas numéricos disponíveis; os perfis e intensidades precisam ser revistos durante o balanceamento.

Os dez casos têm faixas numéricas de leucócitos e parâmetros laboratoriais por estágio quando o achado integra sua apresentação. Porfiria configura sódio e porfobilinogênio; granulomatose configura função renal, hemácias urinárias e achados de tórax; cistite configura leucócitos urinários, cultura e PCR; gastroenterite configura nitrogênio ureico; os quadros respiratórios configuram PCR. Os resultados relevantes dos dez casos — incluindo hematologia, química clínica, urinálise, cultura, gasometria, ECG e imagem torácica, craniana e da coluna — são apresentados como medições estruturadas, salvas com referência e momento de coleta. A PaO₂ simulada acompanha a saturação. Os laudos textuais `Exams` foram removidos dos dez JSONs padrão; o leitor ainda aceita esse campo em definições externas antigas. As faixas e regras diagnósticas são parâmetros de jogo provisórios, sujeitos a revisão clínica e balanceamento; exames sem parâmetro configurado não fornecem evidência quantitativa.

O catálogo também registra PCR, glicose casual, nitrogênio ureico, contagem bacteriana da urocultura e porfobilinogênio urinário. As referências de jogo usam limites simplificados para adultos e não substituem interpretação clínica. Foram consultados os catálogos da Mayo Clinic Laboratories para [PCR](https://www.mayocliniclabs.com/api/sitecore/TestCatalog/DownloadTestCatalog?testId=9731), [glicose casual](https://www.mayocliniclabs.com/test-catalog/Overview/89115), [nitrogênio ureico](https://www.mayocliniclabs.com/test-catalog/overview/81793), [urocultura](https://www.mayocliniclabs.com/test-catalog/overview/60515) e [porfobilinogênio urinário](https://www.mayocliniclabs.com/test-catalog/Overview/82068). A faixa única de nitrogênio ureico combina os limites adultos por sexo para simplificar o jogo; as faixas por estágio e critérios diagnósticos são conteúdo de simulação a revisar.

O painel hepático usa AST, ALT, albumina e bilirrubina total; o exame de coagulação migrado apresenta INR. As referências simplificadas consideram o [painel metabólico](https://www.mayocliniclabs.com/test-catalog/overview/113631) e o [tempo de protrombina](https://www.mayocliniclabs.com/test-catalog/overview/40934) da Mayo Clinic Laboratories. Para AST e ALT, o jogo usa uma única faixa adulta que abrange os limites apresentados por sexo na fonte.
