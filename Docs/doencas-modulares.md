# Plano de implementação de doenças modulares

Cada doença do Lumen Santé será definida em um único arquivo JSON. O plugin descobrirá e carregará automaticamente os arquivos externos ao iniciar. Um criador poderá adicionar uma doença que use as mecânicas existentes sem alterar código, registrar o caso manualmente ou recompilar a DLL.

A migração vem antes do Workers. Os 12 casos atuais usarão o mesmo formato disponibilizado aos criadores externos.

## Estado da implementação

A versão 1 do leitor, os 12 casos JSON editáveis em `Diseases`, a descoberta de arquivos e os laudos por estágio estão implementados. O contrato está em [Formato de doença JSON](formato-doenca.md), com um exemplo completo instalável. As verificações automatizadas incluem personalização e remoção de casos padrão, descoberta de um 13º caso pela pasta relativa à DLL, laudos no consumidor real, validação de arquivos e colisões de IDs. A conferência do atendimento completo no GTA V continua pendente. O Workers seguirá o plano próprio depois dessa validação.

## Contrato do arquivo

| Informação | O que o JSON define |
| --- | --- |
| Identidade | Versão do esquema, ID estável e único, nome exibido, peso positivo de seleção e tags como `respiratoria`, `nasal` e `tosse`. |
| Evolução | Intervalos de avaliação, probabilidades de progressão e os dez estágios do jogo. Os estágios vivos incluem faixas de pulso, pressão arterial, frequência respiratória, saturação e temperatura, além dos achados de avaliação geral. |
| Conversa | Perguntas e respostas por estágio vivo. A primeira pergunta é “Como você está se sentindo?”. A resposta inicial libera as demais perguntas, com progresso por prontuário e respostas estáveis por paciente, pergunta e estágio. |
| Exames | Laudos vinculados aos IDs dos exames existentes. Cada entrada pode definir um laudo padrão para a doença e substituições por estágio. O estágio usado para consultar o laudo é o registrado na solicitação. |
| Conduta | Classes de medicamentos compatíveis, seus efeitos na progressão e, quando aplicável, nome e parâmetros do tratamento específico. |

O carregador converte os dados na representação usada pela simulação. O plugin continua responsável pelo sorteio de casos, pelas transições, pela seleção das falas e pela execução dos exames e tratamentos. Os IDs dos exames e classes de medicação referenciam os catálogos do plugin. Equipamentos, salas, rotas e animações necessários a uma nova modalidade de exame pertencem à implementação dessa modalidade.

## Laudos inconclusivos e uso pelo Workers

- Um exame sem laudo configurado ou com valor `null` para o estágio aplicável exibe **Exame inconclusivo** depois de concluído. Um `null` explícito no estágio prevalece sobre a frase padrão. O estado de conclusão do pedido permanece distinto da existência de uma frase informativa.
- Um resultado normal que ajude a diferenciar hipóteses tem uma frase própria. `TemResultado` é derivado da presença de uma frase, evitando duplicar a informação no JSON.
- O Workers usará o peso de seleção para determinar a faixa de raridade. Tags compartilhadas definem as hipóteses alternativas, com preferência pelas que compartilham tags mais específicas. Quando faltar uma alternativa plausível, o diagnóstico pode ficar em aberto.
- A lista de candidatas e de exames úteis por par pode ser calculada ao carregar o catálogo. Dois laudos inconclusivos não aumentam a semelhança entre doenças. Um laudo concluído que ajude a distinguir as hipóteses pode permitir uma reavaliação. Os Doctors usam a informação calculada, sem interpretar o texto do laudo durante a partida.

## Descoberta e validação

1. Distribuir os 12 JSONs padrão como arquivos editáveis em `LumenSante/Diseases/`, com o caminho resolvido a partir da pasta da DLL. Na inicialização, carregar exclusivamente os arquivos `.json` em UTF-8 dessa pasta. Inclusões, alterações e remoções passam a valer na próxima carga do plugin.
2. Validar a versão do esquema, campos obrigatórios, IDs, referências a exames e medicações, faixas numéricas, probabilidades, estágios e bancos de respostas. Usar as regras atuais do catálogo, inclusive a exigência de 20 respostas distintas por pergunta e estágio vivo. O estágio de óbito mantém o comportamento atual, com temperatura indisponível.
3. Ignorar arquivos inválidos e identificar no log o arquivo, o campo e o motivo. Quando arquivos válidos declararem o mesmo ID, rejeitar todos os envolvidos e registrar seus caminhos. Continuar carregando as demais doenças. Usuários podem editar ou remover qualquer doença, incluindo as padrão. Um catálogo sem casos válidos deve ser informado antes de iniciar o hospital.
4. Publicar a referência do esquema e um arquivo de exemplo junto da implementação, com os IDs aceitos e as instruções para descobrir se uma doença foi carregada.

## Ordem de implementação

1. Registrar a referência dos 12 casos atuais para comparar a migração. Incluir IDs, pesos, estágios, falas, laudos, efeitos de medicamentos e tratamentos específicos.
2. Implementar o esquema inicial, a descoberta de arquivos e a validação. Usar o mesmo caminho de leitura para os dados oficiais e externos.
3. Converter os 12 casos para JSON e conectar o catálogo carregado aos fluxos de atendimento. Migrar os laudos para os arquivos de doença, preservando os achados informativos. Registrar as mudanças intencionais para **Exame inconclusivo**.
4. Remover as definições C# individuais e o registro manual depois de comprovar a paridade. Atualizar os validadores e o guia de criação de doenças para o fluxo por JSON.
5. Validar um caso externo completo e verificar o fluxo no GTA V antes de integrar o Workers.

## Critérios de aceitação

- Os 12 casos oficiais mantêm seus IDs, pesos, estágios, falas, efeitos de medicamentos e tratamentos. As diferenças nos laudos se limitam às mudanças documentadas.
- Uma 13ª doença entra no sorteio apenas com a instalação do JSON e a recarga do plugin. A doença funciona na conversa, nos sinais vitais, nos exames, na evolução e na seleção das condutas suportadas. Remover o arquivo e recarregar retira a doença do catálogo.
- Arquivos inválidos, versões incompatíveis, referências desconhecidas e IDs duplicados geram mensagens que permitem corrigir o problema. As demais doenças continuam disponíveis.
- Um pedido concluído com laudo `null` mostra **Exame inconclusivo**. Um resultado normal informativo exibe a frase cadastrada. Substituições por estágio usam o estágio da solicitação.
- Compilar com MSBuild e executar as validações existentes de catálogo, medicação e temperatura. Conferir no GTA V a descoberta do caso externo e o atendimento de ponta a ponta.
