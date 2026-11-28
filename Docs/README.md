# Lumen Santé

Lumen Santé é um projeto de plugin para o ecossistema LSPDFR no GTA V Legacy, executado via RAGE Plugin Hook. O jogador assume um plantão no Central Los Santos Medical Center e acompanha pacientes fictícios da chegada ao desfecho.

Esta pasta reúne referências e planos do projeto. Marcos implementados ficam em `milestones/achieved`; propostas e planos com trabalho pendente ficam em `milestones/ideas`. A classificação não substitui a validação no GTA V, indicada em cada documento.

| Documento | Conteúdo |
|---|---|
| [Natural NPCs](milestones/ideas/natural-npcs.md) | Proposta de aparência, comportamento e rotina dos NPCs |
| [Visão e escopo](visao.md) | Experiência, papel do jogador e primeira versão |
| [Plantão e casos](jogabilidade.md) | Fluxo, estados e casos originais |
| [Arquitetura e integração](arquitetura.md) | Componentes, ciclo de vida e validação do ambiente |
| [Roteiro e aceitação](milestones/ideas/roteiro.md) | Ordem de implementação, testes e decisões pendentes |
| [Doenças modulares](milestones/achieved/doencas-modulares.md) | Plano para migrar o catálogo para JSON e carregar doenças externas antes do Workers |
| [Formato de doença JSON](formato-doenca.md) | Contrato da versão 1, instalação de doenças externas e exemplo completo |
| [Catálogo clínico](catalogo-clinico.md) | Dez casos padrão, pesos e situação do modelo numérico |
| [Workers](milestones/achieved/workers.md) | Plano de atendimento autônomo dos Doctors, diagnóstico, cobertura de áreas e prioridade do jogador |
| [Agenda Médica](milestones/achieved/agenda-medica.md) | Consultas próprias, retornos e persistência dos prontuários da agenda |
| [Médicos e Especialidades](milestones/ideas/medicos-e-especialidades.md) | Ciclo I com criação de médicos, armário e agendas por perfil |
| [Instalação dos recursos](instalacao/instalacao-recursos.md) | Pacote unificado de mapa, jalecos e site |
| [Instalação do mapa](instalacao/instalacao-mapa.md) | Procedimento separado para o interior |
| [Jaleco opcional](instalacao/instalacao-jaleco.md) | Instalação e reconstrução do jaleco |
| [Salário e carteira](milestones/ideas/salario-e-carteira.md) | Proposta de pagamento e persistência por médico |
| [Vida fora do hospital](milestones/ideas/vida-fora-do-hospital.md) | Estudo da residência, rotina, integração financeira e veículo pessoal |
| [Catálogo de residências nativas](catalogo-residencias-nativas.md) | Interiores e endereços candidatos no modo história e em atualizações |
| [Lotes de vida fora do hospital](milestones/ideas/lotes-vida-fora-hospital.md) | Sequência de implementação e validação das residências e dos ciclos |

## Compilação local

Use o Visual Studio ou MSBuild com o pacote de desenvolvimento do .NET Framework 4.8. Coloque o SDK do RAGE Plugin Hook (`RagePluginHookSDK.dll`) e o `RAGENativeUI.dll` em `Dependencies` e compile `lumen-sante.slnx`. O mapa do hospital é instalado separadamente.
