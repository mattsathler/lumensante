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
| Sintomas relatados | O jogador ouviu o relato. O leito segue ocupado até a etapa de desfecho ou o fim do plantão. |
| Interrompido | O plantão encerrou ou o NPC ficou indisponível. O plugin libera o leito e limpa seus recursos. |

Quando houver vários quartos, a equipe escolhe um leito livre. O primeiro protótipo valida um NPC e uma cama. Cada leito precisa do nome ou número do quarto, da posição de colocação do NPC, da orientação da cama e de um ponto no chão para a interação do médico.

## Caso 1 — Câncer de pulmão

O paciente começa no nível 1 de gravidade na admissão. A cada dois minutos, o quadro avança um nível e atualiza o relato e os sinais vitais. O nível 10 registra o óbito. Esses níveis, o tempo e os valores vitais são regras da simulação.

À beira do leito, pressione E. A ficha abre as opções **Sinais vitais**, **Conversa com Paciente**, **Encaminhar para exame** e **Solicitar exames**. As perguntas cobrem os sintomas atuais, início, falta de ar, sangue ao tossir e perda de peso. A doença fica oculta na interface para futura investigação. As opções de exames já aparecem nos menus e permanecem indisponíveis nesta etapa.

Os sintomas do caso foram inspirados na descrição do [National Cancer Institute](https://www.cancer.gov/types/lung/patient/non-small-cell-lung-treatment-pdq).
