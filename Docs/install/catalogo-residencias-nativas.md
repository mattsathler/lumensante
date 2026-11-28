# Catálogo de residências nativas — estudo

**Situação:** pesquisa de candidatos para GTA V Legacy em modo história, sem teste próprio em jogo. As posições e nomes abaixo são referências para investigação, não pontos de spawn seguros nem opções já implementadas. Conteúdo nativo significa arquivos do próprio jogo, inclusive conteúdo originalmente lançado para GTA Online; não significa que todo interior esteja disponível no modo história. Este levantamento reúne as famílias residenciais encontradas nas fontes consultadas, mas não comprova que esgote todos os interiores, endereços ou atualizações do jogo.

Uma **planta/interior** é o espaço renderizado; um **endereço** é a porta no mundo. Várias casas de Blaine County e apartamentos de Los Santos podem compartilhar a mesma planta subterrânea. Uma coordenada da planta não deve ser usada como endereço externo. O perfil do médico precisa preservar o imóvel específico, não apenas a planta.

## Interiores de base com endereços mapeados para modo história

O projeto de terceiros [InteriorsV](https://github.com/SuleMareVientu/InteriorsV-ScriptHookV) descreve 9 entradas econômicas, 13 médias e 17 de alto padrão para seu mod single-player. O próprio [código de mapeamento](https://raw.githubusercontent.com/SuleMareVientu/InteriorsV-ScriptHookV/main/InteriorsV/source/utils/onlineteleports.cpp) associa os pontos externos a interiores. Isso é evidência de viabilidade e uma lista de pesquisa, não validação no Lumen Santé, no RAGE Plugin Hook ou nesta versão do GTA. O código usa tratamento de visibilidade para alguns prédios, portanto simples teletransporte não basta presumivelmente.

### Econômico: um interior compartilhado, nove entradas

Referência para localizar o interior: `261.4586, -998.8196, -99.00863`; referência de entrada no interior usada pelo InteriorsV: `265.1292, -1000.9500, -100.0086`. Valores são amostras de terceiros e precisam de calibração.

| Endereço | Região aproximada |
|---|---|
| 140 Zancudo Avenue | Sandy Shores / Blaine County |
| 1893 Grapeseed Avenue | Grapeseed / Blaine County |
| 2143 Las Lagunas Boulevard, apt. 9 | Los Santos |
| 0069 Cougar Avenue, apt. 19 | Los Santos |
| 1237 Prosperity Street, apt. 21 | Los Santos |
| 1561 San Vitas Street, apt. 2 | Los Santos |
| 1115 Boulevard Del Perro, apt. 18 | Los Santos |
| 2057 Vespucci Boulevard, apt. 1 | Los Santos |
| 0112 South Rockford Drive, apt. 13 | Los Santos |

O [catálogo do interior econômico](https://github.com/Bob74/bob74_ipl/wiki/GTA-Online:-Low-end-house) também descreve variações de objetos. A proximidade da rota até o hospital e espaço para carro devem ser verificados em cada exterior.

### Médio: um interior compartilhado, 13 entradas

Referência para localizar o interior: `347.2686, -999.2955, -99.19622`; ponto de entrada de referência do InteriorsV: `346.5050, -1002.4710, -100.2062`. [Detalhes do interior médio](https://github.com/Bob74/bob74_ipl/wiki/GTA-Online:-Middle-end-house).

| Endereço | Região aproximada |
|---|---|
| 4401 Procopio Drive | Paleto Bay |
| 4584 Procopio Drive | Paleto Bay |
| 4 Hangman Avenue | Los Santos |
| 0115 Bay City Avenue, apt. 45 | Los Santos |
| 0184 Milton Road, apt. 13 | Los Santos |
| 12 Sustancia Road | Los Santos |
| 0504 South Mo Milton Drive | Los Santos |
| 0325 South Rockford Drive | Los Santos |
| Dream Tower, apt. 15 | Los Santos |
| 1162 Power Street, apt. 3 | Los Santos |
| 0605 Spanish Avenue, apt. 1 | Los Santos |
| 0604 Las Lagunas Boulevard, apt. 4 | Los Santos |
| The Royale, apt. 19 | Los Santos |

### Alto padrão: 17 entradas listadas pelo InteriorsV

| Prédio | Apartamentos mapeados |
|---|---|
| Eclipse Towers | 5, 9, 31, 40 |
| Weazel Plaza | 26, 70, 101 |
| Tinsel Towers | 29, 45 |
| Richards Majestic | 4, 51 |
| 4 Integrity Way | 30, 35 |
| 3 Alta Street Tower | 10, 57 |
| Del Perro Heights | 7, 20 |

A referência contém pontos externos e internos para cada entrada e rotinas específicas de oclusão/exterior. Essas rotinas são dependentes do ambiente e **não** foram transplantadas para o Lumen Santé. Avaliar risco de ocultar partes do mundo e interferir em outros mods antes de qualquer adaptação. Os números representam endereços, não necessariamente 17 plantas diferentes.

## Outras famílias do GTA Online e atualizações

Fontes abaixo descrevem recursos usando APIs do FiveM; a presença de um IPL ou interior ID não demonstra que esse recurso funciona em modo história Legacy. Os IDs numéricos de interior não devem ser tratados como estáveis entre versões. Em cada caso, testar IPL, ativação, colisão, iluminação, exterior e disponibilidade dos arquivos.

| Família | Opções encontradas | Observações |
|---|---|---|
| Apartamentos básicos de alto padrão | 4 Integrity Way, apt. 30; Del Perro Heights, apt. 7 | [Dois interiores catalogados](https://github.com/Bob74/bob74_ipl/wiki/GTA-Online:-High-end-apartments). Os mesmos endereços aparecem na lista do InteriorsV: conferir correspondência em vez de contar duas vezes. |
| Apartamentos High Life | Del Perro Heights 4; Richards Majestic 2; Tinsel Towers 42; Eclipse Towers 3; 4 Integrity Way 28; uma sexta instância sem endereço identificado | [Seis instâncias](https://github.com/Bob74/bob74_ipl/wiki/High-Life:-High-end-apartments). A sexta está abaixo do mapa; endereço e finalidade em aberto. |
| Penthouses executivas | Eclipse Towers, suites 1, 2 e 3 | [Três instâncias com oito temas](https://github.com/Bob74/bob74_ipl/wiki/Executives-%26-Other-Criminals:-Penthouses). A suite 3 moderna é o candidato do protótipo atual. Tema visual não equivale necessariamente a uma nova planta. |
| Casas de alto padrão | 3655 Wild Oats Drive; 2044 e 2045 North Conker Avenue; 2862, 2868 e 2874 Hillcrest Avenue; 2677 Whispymound Drive; 2133 Mad Wayne Thunder | [Oito instâncias catalogadas](https://github.com/Bob74/bob74_ipl/wiki/GTA-Online:-High-end-houses). Conferir a relação entre os endereços da fonte e a localização real antes de disponibilizar. |
| Cobertura do Diamond Casino | Penthouse do cassino | [Código da cobertura](https://github.com/Bob74/bob74_ipl/tree/master/dlc_casino): exige investigação das opções e dos recursos da atualização; é opção de escopo e complexidade maiores. |
| Suítes em propriedades de negócio e iates | Quartos ligados a escritórios, clubes ou embarcações | Possibilidades futuras se forem residenciais e compatíveis; inventário e entradas não verificados neste estudo. Uma garagem isolada não conta como casa. |
| Atualizações residenciais recentes | Propriedades adicionadas depois dos apartamentos clássicos | A [lista de carregamento do bob74_ipl](https://github.com/Bob74/bob74_ipl/blob/master/client.lua) inclui conteúdo por build, inclusive atualizações mais recentes. Confirmar que cada DLC existe no Legacy e que seus mapas/interiores carregam em SP antes de incluí-lo. Não presumir paridade com GTA Online atual ou Enhanced. |

### High Life: IPLs identificados

Os seis scripts `apartment1.lua` a `apartment6.lua` do [bob74_ipl](https://github.com/Bob74/bob74_ipl/tree/master/dlc_high_life) identificam a família `mpbusiness_int_placement_interior_v_mp_apt_h_01_milo_` e os sufixos `_1` a `_5` após o sublinhado final. Usar os nomes exatos dos arquivos de referência na implementação; não inferir sequências sem verificá-las. As coordenadas de ponto interno encontradas na wiki diferem das coordenadas usadas para localizar os interiores em alguns scripts, por vezes aproximadamente um metro no eixo Z. São pontos com funções diferentes, não valores intercambiáveis.

## Casas e ambientes residenciais do modo história

O [InteriorsV lista estes espaços](https://github.com/SuleMareVientu/InteriorsV-ScriptHookV#locations-only-los-santos-full-map-in-releases) como safehouses ou interiores de missão. O uso pelo médico pode conflitar com personagens, cenas, portas e progresso do enredo; requer avaliação individual.

| Candidato | Local | Observações |
|---|---|---|
| Casa da tia de Franklin / Clinton Residence | Sul de Los Santos | Casa de personagem do enredo; verificar ocupação, missões e portas. |
| 3671 Whispymound Drive / Franklin | Vinewood Hills | [Estados de decoração documentados](https://github.com/Bob74/bob74_ipl/wiki/GTA-V:-Franklin's-house); alterações podem afetar o modo história. |
| Casa de Michael | Rockford Hills | Interior residencial completo, porém associado ao protagonista e à família. |
| Trailer de Trevor | Sandy Shores | [Interior limpo ou bagunçado](https://github.com/Bob74/bob74_ipl/wiki/GTA-V:-Trevor's-trailer); ótima cobertura rural, mas fortemente associado ao enredo. |
| Apartamento de Floyd | Vespucci | Interior de missão; estado e acesso dependem do progresso. |
| Casa de Lester | El Burro Heights | Residência de personagem da história; avaliar rotinas e colisões com missões. |
| Apartamento de Harvey Molina | Los Santos | Interior de missão; verificar se pode servir como residência cotidiana. |
| Fazenda O'Neil | Blaine County | Estado potencialmente alterado por eventos da história; investigar interior, exterior e conflitos. |
| Quarto do Motor Motel | Blaine County / Harmony | Quarto em vez de casa; avaliar dimensões, saída e compatibilidade com a rotina. |

Não tratar uma fachada, garagem, loja, galpão, negócio ou quarto de missão como residência funcional sem confirmar espaço habitável e acesso. Outros locais podem existir: falta um cruzamento completo dos arquivos de mapa da instalação Legacy e dos interiores de missões/atualizações.

## Recomendações para a primeira seleção

1. Priorizar quatro opções geograficamente distintas para calibração: uma entrada econômica em Sandy Shores, uma em Grapeseed, uma média em Paleto Bay e um apartamento em Los Santos. Isso cobre o pedido de residências fora da cidade sem depender inicialmente de modificar casas dos protagonistas.
2. Experimentar depois um dos apartamentos de alto padrão e o protótipo da Eclipse Towers; conferir oclusão, vista externa, iluminação, objetos e compatibilidade entre IPLs do mapa SP e conteúdo de atualizações.
3. Guardar catálogo com IDs próprios estáveis e separados para **endereço**, **planta** e **variante visual**. Cada endereço precisa de entrada externa, ponto seguro de saída, orientação, eventual garagem e vínculo com a planta. A planta precisa dos requisitos de carregamento, interiores, pontos seguros de spawn/saída e cenas calibradas.
4. Validar cenas de TV, cama e banho em cada planta, por gênero/modelo e roupa; a existência de móvel ou banheiro não garante que a interação esteja pronta. Validar reinicialização, streaming, queda, morte, troca de perfil, controle de câmera e tráfego/estacionamento.
5. Nunca remover IPLs ou alterar props globais indiscriminadamente: o jogo, a história ou outros mods podem usar os mesmos recursos. A seleção deve ser limitada aos imóveis testados na versão do jogo do usuário.

**Limites:** a enumeração de 39 entradas é específica da fonte citada, não uma contagem de todas as propriedades do GTA V. Não há teste visual, de instalação ou de coordenadas no GTA V Legacy neste levantamento. As demais famílias e todos os endereços fora das listas identificadas permanecem para pesquisa ou extração direta dos arquivos locais.
