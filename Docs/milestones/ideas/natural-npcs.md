# Natural NPCs

**Status:** ideia em estudo; nenhum ciclo deste documento está concluído. Objetivo: tornar o hospital mais imersivo com pessoas visualmente coerentes, comportamento variado e continuidade entre visitas, preservando o funcionamento clínico.

## Base existente

Funcionários possuem nomes e identidades durante a sessão, rotas e tarefas clínicas; pacientes usam um catálogo de modelos e podem ter aparência preservada. A identidade permanente da equipe, a escala semanal e a relação entre idade e aparência ainda precisam ser construídas. O relógio de tempo decorrido não é, por si só, uma escala semanal. Detalhes dependem da implementação e de testes no GTA V Legacy.

## Ciclo I — Natural Appearance

- Gerar na primeira inicialização uma equipe persistente de médicos, cirurgiões e enfermeiros, usando os modelos multiplayer masculino e feminino. Guardar ID estável, nome e sobrenome sorteados das listas existentes, cargo, sexo, aparência física e escala.
- Gerar rostos e características variados e reproduzíveis. Separar os dados pessoais dos uniformes para que uma troca de roupa não altere a identidade. Verificar quais parâmetros dos modelos multiplayer precisam ser salvos além dos componentes e acessórios já capturados para pacientes.
- Disponibilizar uniformes padrão por cargo e sexo, a definir posteriormente, e permitir configurá-los no menu, com visualização e restauração dos padrões. Validar combinações de componentes e texturas no jogo.
- Gerar uma escala semanal persistente e aleatória como conjunto, garantindo cobertura mínima por cargo em todos os horários, inclusive na virada do dia. A mesma segunda-feira às 9h deve apresentar os mesmos funcionários entre sessões. A escala é interna, sem obrigação de exibição na interface.
- Definir quantidade por cargo, duração de plantões, reserva, trocas graduais e compatibilidade com o limite de população. Não retirar um profissional de uma tarefa em andamento sem conclusão ou transferência segura. Definir a função clínica e a cobertura necessária dos cirurgiões.
- Usar data e hora do jogo para a escala e tratar saltos de tempo, reinicialização e falha de criação. Preservar IDs de autoria clínica e não duplicar funcionários. Versionar e validar os dados persistidos; preservar arquivos inválidos e oferecer recuperação segura.

## Ciclo II — Natural Behavior

- Testar navegação e desvio de pedestres nativos nos corredores, mantendo rotas e recuperação para NPCs presos quando necessário. Evitar que comportamentos opcionais interrompam tarefas clínicas ou bloqueiem passagens.
- Adicionar olhares breves e contextuais durante a caminhada e espera, com intervalos naturais. Experimentar telefone, pequenas pausas e animações compatíveis com a tarefa e o ambiente.
- Variar velocidade e marcha conforme condição clínica. Usar intensidade efetiva de dor para marcha de dor, incluindo melhora após tratamento; avaliar como transportar pacientes que não possam caminhar. Gravidade e dor são fatores distintos.
- Definir prioridade e cancelamento: emergências e cuidados prevalecem sobre deslocamentos, que prevalecem sobre ações opcionais. Ações devem liberar objetos e recursos ao serem interrompidas.
- Gerar perfil e aparência de pacientes de modo coerente: nome, sexo, faixa etária e modelo visual compatíveis, preservados em retornos. Classificar modelos existentes e evitar associações estereotipadas.
- Estudar metadados opcionais de idade e sexo nas doenças, usando pesos para probabilidades e restrições apenas quando clinicamente necessárias. Manter compatibilidade com JSONs de doenças existentes e externos.

## Ciclo III — Natural Hospital

Proposta para aprofundar a vida hospitalar após validar os ciclos anteriores:

- Interações curtas e interrompíveis entre colegas, como cumprimentos e conversas em locais apropriados.
- Rotinas por função, pausas e circulação contextual, sem prejudicar cobertura ou atendimentos.
- Passagem de plantão com sobreposição e saídas graduais, preservando reservas e tarefas.
- Reações discretas a emergências e passagem de macas, sem impedir o jogador ou outros NPCs.
- Continuidade de funcionários e pacientes em visitas e retornos, respeitando os registros existentes.

## Validação e decisões

Começar com protótipos de aparência multiplayer e de escala; testar navegação, animações e colisões em trechos restritos antes de ampliar a população. Validar no GTA V rostos e uniformes após reinicialização, cobertura ao longo de toda a semana, trocas durante atendimentos, mudanças de relógio, compatibilidade de doenças externas e retomada após interrupções. Medir desempenho com a população configurada e com o jogador distante.

Permanecem a definir uniformes, número de profissionais e cobertura por cargo, duração dos plantões, alcance do editor, regras clínicas de mobilidade e comportamentos nativos viáveis no interior do hospital.
