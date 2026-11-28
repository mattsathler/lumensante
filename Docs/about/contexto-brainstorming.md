# Lumen Santé — contexto para brainstorming

Atualizado em 24 de novembro de 2026. Este documento resume o projeto e as decisões discutidas para servir de contexto em novas conversas. O código e os testes no GTA V são a referência para detalhes de implementação; parte da documentação histórica descreve propostas anteriores.

## Visão

Lumen Santé é um mod para GTA V Legacy no PC, executado com RAGE Plugin Hook. O jogador interpreta um médico no Central Los Santos Medical Center, acompanhando pacientes desde a recepção até a consulta, internação ou alta. A intenção é criar uma rotina hospitalar imersiva, com o mundo e a equipe em atividade ao redor do jogador.

Lumen Santé é o nome do mod. O hospital e seu site usam o nome **Los Santos Medical Center** ou **Central Los Santos Medical Center**.

O projeto é desenvolvido de forma iterativa: implementar um módulo, permitir que o jogador teste no jogo e incorporar os ajustes antes de avançar. Ideias visuais podem ser mais ambiciosas, mas devem considerar os sistemas que o plugin realmente possui.

## Plataforma e distribuição

- Plugin em C#, com .NET Framework 4.8, GTA V Legacy e RAGE Plugin Hook. Menus usam RAGENativeUI.
- O hospital usa um interior customizado. O pacote de recursos OIV reúne o interior, o jaleco e os recursos do site e do celular. A DLL é compilada separadamente.
- O projeto busca usar recursos e comportamentos nativos do GTA V quando viáveis. Streaming de interiores, colisão, animações, navegação de NPCs e interfaces precisam de validação dentro do jogo.
- As doenças são modulares, com definições JSON editáveis e simulação clínica própria. As decisões médicas e os valores são mecânicas de jogo.

## Hospital e atendimento

O jogador inicia e encerra plantões no hospital. Há pacientes gerados com identidade, sintomas e evolução clínica; fila de espera, consultas em salas de diagnóstico, leitos de enfermaria e quartos. O atendimento inclui conversa, avaliação geral, sinais vitais, exames e resultados, anotações, histórico, tratamentos, medicamentos, internação e alta. A agenda contempla consultas e retornos.

Enfermeiros e médicos NPCs participam da rotina e usam rotas pelo hospital. Os médicos autônomos podem avaliar pacientes e executar decisões clínicas conforme disponibilidade e regras do atendimento. O jogador também pode assumir casos. Há lógica para óbitos e encaminhamento aos Coroners.

Foi adicionada a transferência entre quarto e enfermaria pelo menu do leito. O submenu foi pensado para receber outros destinos no futuro, como Emergência. A ordem solicitada no leito é Conversa, Exames (Sinais vitais e Avaliação geral), Transferir Paciente e Alta Hospitalar.

## Site do hospital

O hospital possui um site dentro da internet fictícia nativa do GTA V, com endereço exibido como `www.lossantosmedicalcenter.com`. O site usa Scaleform/GFx e ActionScript integrados ao navegador do jogo; o plugin fornece dados e recebe ações. Não se trata de uma página HTML comum. Foi estudada a injeção de HTML, mas a ideia foi descartada porque o navegador nativo não renderizaria HTML dessa forma.

O site oferece início, pacientes e agenda; prontuários incluem exames, anotações, histórico, tratamentos e medicamentos. Há pesquisas e agrupamento de exames. Um cabeçalho fixo permite navegar entre as páginas. A identidade visual usa verde, dourado, logo do hospital e referências à linguagem dos sites nativos do Eyefind, com layout de painel hospitalar.

O computador da sala de diagnóstico abre o navegador no prontuário do paciente da sala quando aplicável; caso contrário, restaura a última página acessada durante a sessão. O celular nativo tem um atalho Hospital quando há posição disponível, sem substituir aplicativos existentes. O tablet do F7 foi removido.

O cache de envio dos dados do site foi removido após problemas de dessincronização. As alterações mais recentes procuram evitar o piscar da tela de carregamento: a página existente é mantida durante ausências temporárias dos dados e a primeira abertura mostra um indicador localizado. Essa alteração e a navegação devem ser verificadas no jogo.

## Vida fora do hospital

A proposta é ampliar a rotina do médico para fora do plantão. Perfis de médico podem ter residências vinculadas, usando interiores nativos do GTA V com entradas configuradas. O jogador pode entrar e sair; a inicialização em casa usa uma transição e uma introdução de câmera, com uma breve atividade inicial. O usuário confirmou que as entradas e saídas dos apartamentos funcionam e ficou satisfeito com a etapa atual.

Casas de Paleto e Zancudo foram confirmadas durante a investigação; alguns outros interiores exigiram correções e validação. Há catálogo e estudo dos imóveis na pasta `Docs`. Interações com móveis como banho e televisão foram deixadas para o futuro: o interior por si só não garante que as interações do GTA Online estejam disponíveis no modo história.

## Ideias e limitações pendentes

- **Dinheiro nativo:** a integração financeira do personagem personalizado com compras do GTA V foi investigada, mas permaneceu pendente. Experimentos com o estado de protagonista tiveram efeitos indesejados e não resolveram de forma satisfatória o uso do dinheiro. Não considerar a carteira integrada como pronta.
- **Ciclo futuro:** salário fixo ao fim do plantão e veículo pessoal persistente por médico são planejados, com regras de elegibilidade e persistência ainda a definir.
- **Rotina residencial:** ampliar atividades e interações com móveis no futuro, conforme viabilidade técnica e testes no modo história.
- **Site:** melhorar estabilidade visual, sincronização e navegação; confirmar recursos e aparência no jogo. Fotos de pacientes no site exigem investigação própria de texturas.
- **NPCs:** acompanhar população, entrada no hospital, rotas e liberação de enfermeiros após escoltas. Uma alteração recente permite reservas separadas para as salas de diagnóstico II e III; precisa de confirmação em jogo.
- **Possíveis expansões:** novos setores e transferências, incluindo Emergência, devem ser planejados como evolução do hospital, sem presumir que já existam.

## Preferências para novas ideias

Ao propor funcionalidades, priorize imersão, coerência com a rotina de um médico e integração com o GTA V nativo. Explique como a ideia apareceria para o jogador, quais sistemas existentes poderia aproveitar, suas dependências e o que precisaria de prova no jogo. Distinga claramente o que já está implementado do que é proposta; evite tratar imagens de referência como uma lista de funcionalidades existentes.

Arquivos úteis no repositório incluem `Docs/milestones/ideas/computador-eyefind.md`, `Docs/milestones/ideas/vida-fora-do-hospital.md`, `Docs/catalogo-residencias-nativas.md`, `Docs/milestones/achieved/workers.md`, `Docs/milestones/achieved/agenda-medica.md` e `Docs/milestones/achieved/doencas-modulares.md`.
