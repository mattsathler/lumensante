# Lumen Santé

Lumen Santé é um projeto de plugin para o ecossistema LSPDFR no GTA V Legacy, executado via RAGE Plugin Hook. O jogador assume um plantão no Central Los Santos Medical Center e acompanha pacientes fictícios da chegada ao desfecho.

Esta pasta reúne a proposta inicial. As regras de jogo e escolhas técnicas marcadas como propostas servem de ponto de partida para o protótipo.

| Documento | Conteúdo |
|---|---|
| [Visão e escopo](visao.md) | Experiência, papel do jogador e primeira versão |
| [Plantão e casos](jogabilidade.md) | Fluxo, estados e casos originais |
| [Arquitetura e integração](arquitetura.md) | Componentes, ciclo de vida e validação do ambiente |
| [Roteiro e aceitação](roteiro.md) | Ordem de implementação, testes e decisões pendentes |
| [Doenças modulares](doencas-modulares.md) | Plano para migrar o catálogo para JSON e carregar doenças externas antes do Workers |
| [Formato de doença JSON](formato-doenca.md) | Contrato da versão 1, instalação de doenças externas e exemplo completo |
| [Catálogo clínico](catalogo-clinico.md) | Dez casos padrão, pesos e situação do modelo numérico |
| [Doenças de internação](doencas-internacao.md) | Histórico do catálogo anterior de 50 casos |
| [Workers](workers.md) | Plano de atendimento autônomo dos Doctors, diagnóstico, cobertura de áreas e prioridade do jogador |
| [Agenda Médica](agenda-medica.md) | Consultas próprias, retornos e persistência dos prontuários da agenda |
| [Médicos e Especialidades](medicos-e-especialidades.md) | Ciclo I com criação de médicos, armário e agendas por perfil |
| [Instalação do mapa](instalacao-mapa.md) | Preparação do interior customizado para os testes |
| [Salário e carteira](salario-e-carteira.md) | Proposta de pagamento e persistência por médico |
| [Vida fora do hospital](vida-fora-do-hospital.md) | Estudo da residência, rotina, integração financeira e veículo pessoal |
| [Catálogo de residências nativas](catalogo-residencias-nativas.md) | Interiores e endereços candidatos no modo história e em atualizações |
| [Lotes de vida fora do hospital](lotes-vida-fora-hospital.md) | Sequência de implementação e validação das residências e dos ciclos |

## Compilação local

Use o Visual Studio ou MSBuild com o pacote de desenvolvimento do .NET Framework 4.8. Coloque o SDK do RAGE Plugin Hook (`RagePluginHookSDK.dll`) e o `RAGENativeUI.dll` em `Dependencies` e compile `lumen-sante.slnx`. O mapa do hospital é instalado separadamente.
