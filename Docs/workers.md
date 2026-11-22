# Plano de implementação do Workers

O hospital deve funcionar por conta própria e transmitir a sensação de que o jogador é um funcionário da equipe. Doctors NPCs farão consultas, formularão hipóteses, solicitarão exames, internarão, prescreverão, acompanharão pacientes e darão altas. O jogador poderá atuar livremente e terá prioridade nas decisões.

A implementação começa depois da migração descrita em [Doenças modulares](doencas-modulares.md). O Workers consumirá as doenças oficiais e externas carregadas pelo mesmo catálogo.

## Regras de funcionamento

- Manter pelo menos três Doctors vivos e presentes no hospital. Profissionais entrando ou saindo não sustentam esse mínimo. Saídas normais respeitam a cobertura mínima, e desaparecimentos ou mortes acionam reposição prioritária com nova tentativa em caso de falha de criação.
- Apenas Doctors assumem as decisões clínicas autônomas. Enfermeiros executam as escoltas, exames e visitas habituais, recebendo também os pedidos dos Doctors.
- O hospital continua funcionando quando o jogador encerra seu plantão. O pause do jogo pausa a simulação. O descarregamento do plugin encerra a sessão conforme a persistência atual.
- O jogador pode trocar de setor e atender pacientes em qualquer área. A distribuição dos Doctors acompanha essa atuação automaticamente.
- Doctors atendem pacientes sentados na consulta, sem utilizar a maca. Para chamar um paciente, precisam alcançar e utilizar um computador disponível. Para solicitar exames de internados, precisam visitar o quarto ou a enfermaria e alcançar o ponto de atendimento.

## Cobertura de áreas

Considerar os setores Diagnóstico, Enfermaria e Quartos. Medir a participação recente do jogador por consultas, conversas, avaliações, pedidos de exames, prescrições, internações e altas. A posição física auxilia a navegação, enquanto as ações de atendimento definem a cobertura. A leitura isolada de um prontuário não toma o caso de um Doctor.

A influência das ações diminui com o tempo. Se o jogador estiver atendendo mais no diagnóstico e na enfermaria, os Doctors priorizam os quartos. Ao assumir os quartos, ele permite que a equipe aumente a cobertura dos outros setores.

O agendador considera demanda, tempo sem atendimento, pendências, cobertura recente do jogador e tarefas já atribuídas. Distância ajuda a escolher o profissional. A espera acumulada aumenta a prioridade para evitar abandono de setores. A redistribuição ocorre em eventos relevantes e em verificações periódicas espaçadas. Uma mudança de área prefere o fim da etapa atual, com interrupção quando o jogador assume diretamente o atendimento.

## Diagnóstico por raridade e tags

O diagnóstico usa sorteios e consultas ao catálogo. Cada doença tem peso de seleção e tags nos próprios dados. O peso determina sua faixa de raridade. Quanto mais rara, maior a chance de erro do Doctor.

| Faixa | Chance inicial de acerto proposta |
| --- | --- |
| Comum | 95% |
| Intermediária | 85% |
| Rara | 65% |

Esses percentuais são parâmetros iniciais de balanceamento. Os limites que convertem peso em faixa de raridade e o aumento de acerto após exames devem ser definidos na implementação e registrados para permitir ajuste. A classificação deve considerar a expansão do catálogo por arquivos externos.

Quando houver erro, sortear uma hipótese entre doenças com tags compartilhadas. Dar preferência às candidatas que compartilham tags específicas. Uma doença pode ter várias tags, como `respiratoria`, `nasal` ou `tosse`. Sem candidata plausível, manter o diagnóstico em aberto e agendar reavaliação.

A conduta segue a hipótese registrada. O Doctor pode prescrever uma medicação incompatível com a doença real quando sua hipótese estiver errada. O sistema de evolução continua avaliando os efeitos contra a doença real.

Salvar a hipótese por episódio e o histórico de revisões. Trocar de Doctor, visitar novamente ou reabrir uma ficha preserva a hipótese. Um exame concluído que ajude a separar as candidatas permite uma reavaliação com chance maior de acerto. Cada nova evidência deve ser consumida uma vez, impedindo sorteios repetidos sobre o mesmo laudo.

## Exames e investigação

Usar o contrato dos JSONs de doença. Um laudo ausente ou `null` aparece como **Exame inconclusivo**. Resultados normais que ajudem a distinguir hipóteses mantêm suas frases. A informação sobre existência de resultado é derivada da frase aplicável ao estágio da solicitação.

Calcular as listas de hipóteses semelhantes e exames úteis por par ao carregar o catálogo. Dois resultados inconclusivos não aumentam a similaridade. Laudos iguais mantêm ambas as hipóteses plausíveis. Um resultado presente em apenas uma hipótese, ou resultados distintos, pode permitir revisão. A lógica usa essas relações preparadas na carga, sem interpretar as frases durante o atendimento.

Na primeira versão, preservar a fila de execução existente e as solicitações de internados a partir do leito. Doctors solicitam exames pertinentes à hipótese, evitam duplicar pedidos e podem atender outros pacientes enquanto aguardam resultados. A conclusão cria uma pendência de revisão. Exames inconclusivos ou evidências já consumidas não geram novos sorteios.

## Prioridade e autoria do jogador

Registrar autor, horário e revisão nas decisões clínicas. Ao interagir diretamente com um paciente, o jogador ganha prioridade imediata. Cancelar ações ainda pendentes do Doctor para aquele atendimento, preservando procedimentos já em execução até que possam ser interrompidos com segurança.

Quando o jogador definir um tratamento, prescrever ou retirar uma medicação, proteger o plano terapêutico inteiro durante o episódio. A proteção acompanha transferências e retornos relacionados. Doctors podem avaliar, acompanhar e solicitar exames, mas alterações da conduta e alta desse paciente ficam com o jogador. Uma escolha incompatível também recebe essa proteção.

A expiração normal de medicamentos continua ocorrendo. O Doctor não reintroduz medicações retiradas pelo jogador. Planos criados por Doctors podem ser substituídos pelo jogador e revisados pela equipe enquanto não houver proteção.

Antes de aplicar qualquer ação planejada, conferir novamente o estado e a revisão do prontuário. Uma decisão do jogador tomada durante o deslocamento invalida o plano anterior do Doctor. A verificação pertence às operações compartilhadas de atendimento.

## Reservas e capacidade

A reserva garante capacidade para uso do jogador. Com duas salas de diagnóstico, no máximo uma pode ficar comprometida com atendimento autônomo. O jogador pode ocupar a outra enquanto o Doctor atende. Ao usar a vaga protegida, o jogador a ocupa normalmente, e os Workers aguardam disponibilidade para ampliar sua ocupação.

| Recurso | Política |
| --- | --- |
| Diagnóstico | Reservar uma das duas salas para uso do jogador. |
| Quartos | Workers deixam pelo menos um dos quatro quartos disponível para o jogador. |
| Enfermaria | Usar os sete leitos conforme disponibilidade, respeitando reservas e transferências. |
| Computadores | Reservar a estação separadamente da ocupação clínica da sala, respeitando a prioridade do jogador. |
| Pacientes | Permitir no máximo uma ação clínica exclusiva por paciente, coordenada com exames e transferências. |

Reservas contam desde o início da escolta ou transferência. Uma vaga protegida pode mudar para outro espaço equivalente livre. O jogador pode usar a última vaga disponível. Workers suspendem novas ocupações quando necessário, sem remover pacientes nem conceder altas para fabricar vagas.

Se faltar leito, o paciente entra em **Aguardando leito**. Quando os critérios de estabilidade do jogo permitirem, ele retorna à recepção, libera a consulta e preserva sua prioridade. Se precisar continuar acompanhado na sala, permanece nela e novas chamadas autônomas aguardam. Implementar esse estado explicitamente, preservando prontuário, hipótese, tratamento e pendências.

## Ciclo de atendimento

1. Escolher uma tarefa, conferir elegibilidade e reservar os recursos.
2. Para uma nova consulta, alcançar o computador, chamar o paciente e aguardar a escolta de enfermagem.
3. Atender o paciente sentado e registrar a hipótese sorteada.
4. Aplicar a conduta compatível com a hipótese, encaminhar à internação ou manter acompanhamento conforme critérios do jogo.
5. Quando houver necessidade de investigação hospitalar, reservar o leito, concluir a transferência e visitar o paciente antes de solicitar exames.
6. Liberar o Doctor para outras tarefas enquanto exames e procedimentos avançam.
7. Rever a hipótese quando houver nova evidência elegível e revisar sua própria conduta quando permitido.
8. Reavaliar o paciente e dar alta quando os critérios de jogo permitirem e o plano não estiver protegido pelo jogador.

## Simulação e recuperação de falhas

Perto do hospital, executar deslocamentos, uso de computadores e visitas com NPCs físicos. Longe, avançar as mesmas etapas pelo relógio, respeitando durações, reservas e pendências, sem manter todas as animações e rotas ativas. Ao retornar, reconstruir a representação física a partir do estado da simulação. A transição não pode duplicar chamadas, prescrições ou conclusões de exames.

Uma falha de rota não conclui uma visita. Limitar tentativas, liberar reservas abandonadas e devolver tarefas ao agendador. Morte ou desaparecimento de um Doctor preserva o prontuário e permite redistribuir seu trabalho. Paciente ausente, falecido, em exame ou em transferência precisa ser tratado antes da execução de uma ação incompatível.

Manter os Doctors no mesmo conjunto de personagens da população ambiente, alternando circulação e trabalho. Registrar tarefas, autoria das condutas, sorteios e motivos de cancelamento no log. O histórico consultável do paciente deve permitir identificar a hipótese e as condutas da equipe.

## Organização e sequência de implementação

As operações atualmente concentradas em `HospitalShift` precisam receber explicitamente o paciente e o profissional responsável. Extrair as operações compartilhadas de chamada, internação, solicitação de exame, prescrição e alta. Coordenar o estado sem depender do paciente selecionado na interface do jogador.

1. Concluir e validar a modularização das doenças por JSON.
2. Extrair as operações compartilhadas, registrar autoria e implementar proteção do plano do jogador.
3. Separar a rotina hospitalar do plantão individual, preservando evolução, filas e acompanhamento.
4. Implementar reservas de pacientes, profissionais, computadores, salas e leitos, além do mínimo de três Doctors.
5. Implementar tarefas físicas, consultas sentadas, chamadas por computador e visitas aos internados.
6. Integrar diagnóstico por raridade, candidatas por tags, pedidos pertinentes e revisão por exames.
7. Completar internação, espera por leito, conduta, acompanhamento, alta e redistribuição de áreas.
8. Implementar a transição entre simulação próxima e distante e validar recuperação de falhas.

## Critérios de aceitação

- Completar um ciclo autônomo de consulta, investigação quando necessária, conduta, acompanhamento e alta, inclusive com o jogador fora de plantão.
- Mudar a cobertura dos Doctors conforme a atuação recente do jogador, preservando atendimento das pendências e evitando mudanças contínuas de setor.
- Manter o mínimo de três Doctors nas saídas normais e repor perdas com prioridade. Enfermeiros continuam executando seus trabalhos habituais.
- Chamar pacientes somente após alcançar o computador e solicitar exames de internados somente após a visita. Nenhum atendimento autônomo utiliza a maca.
- Respeitar as vagas protegidas e impedir reservas duplicadas, chamadas simultâneas do mesmo paciente e movimentações conflitantes.
- Preservar decisões do jogador, inclusive quando um Doctor já tiver planejado uma ação. Respeitar retirada e expiração de medicação e proteção contra alta automática.
- Aplicar as probabilidades de diagnóstico configuradas e limitar erros às candidatas por tags. Preservar hipóteses entre visitas e consumir cada evidência de revisão uma única vez.
- Carregar uma doença externa por JSON e incluí-la nas candidatas e decisões sem registrar código específico no Workers.
- Não bloquear permanentemente salas quando faltar leito. Preservar o caso e a prioridade durante a espera.
- Recuperar falhas de rota, ausência de NPC e troca entre simulação próxima e distante sem perder prontuários ou duplicar ações.
- Compilar, executar verificações de lógica de diagnóstico, reservas e autoria, e validar no GTA V as rotas, computadores, visitas e atendimento simultâneo com o jogador.
