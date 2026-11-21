# Roteiro e aceitação

| Etapa | Entrega | Critério de saída |
|---|---|---|
| 1. Prova de integração | DLL mínima, instruções locais de compilação e matriz inicial de versões | Carregar com LSPDFR ativo, emitir mensagem de diagnóstico, descarregar e recarregar sem duplicar recursos |
| 2. Hospital | Ponto de interação e sessão vazia | Iniciar e encerrar dois plantões consecutivos no local validado |
| 3. Primeira admissão | Admissão automática, alerta e conversa no quarto | Iniciar o plantão, receber a identificação do paciente e do quarto, encontrar o NPC na cama e ouvir o relato |
| 4. Primeira versão jogável | Avaliação, desfecho, liberação de leitos e segundo caso | Concluir os atendimentos e registrar os resultados no resumo do plantão |

## Validação antes de distribuir

- Executar os dois casos de ponta a ponta e conferir que cada ficha habilita apenas seus desfechos válidos.
- Tentar concluir antes do resultado e verificar que a interface explica qual etapa falta.
- Acionar a mesma interação rapidamente e confirmar que só existe um atendimento ativo.
- Interromper um caso, reiniciar o plantão e verificar que o ciclo continua sem NPCs, menus ou marcadores duplicados.
- Simular falha de criação e perda do NPC ativo, verificando recuperação da fila.
- Validar carregamento, morte do jogador, descarregamento e coexistência com LSPDFR fora e durante o serviço policial.
- Registrar build do GTA V Legacy, versões de RPH e LSPDFR, SDK, framework de destino, método de carregamento, resultado e caminho do log. Antes de distribuir, documentar a instalação e remoção com base nessa combinação testada.

## Decisões da prova de integração

| Questão | Como resolver |
|---|---|
| Onde fica o ponto de plantão? | Inspecionar acesso a pé e espaço para NPCs no mapa base e registrar coordenadas no protótipo |
| Como o plugin carrega no ambiente instalado? | Testar o mecanismo de carregamento com os builds disponíveis e registrar o fluxo validado |
| Qual entrada evita conflitos? | Testar a interação com os atalhos padrão do ambiente de teste e oferecer configuração |
| Há dependência direta da API do LSPDFR? | Decidir após verificar comportamento durante o serviço policial e identificar quais sinais seriam necessários |

## Teste do menu de plantão

1. Compile a solução. Copie `RAGENativeUI.dll` da saída da compilação para a pasta principal do GTA V, ao lado de `RAGEPluginHook.exe`. Coloque `lumen-sante.dll` na pasta `Plugins` usada na prova de carregamento.
2. Carregue o plugin pelo RAGE Plugin Hook. O menu pode ser testado com o LSPDFR desativado.
3. Entre a pé no Central Los Santos Medical Center e vá até a recepção, nas coordenadas (354, -1397, 32). Pressione **E** para iniciar o plantão. Abra o menu novamente no mesmo ponto para encerrá-lo.
4. Repita o ciclo, confira as notificações e verifique os registros de início e fim no log. Saia da área com o menu aberto e confirme que ele fecha. Descarregue o plugin com o menu aberto e verifique se a interface desaparece e se a recarga começa com o plantão encerrado.

## Teste da primeira admissão

1. Inicie o plantão na recepção ou execute `LumenSantePlantao` no console do RAGE Plugin Hook. Aguarde 10 segundos pelo alerta com o nome sorteado e o Room 1.
2. Vá até (357, -1416, 36) e pressione **E**. Abra **Sinais vitais** e confira os valores. Em **Conversa com Paciente**, faça uma pergunta e confira a resposta na legenda. Verifique também se as duas categorias de exames aparecem.
3. Confira o alinhamento do NPC com o colchão. Use F6 voltado da cabeceira para os pés para registrar a direção junto das coordenadas.
4. Encerre o plantão pela recepção ou com `LumenSantePlantao` e confirme que o leito é liberado. Inicie outro turno e confira se há apenas um paciente com nome e aparência sorteados.
