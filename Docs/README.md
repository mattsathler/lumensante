# Lumen Santé

Lumen Santé é um projeto de plugin para o ecossistema LSPDFR no GTA V Legacy, executado via RAGE Plugin Hook. O jogador assume um plantão no Central Los Santos Medical Center e acompanha pacientes fictícios da chegada ao desfecho.

Esta pasta reúne a proposta inicial. As regras de jogo e escolhas técnicas marcadas como propostas servem de ponto de partida para o protótipo.

| Documento | Conteúdo |
|---|---|
| [Visão e escopo](visao.md) | Experiência, papel do jogador e primeira versão |
| [Plantão e casos](jogabilidade.md) | Fluxo, estados e casos originais |
| [Arquitetura e integração](arquitetura.md) | Componentes, ciclo de vida e validação do ambiente |
| [Roteiro e aceitação](roteiro.md) | Ordem de implementação, testes e decisões pendentes |
| [Instalação do mapa](instalacao-mapa.md) | Preparação do interior customizado para os testes |

## Compilação local

Use o Visual Studio ou MSBuild com o pacote de desenvolvimento do .NET Framework 4.8. Coloque o SDK do RAGE Plugin Hook (`RagePluginHookSDK.dll`) e o `RAGENativeUI.dll` em `Dependencies` e compile `lumen-sante.slnx`. O mapa do hospital é instalado separadamente.
