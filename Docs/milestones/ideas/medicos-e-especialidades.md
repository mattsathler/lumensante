# Médicos e Especialidades

## Escopo

O **Ciclo I · Médicos** cria o gerenciamento de personagens, o Armário e a agenda por médico. O jogador poderá criar e vestir seu personagem no Lumen Santé usando os modelos freemode masculino e feminino do GTA V. A instalação e a configuração do Lumen Santé passam a ser independentes do EUP Menu.

O **Ciclo II · Especialidades** será detalhado depois. O Ciclo I cria a identidade permanente que permitirá associar especialidades e encaminhamentos a cada médico.

## Ciclo I · Médicos

Abra o menu principal do Lumen Santé com **F9**. Escolha **Médicos** para gerenciar personagens ou **Armário** para acessar as roupas do médico ativo. Defina **Personagem padrão** no editor e salve o médico para gravar a preferência.

Use **Backspace** para retornar à tela anterior com a mesma seleção e rolagem. Na tela principal, Backspace fecha o menu. **Esc** e **F9** fecham o menu de qualquer tela. Ao sair de uma edição com alterações, escolha **Salvar**, **Descartar** ou **Continuar editando**.

Confirme a linha de um médico fora do plantão para aplicá-lo. O perfil em uso aparece como **Ativo**. Confirme a linha de uma roupa para vesti-la. Os botões **Editar** e **Excluir** ficam abaixo da lista e agem sobre o médico ativo ou a roupa selecionada.

Os traços faciais podem ser ajustados pelas barras ou pela grade. Use **Girar prévia** no Armário ou nas tatuagens para conferir outros ângulos. A lista de tatuagens tem filtros por região e por itens aplicados. O editor mostra os nomes das variações quando estão disponíveis no jogo.

Os perfis ficam em `Plugins/LumenSante/doctors.json`. Cada médico começa com uma agenda nova em `Plugins/LumenSante/Agendas/<id>/agenda.json`. As agendas anteriores permanecem como arquivos inativos.

### Gerenciamento de personagens

- Usar RageNativeUI. O menu **Médicos** oferece criação, edição, seleção e exclusão, com um fluxo familiar a quem usa o EUP e apresentação própria do Lumen Santé.
- Permitir personalizar os modelos `mp_m_freemode_01` e `mp_f_freemode_01`. Mapear as opções disponíveis no GTA V Legacy, incluindo mistura de ascendência, traços faciais, cabelo, cores, olhos, sobreposições, maquiagem, tatuagens, roupas e acessórios. O editor deve consultar as opções que o jogo oferece para o modelo e fazer prévia das alterações.
- O checkbox **Personagem padrão** é exclusivo. Marcar outro médico transfere a preferência. Ao carregar o plugin, aplicar o médico padrão quando o personagem do jogo estiver disponível.
- Sem personagem padrão, o menu oferece as ações necessárias para criar, editar e definir um médico. As demais áreas, como o Armário e a agenda pessoal, ficam bloqueadas. Iniciar plantão exige um médico padrão configurado e um médico criado pelo jogador ativo.
- A seleção de outro médico fica disponível fora do plantão. Selecionar um médico para a sessão e marcá-lo como padrão são ações distintas. Trocar de roupa fica disponível dentro e fora do plantão.
- Salvar o personagem somente quando o jogador confirmar a edição. Cancelar a prévia deve restaurar a aparência anterior. A exclusão exige confirmação e, se atingir o padrão, deixa a preferência vazia até o jogador escolher outro.

### Armário

Cada médico pode ter até **20 outfits**, incluindo os dois conjuntos criados com o personagem. Preparar um scrub e uma roupa casual para o modelo feminino e um scrub e uma roupa casual para o modelo masculino. Os conjuntos iniciais são editáveis. O editor permite vestir, criar, renomear, editar e excluir outfits, com prévia de componentes, texturas e acessórios no personagem ativo.

O checkbox **Roupa de Plantão** classifica cada outfit. Vários outfits podem ser marcados. Os demais são roupas de folga. O médico pode vestir manualmente qualquer outfit próprio durante ou fora do plantão.

| Momento | Comportamento |
| --- | --- |
| Carregar o plugin | Aplicar o médico padrão e sortear uma de suas roupas de folga disponíveis. |
| Iniciar plantão | Vestir automaticamente uma de suas roupas de plantão disponíveis. |
| Encerrar plantão | Vestir a última roupa de folga que o médico usou. |
| Vestir manualmente | Aplicar a roupa escolhida ao personagem atual. Se for de folga, salvar seu ID como a última usada. |

Para o piloto, a proposta é sortear a roupa de plantão entre os outfits marcados. Se a última roupa de folga tiver sido excluída ou reclassificada, usar outra roupa de folga válida. Se nenhuma roupa da categoria necessária estiver disponível, preservar a roupa atual e manter a troca manual acessível.

### Persistência e agenda por médico

- Atribuir a cada médico um ID permanente independente do nome. Guardar a lista de médicos, o ID do padrão, a aparência de cada personagem, os outfits e o ID da última roupa de folga. Usar dados locais com versão, validação e gravação atômica.
- Vincular compromissos, retornos pendentes e prontuários persistentes ao ID do médico. A agenda do computador e do tablet segue o médico ativo. Uma alta que agenda retorno usa o médico responsável pelo atendimento.
- Ao trocar de médico, guardar a agenda anterior e carregar a do perfil selecionado. Um caso agendado que já esteja em atendimento mantém o vínculo com seu médico original. A troca de perfil não transfere casos nem libera o acesso por outro médico.
- Preservar o relógio do GTA V e a tolerância atual. Ao abrir a agenda de outro médico, reconciliar compromissos vencidos desde a última vez em que ele esteve ativo. Estar de plantão com um perfil não conta como presença para outro.
- Na transição, cada agenda por médico começa vazia e passa a receber novos compromissos. Os arquivos anteriores permanecem fora do fluxo do jogo.
- Na exclusão de um médico, a proposta é arquivar sua agenda e seus prontuários sob o ID original. Isso evita transferir ou apagar acompanhamentos por acidente.

### Integração com o GTA V e com o plantão

- Separar o armazenamento de perfis, o editor RageNativeUI e a aplicação no personagem local. Salvar os dados da aparência e dos outfits, sem registrar entidades do jogo.
- Consultar os limites de componentes, texturas e acessórios para o modelo ativo. Conferir a disponibilidade de peças na instalação antes de aplicá-las, inclusive após mudanças de conteúdo do jogo. Usar configurações iniciais disponíveis no GTA V Legacy e validar os dois modelos.
- Aplicar o médico padrão antes da tentativa de início automático existente em `HospitalShift`. Validar o perfil também no início pelo menu e pelo comando de plantão. Se o modelo falhar ao carregar, manter o personagem atual e explicar por que o plantão ainda não pode começar.
- Trocar de modelo apenas ao ativar outro médico. A roupa de plantão e a de folga devem ser aplicadas ao modelo já ativo para preservar o vínculo do plantão com o personagem. Bloquear a troca de perfil e a exclusão do médico ativo enquanto ele estiver em plantão.
- Em caso de morte, recarregamento ou troca externa de personagem, restaurar o médico apenas quando o personagem local estiver em uma condição adequada. O fluxo de interrupção do plantão deve tratar esses casos sem registrar uma saída normal.
- Atualizar os guias de instalação e as propostas que ainda citam o EUP como caminho para criar o personagem ou escolher uniformes. Validar o piloto com o EUP ausente.

### Ordem de implementação

1. Definir o formato dos perfis, a regra de padrão exclusivo e a persistência local.
2. Implementar seleção do modelo freemode e editor de aparência com prévia, confirmação e cancelamento.
3. Implementar o Armário, os quatro conjuntos iniciais e a troca automática de outfits nos eventos do plantão.
4. Vincular a agenda e os prontuários persistentes ao médico, começando com agendas novas.
5. Revisar os fluxos de carregamento, início e fim de plantão, exclusão e troca de perfil. Compilar e testar no GTA V Legacy.

### Critérios de aceitação

- Criar um médico de cada modelo, editar aparência e roupas e recarregar o plugin. As alterações e o padrão escolhido são recuperados.
- Definir outro padrão desmarca o anterior. Com a preferência vazia, o Armário e o plantão ficam bloqueados e o menu permite configurar outro padrão.
- Cada médico recebe seus dois outfits iniciais e pode chegar a 20. A roupa de plantão é aplicada automaticamente, a troca manual funciona durante o turno e o fim do turno recupera a última roupa de folga válida.
- Tentar trocar de médico durante o plantão é bloqueado. A troca de roupa não interrompe o turno.
- Dois médicos veem agendas separadas. Um retorno permanece no prontuário e na agenda do médico que o marcou, inclusive após trocar de perfil e recarregar o plugin.
- Na primeira carga, nenhum compromisso, pendência ou prontuário da agenda antiga aparece. Compromissos registrados nas novas agendas persistem após recarregar.
- Criação, personalização, guarda de roupas, agenda e plantão funcionam numa instalação sem EUP Menu.

### Validação

Após compilar em Release, execute `Skills/lumen-sante-gameplay/scripts/Validate-DoctorProfiles.ps1` e `Validate-MedicalAgenda.ps1`. No GTA V Legacy, confira os conjuntos iniciais, a prévia e a seleção de peças e tatuagens nos dois modelos. Teste o carregamento do padrão, as trocas automáticas na recepção, a troca manual em plantão e a separação da agenda no computador e no tablet.
