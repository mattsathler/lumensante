# Plantão e casos

## Fluxo de jogo

1. Iniciar o plantão na recepção do Central Los Santos Medical Center.
2. A equipe admite pacientes automaticamente e encaminha cada um a um leito livre.
3. O jogador recebe um alerta na interface com o nome do paciente e o quarto onde ele deu entrada.
4. No quarto, o jogador se aproxima da cama e usa **Interagir** para ouvir o relato do paciente.
5. Nas próximas etapas, o médico solicita exames, interpreta os resultados, registra uma hipótese diagnóstica e escolhe um tratamento. O caso define a condição do paciente e o jogo avalia a decisão do médico.

A primeira entrega terá uma admissão automática em um quarto validado. O NPC aparece no leito com nome e aparência sorteados. O alerta sai depois que ele estiver disponível. O relato apresenta sintomas, como tosse frequente, para o médico investigar. A chegada será simulada nessa entrega. A caminhada da entrada até o quarto poderá ser adicionada depois.

## Estados propostos

| Estado | Comportamento |
|---|---|
| Aguardando admissão | O plantão está ativo e o hospital aguarda uma chegada automática. |
| Internado | O leito fica ocupado e o jogador recebe o nome e o quarto. |
| Sintomas relatados | O jogador ouviu o relato. O leito segue ocupado entre plantões. |
| Interrompido | O plantão encerrou. O caso fica preservado na sessão e a progressão clínica pausa até o próximo turno. Se o NPC desaparecer, ele é recriado ao retornar ao hospital. |

Quando houver vários quartos, a equipe escolhe um leito livre. O primeiro protótipo valida um NPC e uma cama. Cada leito precisa do nome ou número do quarto, da posição de colocação do NPC, da orientação da cama e de um ponto no chão para a interação do médico.

## Caso 1 — Câncer de pulmão

O paciente começa no nível 1 de gravidade na admissão. Cada doença define uma chance de piora por avaliação e uma taxa de sucesso do tratamento. O câncer de pulmão usa inicialmente 55% de risco e 40% de sucesso base, com avaliações em intervalos aleatórios de duas a quatro horas do jogo por paciente. O nível 10 registra o óbito. Os percentuais e intervalos são parâmetros de balanceamento do jogo.

Na central do hospital, abra o prontuário do paciente e a aba **Tratamentos**. A lista mostra o tratamento e a doença correspondente, permitindo ao jogador escolher uma hipótese. Há uma única seleção por paciente, e uma nova escolha substitui a anterior. Use a roda do mouse, Page Up/Page Down ou as setas da lista para percorrer catálogos maiores. O catálogo atual oferece **Terapia oncológica para Câncer de Pulmão**. A escolha permanece entre plantões.

O tratamento correto muda as chances conforme a gravidade. No câncer de pulmão, o estágio inicial tem 80% de chance de melhorar e 8,25% de piorar por avaliação. No estágio 9, essas chances são 14% e 71,5%. Os valores intermediários variam linearmente. Um único sorteio decide melhora, piora ou estabilidade. Um tratamento para outra doença mantém o risco de piora sem conceder a chance de melhora do tratamento correto. Melhoras, pioras, seleção de tratamento e óbitos notificam o jogador.

Pacientes internados com tratamento selecionado recebem visitas de enfermeiros disponíveis. A primeira visita fica pendente após a seleção e as próximas ocorrem entre 30 e 60 minutos do jogo após cada visita, que dura cinco minutos do jogo. A equipe usa as rotas existentes e coordena a disponibilidade com os exames. As visitas continuam fora do plantão do jogador.

Casos, pedidos de exames e tratamento permanecem em memória entre plantões. Ao descarregar o plugin ou fechar o jogo, os casos são encerrados. A recriação de um NPC preserva modelo, identidade e estado clínico do caso, colocando o internado no leito reservado e o paciente de consulta na cadeira da sala.

Pedidos pendentes do mesmo paciente para MRI ou laboratório são agrupados em uma única visita à sala. Novos pedidos compatíveis podem entrar no grupo enquanto a equipe se desloca ou se prepara, até começar a realização. O grupo usa a duração do pedido mais demorado e conclui todos os resultados juntos. Cada exame mantém seu pedido, estágio de solicitação e laudo individuais. Uma interrupção cancela todos os pedidos do grupo, permitindo solicitá-los novamente. Pedidos de outros pacientes ou salas mantêm sua ordem na fila. O catálogo atual possui 51 exames, divididos em 27 laboratoriais, 12 de MRI, 11 de RX e um exame cardíaco à beira do leito. No menu de solicitação, abra **Exames** e escolha **Laboratório**, **Ressonância magnética (MRI)**, **Radiografia (RX)** ou **À beira do leito**. Os exames aparecem em ordem alfabética dentro de cada grupo, com rolagem e estado individual de cada pedido. A lista de exames da central tem rolagem pela roda do mouse, Page Up/Page Down e setas.

À beira do leito, pressione E. A ficha abre as opções **Sinais vitais**, **Conversa com Paciente** e **Exames**. As perguntas cobrem os sintomas atuais, início, falta de ar, sangue ao tossir e perda de peso. A doença fica oculta na interface para futura investigação. Em **Exames**, o médico seleciona um item **A solicitar** para enviar o pedido à equipe. O estado passa a **Aguardando realização** e, após 10 a 60 minutos do jogo conforme o exame, a **Realizado**. O pedido é registrado uma vez por exame e admissão durante o plantão. O acesso aos resultados será implementado na central do hospital.

Os sintomas do caso foram inspirados na descrição do [National Cancer Institute](https://www.cancer.gov/types/lung/patient/non-small-cell-lung-treatment-pdq).

Os pacientes de ambiente permanecem de 90 a 180 minutos do jogo nos anchors sentados e de 60 a 120 minutos nos anchors em pé. Esse intervalo aumenta a permanência dos pacientes enquanto os funcionários continuam circulando. As salas de consulta são identificadas como Diagnóstico II e Diagnóstico III. No leito, a opção **Conversa com Paciente** oferece as mesmas perguntas da consulta, com respostas do estágio atual.

Os novos exames incluem MRI de crânio e coluna, RX de costelas, pelve e membros, gasometria arterial, coagulograma, glicemia, proteína C reativa e urina tipo I. Os laudos são achados simulados do caso de jogo. As modalidades e os tipos de amostra foram consultados nas descrições de exames do [National Cancer Institute](https://www.cancer.gov/about-cancer/diagnosis-staging/diagnosis) e de investigação do câncer de pulmão da [American Cancer Society](https://www.cancer.org/cancer/types/lung-cancer/detection-diagnosis-staging/how-diagnosed.html).
A expansão para 51 exames inclui testes de minerais, função tireoidiana, ferro, perfil lipídico, hemoglobina glicada, lactato, marcadores cardíacos e culturas, além de MRI e RX de mais regiões do corpo. Os tempos de 10 a 60 minutos do jogo são balanceamento da simulação, incluindo tempos condensados para culturas. Os novos laudos usam achados basais simulados e os exames já existentes mantêm os achados associados à gravidade do caso. As descrições de testes e amostras foram consultadas no [MedlinePlus](https://medlineplus.gov/lab-tests/) e as regiões examinadas por MRI na [FDA](https://www.fda.gov/radiation-emitting-products/mri-magnetic-resonance-imaging/benefits-and-risks).
Ao abrir **Exames** no menu do paciente ou seus grupos de exames, o médico segura uma prancheta. O Lumen Santé cria o objeto e executa a animação do GTA automaticamente. Ao fechar o menu, encerrar o plantão, trocar de personagem ou descarregar o plugin, a animação é encerrada e o objeto é removido. A referência visual é [Clipboard! (and notepad)](https://www.lcpdfr.com/downloads/gta5mods/scripts/40964-clipboard-and-notepad/). A integração usa o prop e a animação do próprio GTA.

A cama do Room 1 usa a posição (356.498, -1417.208, 37.415) e orientação de 45.9 graus.
Ao chamar o próximo paciente, o hospital reserva um enfermeiro disponível. Ele vai até a recepção e ao banco do paciente. O paciente aguarda sentado até a chegada do enfermeiro, levanta e acompanha o trajeto até a cadeira da sala de diagnóstico. O enfermeiro volta à circulação após a entrega. Se não houver enfermeiro livre, o jogador recebe um aviso e pode repetir a chamada. Interrupções do acompanhamento preservam o caso e permitem uma nova tentativa após cinco minutos do jogo. A inspeção geral e os sinais vitais no leito executam as animações do médico, voltado ao paciente.

A seleção de pacientes usa uma lista de civis urbanos comuns de Los Santos. A lista inclui moradores de bairros, trabalhadores de escritório, pedestres de rua, corredores e frequentadores das praias. A disponibilidade dos modelos é conferida na instalação do jogo, e a seleção mantém o embaralhamento, a preferência por modelos ausentes e as variações de roupas e acessórios. Os nomes dos modelos foram conferidos na [referência de peds da Cfx](https://docs.fivem.net/docs/game-references/ped-models/).