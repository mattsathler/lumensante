# Vida fora do hospital — lotes de desenvolvimento

O [catálogo de residências](catalogo-residencias-nativas.md) contém a pesquisa de imóveis e referências. Os lotes abaixo organizam implementação e validação, não representam promessa de compatibilidade antes do teste no GTA V Legacy.

## Ciclo I

1. **Base residencial e primeiro grupo:** perfil com imóvel próprio, seleção e entrada/saída; quatro endereços usando as plantas econômicas e médias. Candidatos: 140 Zancudo Avenue em Sandy Shores; 1893 Grapeseed Avenue em Grapeseed; 4401 Procopio Drive em Paleto Bay; 0115 Bay City Avenue em Los Santos. Validar pontos externos e internos, colisão, iluminação, câmera, troca de médico e saída após falhas e reinício.
2. **Inicialização e rotina:** distinguir inicialização do jogo com plugin carregado de carga manual, restaurar o médico em sua casa somente no contexto desejado, e calibrar cenas canceláveis por planta, começando por estar sentado ou acordado. Adicionar outras atividades conforme os recursos comprovados em jogo.
3. **Mais endereços das plantas já disponíveis:** ampliar o catálogo econômico e médio; cada endereço precisa de entrada, saída, orientação, marcação e espaço para futuro carro, sem duplicar a definição da planta.
4. **Outras plantas:** apartamentos altos, High Life, penthouses e casas de alto padrão. Investigar IPLs, culling e conflitos com o exterior antes de oferecer cada opção; avaliar casas do modo história em lote separado por seu vínculo com personagens e missões.
5. **Conta e compras:** comprovar o mecanismo usado por máquinas e serviços nativos com personagem freemode, persistir conta por médico e implementar integração isolada dos fundos dos protagonistas. Confirmar débitos, reinícios, interrupções e dois perfis antes de declarar os serviços compatíveis.

## Ciclo II

6. **Salário fixo:** pagamento único por plantão elegível, persistente por médico e recuperável após falhas; definir valor e condições para interrupção.
7. **Veículo pessoal:** salvar e restaurar modelo, aparência, estado e posição por perfil, evitando duplicação, com política definida para destruição e recuperação.
8. **Expansão da rotina:** novas atividades e funcionalidades conforme a experiência desejada e a capacidade das plantas verificadas.

## Estado atual

O lote 1 está em desenvolvimento. O menu F9 permite escolher um dos quatro endereços fora de plantão; a interação E próxima da entrada ou da saída troca de ambiente. Um identificador é persistido por médico; imóveis econômicos e médios compartilham suas respectivas plantas. O comando `LumenSanteCasa` também permite testar a transição para o imóvel escolhido. As coordenadas de terceiros não foram calibradas em jogo e o carregamento pode não funcionar no Legacy. A troca de médico e de endereço fica bloqueada enquanto o médico está dentro da residência.

No teste relatado em 24/11/2026, as casas de Paleto e Zancudo abriram e suas saídas funcionaram; Grapeseed e Bay City não ficaram prontas. A versão seguinte ajusta temporariamente o foco de streaming para o interior durante o carregamento e registra o imóvel e a duração no log se houver falha. Isso é uma hipótese para corrigir a diferença; ainda precisa ser testada. Um blip de casa marca a entrada do imóvel selecionado e, quando dentro, passa a marcar a saída.

Em novo relato, Grapeseed e Bay City continuaram sem carregar. Como compartilham as mesmas duas plantas usadas por Zancudo e Paleto, respectivamente, as plantas existem na instalação testada. A nova tentativa verifica se o interior está desabilitado ou bloqueado e o ativa antes de esperar a prontidão, além do foco de streaming. Essa alteração precisa de teste em jogo; não há confirmação de que resolva todos os casos.

No teste seguinte o interior estava desabilitado no início da tentativa e continuou sem carregar, enquanto o atalho do celular funcionou. A nova versão inicia uma carga de cena para o destino quando disponível, verifica periodicamente se o interior voltou a ser desabilitado e registra estados inicial e final, quantidade de ativações e início da carga de cena. O resultado ainda depende de teste no GTA V.

A restauração automática começa fora de plantão. Quando o plugin inicia enquanto `Game.IsLoading` está ativo e há médico padrão, ele tenta posicioná-lo na residência após o carregamento, desde que o jogador esteja disponível e fora de missão ou cena. A tentativa é cancelada após um minuto se não houver contexto seguro. A carga manual durante uma sessão preserva a posição. O RPH pode iniciar plugins quando a tela de carregamento já terminou; nesse caso não há evidência suficiente para distinguir essa inicialização de uma carga manual, e a posição é preservada. O log informa se o carregamento inicial foi detectado. A detecção precisa ser validada na instalação do usuário.

Após relato de que o GTA aplicava o médico antes de restaurar o personagem e o local do save, a restauração do perfil também aguarda o jogador ter controle, tela visível, colisão carregada, nenhuma troca de personagem ou missão, e o mesmo personagem e posição estáveis por quatro segundos. Carregamento, pausa, mudança de personagem ou deslocamento reiniciam a espera. Essa proteção é usada também ao carregar manualmente o plugin, sem solicitar o teletransporte para casa. O fim da tela de carregamento por si só não é tratado como prova de que o save já está pronto.

Ao chegar automaticamente, o médico inicia uma cena em pé (celular ou bebida) por até 20 segundos, cancelável por movimento, interação ou abertura do menu. Estas são cenas iniciais, ainda sem pontos calibrados para cama, TV ou banho; testar visualização, objetos, entrada e cancelamento no jogo. Ao recarregar dentro de uma planta, a ocupação é reconhecida pelo interior e imóvel do perfil ativo; essa recuperação ainda precisa de teste no jogo. Conta, compras, salário e carro ainda não estão implementados.
