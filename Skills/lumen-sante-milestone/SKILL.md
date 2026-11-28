---
name: lumen-sante-milestone
description: Planeja e implementa uma milestone do Lumen Santé em branch dedicada, acompanhando todos os ciclos e o build Debug. Use quando solicitado a desenvolver uma milestone; para encerrá-la e gerar Release, use lumen-sante-fechar-milestone.
---

# Desenvolver uma milestone

Trabalhe a partir do Markdown da milestone em `Docs/milestones/ideas` indicado pelo usuário. Leia as skills locais pertinentes ao escopo e a de build. Preserve o trabalho existente: inspecione estado do Git e branch antes de começar, crie uma branch dedicada a partir da base apropriada sem descartar alterações; se mudanças existentes impedirem isolamento seguro, explique o impedimento. Não faça merge, push ou publicação sem autorização.

## Planejamento e transferência

O usuário deseja **GPT-6.1 Terra, esforço high** para planejamento e **Luna, esforço normal** para implementação. Uma skill não troca o modelo da sessão por si só. Se houver delegação e modelos disponíveis, use o planejador solicitado para produzir o plano e o implementador Luna disponível para executá-lo; informe o modelo e esforço efetivamente usados. Não suponha que uma variante disponível seja exatamente a solicitada. Se o ambiente não permitir a troca ou o modelo não estiver disponível, produza o que for possível, informe a limitação e forneça instruções claras para continuar com o modelo escolhido pelo usuário, sem alegar que houve troca. Não force a troca de modelo nem encerre trabalho já autorizado que possa ser feito com segurança por isso.

Investigue código, contratos, documentação e testes relevantes antes de planejar. Crie em `Docs/milestones/ideas` um arquivo `<nome-da-milestone>-implementacao.md`, vinculado à proposta, que sirva de transferência independente de contexto. Registre versão/base e branch, objetivo e escopo, estado real versus proposta, decisões e pressupostos, dependências, arquivos e integrações previstos, contrato de dados e migração quando aplicável, riscos e recuperação, ciclos em ordem com tarefas concretas, critérios de aceitação e validação automatizada e no GTA V. Identifique decisões realmente bloqueantes e trabalho que possa avançar independentemente. Não apresente hipóteses como fatos. Atualize o plano se a implementação exigir mudanças e registre a razão.

## Implementação e acompanhamento

Implemente todos os ciclos propostos, em ordem de dependência, usando o plano como guia e as skills locais relevantes. Preserve compatibilidade e padrões do projeto, inclusive inclusões explícitas de C# no `.csproj`. Revise os fluxos completos e tratamento de falhas afetados. O plano não substitui verificar o código real; corrija-o quando necessário.

Mantenha no documento de implementação o estado de cada ciclo, evidências de testes, decisões, pendências e próximos passos, distinguindo implementado, validado e bloqueado. Não marque como concluído algo que só foi planejado. Resolva falhas e continue até completar o escopo autorizado; se houver impedimento externo, registre o que foi feito e exatamente o que falta.

Gere a DLL **Debug** com `Skills/lumen-sante-build/scripts/Build-Plugin.ps1` e execute os validadores pertinentes, conforme a skill de build e as skills de domínio. Registre comando, resultado e caminho do artefato. Se o ambiente não permitir o build ou um teste, relate a causa e deixe a verificação pendente; não afirme sucesso. Testes visuais, navegação e integração com NPCs exigem GTA V quando aplicáveis.

Ao terminar, informe ciclos, branch, plano, build e resultados, incluindo qualquer verificação pendente. Mantenha a proposta em `ideas`: o encerramento, movimentação para `achieved` e build Release pertencem à skill `lumen-sante-fechar-milestone`, quando solicitados.
