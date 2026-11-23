# Agenda Médica — piloto

## Objetivo

Dar ao jogador uma agenda própria de consultas, paralela ao fluxo espontâneo da clínica. O jogador continua livre para atender qualquer paciente, sem escolher um modo de trabalho. Os horários da agenda seguem correndo enquanto ele atua em outros setores ou está fora do plantão.

O piloto está disponível na página **Agenda** do computador e do tablet. Na confirmação, marque **Agendar retorno?** antes de selecionar **Dar Alta** para manter o acompanhamento. A agenda mostra a data atribuída e oferece as visões **Dia**, **Semana** e **Retornos Pendentes**.

## Configuração inicial

- O jogo abre consultas para novos pacientes às 10h e às 15h, mantendo uma janela de sete dias. O retorno ocupa o primeiro horário livre entre 9h e 17h a partir do dia seguinte.
- Após uma hora de ausência, um paciente novo fica marcado como **Consulta perdida**. Um retorno fica em **Retornos Pendentes**, onde pode ser excluído sem apagar o prontuário.
- Com o jogador de plantão, o paciente aguarda vaga na recepção e então entra no fluxo normal. O jogador chama os pacientes da agenda. Se um atendimento em curso for interrompido pelo descarregamento do plugin, ele recomeça pela recepção com o prontuário salvo.
- A simulação clínica do acompanhamento é retomada quando o paciente volta. Durante a espera, as prescrições continuam a expirar pelo relógio do jogo.
- O save fica em `Plugins/LumenSante/agenda.json`. Ele guarda a agenda e os prontuários vinculados a ela. A substituição do arquivo mantém uma cópia anterior em `agenda.json.bak`.

## Regras definidas

- O jogo cria automaticamente consultas de novos pacientes na agenda do jogador.
- Na alta realizada pelo jogador, ele pode decidir se deseja acompanhar o paciente. Se escolher retorno, o jogo atribui automaticamente dia e horário. Altas de Doctors NPCs não geram retornos na agenda pessoal.
- O retorno usa o mesmo prontuário e representa o acompanhamento do mesmo caso, com visitas sucessivas enquanto o jogador julgar necessário.
- A agenda distingue **Consulta** e **Retorno**. Quando o paciente agendado chega à recepção e está pronto para ser chamado, o jogador recebe uma notificação. Depois, o atendimento segue o fluxo habitual.
- O computador e o tablet exibem a agenda por dia e por semana. A listagem de pacientes pode identificar a origem agendada com uma tag; a apresentação exata da tag pode ser decidida durante o desenho da interface.
- O calendário, o dia da semana e os horários usam apenas a data e a hora do GTA V. O relógio do sistema operacional não participa dos cálculos.
- Se o jogador não estiver de plantão, a consulta vence após uma hora do jogo de tolerância. Retornos perdidos entram em **Retornos Pendentes**. O jogador pode excluir um retorno pendente, encerrando o acompanhamento daquele caso.
- Não há alternância formal entre clínica e agenda. O jogador define sua atuação pelas ações que toma, e os compromissos continuam válidos.
- Especialidades e restrição da agenda à especialidade escolhida ficam para uma etapa futura.

## Ciclo

1. Ao carregar o plugin, recuperar agenda e prontuários persistidos e reconciliar os compromissos com a data atual do GTA V.
2. Manter uma janela futura de consultas de novos pacientes. Gerar apenas as vagas ainda não criadas e gravá-las antes de apresentá-las, para que recarregar o plugin não sorteie novamente a mesma semana.
3. Na alta feita pelo jogador, oferecer **Agendar retorno?**. O jogo encontra uma vaga futura, cria o compromisso e mantém o prontuário para a próxima visita. Uma alta sem agendamento encerra o acompanhamento pessoal.
4. Perto do horário, encaminhar o paciente agendado à recepção, respeitando a capacidade física da sala de espera. O compromisso conserva a identidade do paciente mesmo que a criação do ped precise ser adiada.
5. Ao registrar o paciente na recepção, vincular o ped ao compromisso e ao prontuário existente (ou criar o prontuário persistente do novo paciente). A consulta segue as transições já usadas pelo plantão.
6. Ao chamar o paciente, registrar o compromisso como **Em atendimento**. A alta encerra o compromisso e pode produzir outro retorno no mesmo prontuário.
7. Se o jogador estiver fora do plantão até uma hora do jogo após o horário, marcar a consulta como perdida. Retornos vão para **Retornos Pendentes**.
8. Ao excluir um retorno pendente, cancelar o compromisso e encerrar o acompanhamento, preservando o prontuário histórico.

## Modelo de dados e persistência

Separar estado persistente de entidades do GTA V. O arquivo deve conter pacientes, episódios de atendimento, compromissos e um identificador estável para cada um. `Ped`, blips, rotas, animações, reservas de sala, exames em execução e outros objetos do jogo são reconstruídos ou descartados ao carregar.

O compromisso precisa registrar: ID, ID do paciente/prontuário, origem (`Novo` ou `Retorno`), data e hora do jogo, status (`Agendado`, `Na recepção`, `Em atendimento`, `Atendido`, `Pendente` ou `Cancelado`) e vínculo com o episódio. O prontuário precisa conservar identidade, perfil, doença, queixa, sintomas, resultados de exames, anotações, condutas e histórico de visitas. O formato terá versão para permitir migração futura.

Usar um arquivo JSON em `LumenSante` na instalação do plugin, com gravação atômica por arquivo temporário e substituição. Validar dados ao ler; registrar falhas no log e preservar o arquivo original quando estiver inválido. Salvar imediatamente após alterações de agenda ou prontuário e em pontos seguros do plantão. A ausência de arquivo inicia uma agenda nova. O arquivo precisa ser testado após descarregar e recarregar o plugin.

O `GameClock` atual só mede tempo decorrido desde o início da sessão. A agenda precisa de um serviço separado para ler a data completa e a hora do GTA V, calcular o dia da semana e comparar horários absolutos entre sessões. Saltos de tempo para a frente exigem reconciliação de consultas vencidas sem criar pacientes duplicados. Se o relógio do jogo retroceder, compromissos futuros continuam com suas datas originais; não devem ser disparados duas vezes. Nunca usar `DateTime.Now` ou outra fonte do sistema operacional como referência da agenda.

## Integração com o comportamento existente

- `HospitalShift.cs` mantém casos ativos, altas e históricos apenas em memória. `PatientCase` mistura dados clínicos e objetos do mundo. A persistência deve extrair um estado serializável, mantendo o ciclo de atendimento existente como consumidor desse estado.
- Hoje toda alta recebe um `ReturnAt` aleatório de 8 a 24 horas do jogo, e `UpdateDischargedPatients` pode trazer o paciente de volta automaticamente. Alta do jogador com retorno marcado deve usar somente a agenda, para impedir chegada duplicada. Alta do jogador sem retorno encerra seu acompanhamento. Retornos espontâneos de casos atendidos pela equipe podem continuar no fluxo clínico, sem ocupar a agenda pessoal.
- `ExamOrders` já conserva a mesma `AdmissionId`, `VisitCount` e resultados quando um paciente retorna na sessão atual. O carregamento persistente deve restaurar essa identidade e o histórico antes de permitir nova visita.
- `WaitingRoom.ReceiveReturningPatient` já encaminha um ped à recepção. A chegada agendada pode usar essa rota, mas precisa de uma fila própria para não perder horários quando a recepção estiver cheia.
- A Agenda será uma nova página do `BedManagementPanel`, acessível pelo computador e tablet existentes. A tela terá visão diária, semanal e **Retornos Pendentes**, com abertura do prontuário ligado ao compromisso.
- Doctors NPCs continuam o trabalho normal no hospital. As consultas da agenda pessoal são reservadas ao jogador; a ausência dele segue a regra de tolerância e pendência.

## Ordem de implementação

1. Criar o calendário do jogo e validar virada de dia, semana, mês, pausas e saltos de hora.
2. Criar contratos de dados com versão, armazenamento JSON e restauração de prontuários sem objetos do GTA V.
3. Criar o agendador de novos pacientes e retornos, com deduplicação e reconciliação após recarregar.
4. Integrar a alta do jogador, a chegada pela recepção e o ciclo de consulta existente.
5. Criar a página Agenda no computador e tablet, com visões de dia/semana, pendências e acesso ao prontuário.
6. Cobrir transições, falhas de gravação e limpeza de entidades; compilar e validar em GTA V.

## Critérios de aceitação

- Uma consulta agendada aparece no mesmo dia e horário do jogo antes e depois de recarregar o plugin.
- O paciente chega uma única vez pela recepção e mantém o mesmo prontuário em cada retorno.
- Uma alta do jogador gera retorno somente quando ele o solicita; alta de Doctor NPC não cria compromisso pessoal.
- O jogador pode atender pacientes espontâneos sem pausar, bloquear ou alterar a própria agenda.
- Um retorno não atendido por ausência do jogador após uma hora do jogo aparece em **Retornos Pendentes**; excluí-lo encerra o caso sem apagar o prontuário.
- Recepção cheia, falha ao criar ped, pausa e saltos de relógio não duplicam compromissos nem perdem prontuários.
- A agenda do computador e do tablet mostra os mesmos dados persistidos.

## Ajustes futuros

- Revisar os horários e a frequência de novos pacientes após jogar com a agenda.
- Avaliar o reagendamento de retornos pendentes e a evolução clínica enquanto o paciente aguarda em casa.

## Validação

Execute `Skills/lumen-sante-gameplay/scripts/Validate-MedicalAgenda.ps1` após a compilação Release. A integração com o calendário, os NPCs e a interface deve ser conferida no GTA V.
