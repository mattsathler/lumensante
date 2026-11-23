# Salário e carteira por médico

O médico recebe salário ao encerrar o plantão. O resumo mostra o pagamento e o saldo disponível. A carteira pertence ao perfil médico do Lumen Santé e fica disponível na próxima sessão.

## Primeira entrega proposta

- Associar o plantão ao ID permanente do médico quando ele começar. A troca de roupa mantém o vínculo com esse perfil. Morte, descarregamento e troca externa de personagem seguem o fluxo de interrupção.
- Usar a mesma rotina de encerramento no menu da recepção e no comando do console. Ao concluir um plantão elegível, registrar um único crédito pelo identificador do plantão.
- Guardar saldo e lançamentos em um arquivo local do plugin, com valores inteiros na menor unidade da moeda. Gravar o crédito e o saldo juntos, substituindo o arquivo só depois de preparar a nova versão. Exibir o pagamento após a confirmação da gravação.
- Se a gravação falhar, informar que o pagamento está pendente e permitir nova tentativa com o mesmo identificador de plantão. Se a carteira estiver ilegível, preservar o arquivo e sinalizar a falha para recuperação.
- Exibir o saldo no menu do médico. Futuras compras do Lumen Santé consultam essa carteira e registram um débito após validar os fundos.

## Compras no GTA V

Compras em lojas e serviços do modo história são uma integração posterior, a validar serviço por serviço. Em 2019, LMS explicou que o LSPDFR usava os fundos de Michael em certas interações com personagens personalizados. O LSPDFR Expanded documenta outra abordagem, com salário, compras e arquivos salvos por personagem. Essas referências orientam a investigação. O Lumen Santé mantém a carteira vinculada ao seu próprio perfil. [Explicação do LMS](https://www.lcpdfr.com/forums/topic/98110-money-on-lspdfr-character/) · [LSPDFR Expanded](https://www.lcpdfr.com/downloads/gta5mods/scripts/46108-lspdfr-expanded/)

## Decisões pendentes

- Valor do salário, critérios de elegibilidade e eventual pagamento de plantões interrompidos.
- Lojas e serviços do GTA V que valeria integrar e qual saldo cada um consulta.

## Critérios de aceitação

1. Concluir um plantão elegível pelo menu e confirmar o crédito e o saldo no resumo.
2. Repetir o encerramento ou tentar pelo console em seguida. O mesmo plantão gera um único lançamento.
3. Reiniciar o jogo e conferir saldo e histórico. Simular falha de gravação e confirmar que a nova tentativa não duplica o crédito.
4. Trocar de médico fora de plantão e de uniforme durante o turno. Confirmar que cada médico mantém seu saldo e o vínculo com seu plantão.
5. Testar morte e descarregamento durante o plantão e conferir a regra de pagamento definida para interrupções.
