# Arquitetura e integração

> Documento histórico do protótipo: várias premissas abaixo foram superadas. Para a implementação atual, consulte o código, [Jogabilidade](jogabilidade.md) e [Formato de doença JSON](formato-doenca.md).

## Ambiente-alvo

O alvo é GTA V Legacy para PC em modo história, com RAGE Plugin Hook (RPH) e LSPDFR instalados. A implementação proposta é uma DLL em C#. A combinação de builds, o SDK e o framework de destino serão definidos após um teste de carregamento. A documentação do RPH descreve a interface entre plugins e o jogo. O guia do LSPDFR cobre a instalação do ambiente base. [RAGE Plugin Hook](https://ragepluginhook.net/About.aspx?lang=en-US) · [Guia de instalação do LSPDFR](https://www.lcpdfr.com/lspdfr/04/install/) · [Suporte a Legacy](https://www.lcpdfr.com/forums/forum/920-lspdfr-04-for-legacy-edition-support/)

O teste inicial deve confirmar como a DLL é descoberta e descarregada com LSPDFR carregado. A proposta é ativar o plantão no hospital, inclusive com o jogador fora de serviço policial. Qualquer uso da API do LSPDFR precisa de um caso concreto e de validação no SDK. A compatibilidade com chamados policiais concorrentes será verificada no jogo.

## Responsabilidades

| Componente | Responsabilidade |
|---|---|
| Plantão | Iniciar e encerrar a sessão, manter fila e caso ativo, gerar resumo |
| Casos | Validar transições e ações disponíveis, registrar desfechos |
| Integração com GTA V | Detectar interação no hospital, gerenciar NPCs e marcadores, observar indisponibilidade do jogador |
| Interface | Exibir ficha, ações disponíveis, feedback e resumo |

A lógica dos casos deve operar sem chamadas ao jogo para permitir validação fora do GTA V. O adaptador executa as ações do jogo pelo mecanismo previsto no SDK validado. O primeiro protótipo pode usar notificações e ações simples, com comandos configuráveis assim que houver uma tecla de interação definida.

## Ciclo de vida e falhas

- Inicializar a interação do hospital ao carregar o plugin. Criar pacientes e recursos de atendimento apenas durante o plantão.
- Manter no máximo um atendimento ativo. Ignorar interações repetidas enquanto uma ação estiver em andamento.
- Registrar e limpar somente recursos sob propriedade do plugin. A mesma rotina de limpeza deve poder ser chamada mais de uma vez.
- Se um NPC falhar ao ser criado, manter o registro na fila para nova tentativa e informar a falha. Se um NPC ativo ficar indisponível, interromper o caso e liberar a estação.
- Encerrar a sessão e iniciar a limpeza em caso de morte do jogador, descarregamento ou falha que impeça continuar. Se o ambiente permitir, apresentar o motivo e o resumo.
- Registrar no log carregamento, início e fim do plantão, transições inválidas e falhas de integração. Manter fichas e resumo em memória na primeira versão.

## Organização do código

- `PluginEntryPoint.cs` registra o plugin e o comando do console.
- `Gameplay/HospitalShift.cs` coordena o plantão, a admissão, a evolução do paciente e a limpeza dos recursos do jogo.
- `Interface/PatientMenu.cs` apresenta a conversa, os sinais vitais e os exames no RAGENativeUI.
- `Clinical/DiseaseStage.cs` define as faixas e gera as medições simuladas.
- `Clinical/LungCancerCase.cs` concentra a duração dos estágios, as faixas e os relatos do câncer de pulmão. Para adicionar uma doença, crie uma definição própria com dez estágios, peso relativo de seleção e pelo menos 20 respostas distintas por pergunta em cada estágio em que o paciente está vivo. Registre a definição em `Clinical/DiseaseCatalog.cs`. As respostas podem ser reutilizadas entre doenças.
- `Data/nomes-masculinos.json`, `Data/nomes-femininos.json` e `Data/sobrenomes.json` são as fontes de nomes. Os arquivos entram na DLL como recursos. `Gameplay/PatientNames.cs` valida as listas e sorteia a identidade na admissão.
