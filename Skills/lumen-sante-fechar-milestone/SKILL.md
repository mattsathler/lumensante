---
name: lumen-sante-fechar-milestone
description: Encerra uma milestone implementada do Lumen Santé, verifica seus ciclos, atualiza a documentação, move-a para achieved e gera build Release. Use quando solicitado a fechar ou concluir formalmente uma milestone.
---

# Fechar uma milestone

Localize a proposta em `Docs/milestones/ideas` e seu plano de implementação, se existir. Inspecione a branch, alterações, código e evidências de cada ciclo e critério de aceitação; leia as skills de domínio e `Skills/lumen-sante-build/SKILL.md` conforme o escopo. Não confunda planejamento, implementação, testes automatizados e validação no GTA V. Preserve alterações existentes e não faça merge, push ou publicação sem autorização.

Conclua correções e documentação necessárias dentro do escopo autorizado. Execute verificações pertinentes e gere a DLL com `Skills/lumen-sante-build/scripts/Build-Plugin.ps1 -Configuration Release`; execute os validadores adequados contra a DLL Release. Registre configuração, comandos, resultado, caminho da DLL e conteúdo de distribuição exigido pela skill de build. Se falhar ou faltar ambiente, registre o motivo e não apresente o build como gerado.

Somente classifique a milestone como concluída e mova a proposta para `Docs/milestones/achieved` quando todos os ciclos e critérios obrigatórios estiverem efetivamente concluídos e as validações exigidas tiverem passado. Se testes dependentes do GTA V ou outros requisitos estiverem pendentes, mantenha-a em `ideas`, documente o bloqueio e entregue tudo que puder ser concluído; não invente evidência. Se o usuário modificar explicitamente o escopo ou aceitar uma pendência, registre essa decisão e seus limites antes de classificar.

Ao fechar, mova também o plano de implementação associado, mantendo o vínculo com a proposta; atualize links relativos, índice de `Docs`, referências nas skills e documentos afetados. Atualize a proposta com estado final, comportamento entregue, decisões ou desvios, testes e limitações reais; atualize guias de instalação, uso ou contratos quando impactados. Verifique links locais e alterações para evitar referências quebradas. Informe branch, arquivos, resultados e caminho da DLL Release, distinguindo verificações realizadas das pendentes.
