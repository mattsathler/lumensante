# Estudo: vida fora do hospital

**Status: implementação inicial experimental, ainda sem validação no GTA V.** Alvo: modo história do GTA V Legacy com RAGE Plugin Hook. Referências de FiveM abaixo servem para identificar recursos do jogo, não provam que funcionem no modo história.

## Primeira implementação de teste

O médico padrão é restaurado fora de plantão. O primeiro lote oferece quatro endereços, selecionáveis no menu F9 e vinculados ao perfil. O médico pode interagir com a entrada ou saída usando E, ou usar `LumenSanteCasa` para testar o imóvel selecionado. A inicialização em casa e duas cenas breves em pé estão implementadas para cargas detectadas durante a tela de carregamento do jogo; a detecção e as cenas ainda precisam de teste no Legacy. Não há conta financeira nesta etapa. Veja [lotes de desenvolvimento](lotes-vida-fora-hospital.md).

O carregamento possui prazos e retorna à posição anterior se a colisão não ficar pronta. O usuário confirmou a entrada e saída dos apartamentos; o reconhecimento após recarregar dentro da residência ainda depende de validação.

## Objetivo e ciclos

O médico inicia sua rotina em uma residência, fora de plantão, e pode sair para a cidade e dirigir-se ao hospital. Cada perfil mantém seus próprios dados. A conta deve permitir que os sistemas nativos de compra reconheçam seus recursos, sujeito à comprovação por tipo de compra.

**Ciclo I — casa e integração financeira:** restaurar o médico padrão em uma residência inicial, entrar e sair com transições, apresentar uma atividade inicial e estabelecer uma conta por médico integrada aos pontos de compra nativos comprovadamente compatíveis. A primeira versão pode usar a mesma planta e endereço para todos os perfis, com dados separados; a atribuição de residências distintas fica preparada por um identificador de imóvel. Não há simulação de fatos durante o tempo em que o jogo ficou fechado: a cena inicial representa uma rotina, sem alegar que ela ocorreu de verdade.

**Ciclo II — continuidade:** salário fixo ao encerrar plantão elegível, com crédito único por identificador de plantão; veículo pessoal persistente por médico; expansão de rotinas e recursos após definir regras com o usuário. Salário e veículo não bloqueiam a comprovação do Ciclo I.

## Base atual

- `DoctorProfiles` já identifica médicos por GUID, salva `doctors.json` e usa gravação temporária seguida de substituição com backup. Aparência e roupa casual já são específicas do perfil; a agenda também tem caminho por GUID. Saldo, residência e carro ainda não existem nesses perfis.
- `HospitalShift.Run` cria os perfis e agora restaura o médico sem iniciar o plantão automaticamente; o comando e a entrada de plantão pelo hospital continuam disponíveis.
- `PluginEntryPoint.Main` inicializa recepção, população e sala de espera antes de chamar o coordenador. A execução fora do hospital torna necessário avaliar criação e atualização por proximidade para não gerar NPCs remotos desnecessariamente; não presumir que já sejam suspensos.
- [Salário e carteira](salario-e-carteira.md) é uma proposta anterior, não funcionalidade já entregue. A exigência atual antecipa a integração com compras para o Ciclo I e move salário fixo ao Ciclo II. A decisão sobre transporte do dinheiro substitui a premissa de uma carteira isolada sem integração.

## Residência nativa

O [catálogo de residências nativas](catalogo-residencias-nativas.md) reúne os endereços e as famílias de interiores pesquisadas, incluindo Sandy Shores, Grapeseed e Paleto Bay.

Investigar primeiro um apartamento do conteúdo original, com mobília e saídas verificáveis no Legacy. Como candidato de pesquisa, o projeto [bob74_ipl documenta um apartamento executivo e seus IPLs](https://github.com/Bob74/bob74_ipl/blob/master/dlc_executive/apartment3.lua); é uma referência para localizar recursos, não garantia de disponibilidade, colisão ou aparência no modo história. Verificar também um interior residencial do modo história como alternativa caso o apartamento de conteúdo online dependa de carregamento incompatível. Selecionar o imóvel após conferir disponibilidade no jogo, entrada externa, distância do hospital, iluminação, pontos de câmera, colisão e atividades.

Manter uma definição de residência separada dos perfis: ID estável, localização e orientação da entrada externa, coordenadas internas de chegada/saída, IPLs necessários, locais das cenas, recursos e zona de garagem futura. As coordenadas e os nomes só serão fixados após calibração no Legacy; IDs de interior obtidos por outro ambiente não são assumidos estáveis. `REQUEST_IPL` e `PIN_INTERIOR_IN_MEMORY` são recursos nativos documentados ([IPL](https://raw.githubusercontent.com/citizenfx/natives/master/STREAMING/RequestIpl.md), [interior](https://raw.githubusercontent.com/citizenfx/natives/master/INTERIOR/PinInteriorInMemory.md)); a ordem exata e a necessidade de cada chamada dependem do imóvel escolhido.

Entrada/saída proposta: validar personagem e contexto; fade; solicitar recursos e colisão; aguardar prontidão com prazo; posicionar em ponto seguro; configurar câmera/visibilidade necessária; fade de volta. Na falha, voltar ao ponto externo seguro, restaurar controles e comunicar o erro. Liberar apenas recursos adquiridos pelo plugin, sem descarregar IPLs usados pelo jogo ou outros mods; cobrir morte, descarregamento e transição interrompida. Sem perfil padrão, preservar o fluxo de criação do médico e não teleportar o personagem do modo história.

## Inicialização e rotina

Inicialização residencial deve acontecer somente quando houver um médico padrão válido, depois de o mundo e o personagem estarem prontos. Distinguir carregamento no início do jogo de carregamento manual ou recarga do plugin: investigar se o RPH fornece sinal confiável; se não fornecer, não usar apenas tempo desde a inicialização como prova. Em carga manual, preservar posição e atividade do jogador por padrão, com uma opção explícita para ir à residência. Nunca interromper missão, cutscene, morte, transição ou outro estado incompatível.

Escolher inicialmente uma cena dentre opções calibradas, como sentado assistindo TV, parado ou acordando. Banho depende de animação, orientação, privacidade visual e de o interior realmente oferecer o espaço; não afirmar que já é possível apenas por existir o cômodo. A cena é cancelável, com controles recuperáveis, fallback em pé e restauração segura de roupas. Cada ponto armazena posição/orientação e recursos; não travar o personagem enquanto aguarda indefinidamente.

Definir o imóvel por perfil sem alterar automaticamente a casa ou posição ao trocar de médico no mundo: a política de troca em sessão e de retorno à casa precisa de decisão explícita. A seleção de médico e acesso à agenda devem continuar disponíveis fora de plantão.

## Conta e compras: principal prova técnica

Usar o GUID do médico para manter um saldo durável, isolado e com gravação atômica, protegendo os dados de protagonistas do modo história. Antes de escolher uma ponte, testar em GTA V Legacy 1.0.3889.0 quais scripts de máquina de refrigerante, loja de roupas e outro serviço de fato leem/debitam o dinheiro quando o ped é freemode. O [código do ScriptHookVDotNet](https://github.com/scripthookvdotnet/scripthookvdotnet/blob/main/source/scripting_v3/GTA/Player.cs) associa a propriedade de dinheiro aos três protagonistas e retorna sem suporte para outros modelos. Isso é evidência sobre essa API, não prova de como cada script nativo resolve dinheiro ou se um banco separado é utilizado.

Avaliar, em cópia de teste do save: (A) sincronização temporária com o campo nativo efetivamente usado pelas compras, capturando débitos e restaurando o saldo original do protagonista; (B) redirecionamento controlado de leituras e escritas para a carteira do perfil, se for tecnicamente seguro e compatível com RPH; (C) adaptação por serviço se a integração geral não for viável. Não assumir que polling detecta toda transação nem prometer isolamento total: saves automáticos, crash, outro mod e múltiplas escritas podem gerar perda, duplicação ou contaminação do save. Redirecionamento por hook pode depender de versão, exigir componentes nativos e gerar conflitos; não implementá-lo sem prova e estratégia de recuperação.

O protótipo financeiro deve identificar qual saldo a máquina efetivamente consulta e altera, se o freemode pode interagir, se a mudança sobrevive ao save e quais outros sistemas a usam. Testar médico A/B, dinheiro insuficiente, compra repetida, reload, crash simulado, troca de modelo e abandono do plugin. Se a ponte não preservar os fundos originais e o saldo por médico, registrar a limitação e não apresentar a integração como pronta. Valor inicial, separação entre dinheiro e banco e tratamento de débitos externos continuam decisões de produto.

## Ciclo II: salário e carro

Salário: vincular o turno ao GUID do médico e a um ID único; creditar no encerramento elegível usando o mecanismo de conta já validado. Registrar crédito e identificador juntos, com recuperação de falhas e repetição idempotente. Definir remuneração e regras para interrupção, morte e descarregamento. Atualizar [a proposta anterior](salario-e-carteira.md) quando o contrato de conta for escolhido.

Carro: armazenar por GUID modelo, placa, cores, modificações relevantes, condição e posição/orientação, além de uma identidade própria e estado explícito (em uso, guardado, destruído ou recuperação pendente). `IsPersistent` durante a sessão não substitui salvar e recriar a entidade entre sessões. Recriar apenas uma instância válida, com carregamento do modelo e colisão, prevenção de duplicatas e fallback se local estiver bloqueado. Definir política para destruição, apreensão, troca de veículo, reparos, despesas e quando salvar; não sobrescrever os veículos pessoais do modo história.

## Plano de validação

1. **Exploração em jogo:** comparar candidatos residenciais no Legacy; testar carregamento, saída/entrada, reinício, colisão e interação sem instalar outro interior.
2. **Prova isolada financeira:** registrar antes/depois das compras em diferentes serviços e modelos, inclusive os saldos do modo história; avaliar isolamento e recuperação sem comprometer o save usado pelo jogador.
3. **Integração do Ciclo I:** médico padrão nasce na casa, fora de plantão, e uma cena pode ser cancelada; médico sem perfil não é teleportado; chegada ao hospital e início/encerramento seguem funcionais; compra suportada debita somente o perfil correto e persiste.
4. **Robustez:** iniciar jogo com plugin carregado, carregar manualmente, recarregar, trocar médico, testar morte, missão/cutscene, falta de recursos e falhas de disco. Verificar agenda, roupas, controles e ausência de NPCs duplicados.
5. **Ciclo II:** simular falha antes/depois do crédito para evitar duplicação; reiniciar e trocar médico com o veículo em locais distintos; exercitar destruição e recuperação conforme regra escolhida.

Testes automatizados podem cobrir isolamento por GUID, serialização, migração de dados antigos, transações e idempotência. Streaming, cenas, compras, posição e comportamento de entidades exigem teste no jogo. Nenhum resultado de teste in-game foi produzido neste estudo.
