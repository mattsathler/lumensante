# Site do Lumen Santé na internet nativa do GTA V

O jogador abre a Internet pelo celular ou por um computador do GTA V que já abre o Eyefind. Na home mostrada na captura, encontra o link **Lumen Santé** no cabeçalho, entre o logotipo e o clima. O mesmo site deve aparecer na busca por “hospital” e em **Moda e Saúde**. O destino proposto é `www.lumensante.com`, dentro da internet fictícia do GTA V. A página usa a barra de endereço, o histórico e os controles do próprio jogo.

## Estado da branch

O site atual disponibiliza pacientes por categoria, prontuários, exames, anotações, histórico, tratamentos, medicamentos e agenda. A visão da sala de diagnóstico do médico aparece na home. A busca de tratamentos e exames usa o teclado do navegador. A foto do paciente exibida no computador original ainda não foi migrada para o site; imagens de `PEDHEADSHOT` exigem integração própria com texturas do Scaleform. A logo em `Assets/logo.png` é convertida em uma representação vetorial de 64 × 64 durante a geração do GFX; confira aparência e desempenho no GTA V.

O endereço apresentado no navegador do jogo é `www.lossantosmedicalcenter.com`; o endereço anterior continua aceito para compatibilidade com links e histórico. Os ícones das abas e setas são gerados a partir dos PNGs de `Assets`.

Início, Pacientes e Agenda compartilham um cabeçalho fixo durante a rolagem. O destaque da home usa o prédio recortado de `Assets/banner.png`, convertido para formas de 128 × 48 no GFX; os textos permanecem componentes nativos. A imagem convertida é mantida entre atualizações da home e recriada ao sair e retornar. A qualidade, a navegação pelo cabeçalho e o custo de desenho devem ser conferidos no jogo.

Na cadeira da sala de diagnóstico, `Usar computador` solicita a abertura do `appinternet` nativo e direciona para o site do hospital. Um paciente acessível na cadeira ou na mesa de exame da sala tem prioridade e abre o respectivo prontuário. Sem paciente na sala, o plugin restaura a última página do hospital observada durante a execução atual e, quando aplicável e ainda acessível, o último prontuário; na primeira abertura usa a home. O estado não é persistido entre reinicializações do plugin. A inicialização do aplicativo, o retorno ao menu e a navegação contextual devem ser verificados em jogo.

O atalho **Hospital** no celular é limitado ao GTA V Legacy 1.0.3889.0, cuja tabela interna foi identificada. Ele só ocupa uma linha e uma posição livres da tela inicial e não substitui outros aplicativos. O OIV atualizado inclui filmes iFruit, Badger e Facade modificados para desenhar a logo do hospital na posição do atalho; o identificador original do ícone de internet continua sendo usado como alternativa quando esses filmes não estão instalados. Se não houver espaço, o site permanece acessível pela busca do Eyefind. O tablet no F7 foi removido. A logo, a seleção, a restauração ao descarregar e a abertura do navegador precisam ser validadas no jogo.

Após relato de falha ao abrir o atalho, o plugin passou a solicitar explicitamente o carregamento do aplicativo de internet ao tocar no Hospital, assim como no computador. A alteração requer teste no celular em jogo; eventuais tempos esgotados são registrados no log com a etapa de abertura.

## Sincronização e desempenho

Enquanto uma página do hospital está aberta, o plugin compara os valores de cada slot com o último envio confirmado e transmite apenas alterações. A verificação continua a cada segundo e as ações solicitam atualização imediata; `UPDATE_TEXT` só é enviado quando houve alterações. Ao sair de uma página, ao fechar o navegador ou se uma chamada falhar, o cache é invalidado para reenviar os dados na próxima sincronização. As gerações de pacientes e agenda mudam quando o mapeamento de identificadores muda; no caso de pacientes, mudanças na revisão clínica também invalidam a geração. As ações continuam validadas no plugin.

O cálculo da agenda usa um conjunto de identificadores acessíveis, e os detalhes clínicos são preparados somente na página de pacientes. O mesmo laudo é reutilizado dentro de cada atualização para compor exames e histórico. A interface apresenta carregamento enquanto ainda não recebeu os dados, e só apresenta lista vazia ou plantão encerrado quando o plugin confirmou esse estado. Sem o plugin ativo, a página pode permanecer em carregamento porque não há resposta que permita distingui-lo de uma conexão demorada.

Essas alterações reduzem chamadas nativas e redesenhos redundantes, mas não representam uma medição de FPS. Ainda são necessários testes no GTA com listas grandes, troca de médico, abertura e fechamento do navegador, alterações simultâneas de pacientes, exames, prescrições, anotações e retornos. A conversão da logo em formas e a criação dos componentes visuais são candidatas a medição posterior; não foram substituídas sem dados de desempenho em jogo.

A branch local `feature/computador-eyefind` contém a integração com o navegador nativo do jogo.

O gerador produz dois filmes. O primeiro acrescenta o hospital ao Eyefind e contém as páginas integradas; o segundo fornece o destino solicitado pelo navegador. Os scripts ficam em [Tools/Eyefind](../Tools/Eyefind). O carregamento e as versões anteriores das páginas já foram testados no jogo; alterações posteriores ainda precisam de validação visual e funcional.

## Como o navegador carrega uma página

A instalação analisada usa GTA V Legacy `1.0.3889.0` e RAGE Plugin Hook `1.131.1424.17745`. Extraímos `web_browser.gfx` e `www_eyefind_info.gfx` do RPF da instalação local para uma pasta de build. Nessa versão, o filme da home contém o catálogo da busca, enquanto a categoria **Moda e Saúde** monta sua própria lista.

O método `GO_TO_WEBPAGE` do navegador separa o código de endereço do nome da subpágina. Para o novo destino, ele solicita um filme chamado `WWW_LUMENSANTE_COM` por `REQUEST_GFX_STREAM`. A página fornece `PAGE_NAMES` ao navegador. Ao inicializar, o navegador atualiza a própria barra de endereço e seu histórico. Um filme estático pode usar esse fluxo sem depender das tabelas de dados de banco ou veículos de `appinternet`. A resolução do novo nome pelo mecanismo de streaming permanece a hipótese central do teste. [Referência de appinternet](https://github.com/root-cause/v-decompiled-scripts/blob/master/appinternet.c) · [Mapeamento dos recursos do GTA V](https://github.com/memorx/gta-web-local/blob/main/docs/gta-v.md)

O navegador usa filmes GFx e nomes de recursos do jogo. DNS, arquivos HTML, hospedagem HTTP e WebView2 não registram destinos nessa internet. Se o jogo rejeitar o novo nome mesmo com o arquivo presente no RPF, investigar o índice de streaming e como o jogo monta o catálogo de recursos. Uma substituição temporária em um RPF de teste pode isolar o problema de carregamento, mas o aceite do projeto exige preservar os sites originais. A NUI do FiveM usa outro runtime e fica fora do alvo. [Scaleform no GTA V](https://docs.fivem.net/docs/scripting-manual/using-scaleform/) · [Fullscreen NUI](https://docs.fivem.net/docs/scripting-manual/nui-development/full-screen-nui/)

## Gerar os arquivos do protótipo

1. Da cópia local de `update.rpf` usada no modo história, extraia `x64/patch/data/cdimages/scaleform_web.rpf/www_eyefind_info.gfx`. Use o filme da versão efetivamente carregada pelo GTA V para manter as outras alterações locais da home. O JPEXS 26.3.0 foi usado para compilar ActionScript 2. [rage-cli](https://github.com/VIRUXE/rage-cli) · [JPEXS](https://github.com/jindrapetrik/jpexs-decompiler/releases/tag/version26.3.0)
2. Execute na raiz do repositório.

```powershell
./Tools/Eyefind/Build-NativeEyefind.ps1 -EyefindGfx 'caminho/www_eyefind_info.gfx' -FfdecJar 'caminho/ffdec-cli.jar'
```

O script escreve `www_eyefind_info.gfx` e `www_lumensante_com.gfx` em `lumen-sante/obj/Eyefind/native/scaleform_web/` e exporta o primeiro frame de cada saída para conferir que os scripts foram incorporados. A home gerada é derivada da instalação local. Os binários e arquivos extraídos ficam na pasta de build ignorada pelo Git.

Para gerar os filmes do celular, extraia da instalação usada os arquivos `cellphone_ifruit.gfx`, `cellphone_badger.gfx` e `cellphone_facade.gfx` de `update.rpf/x64/data/cdimages/scaleform_generic.rpf` para a mesma pasta e execute:

```powershell
./Tools/Eyefind/Build-PhoneLogo.ps1 -OriginalDirectory 'caminho/dos/filmes' -FfdecJar 'caminho/ffdec-cli.jar'
```

O gerador converte `Assets/logo.png` para formas, acrescenta a alteração ao primeiro frame de cada filme original e confirma sua presença exportando o resultado. As saídas ficam em `lumen-sante/obj/Eyefind/native/phone/`. Os filmes completos são substituídos no `scaleform_generic.rpf`: alterações de outros mods nesses mesmos filmes precisam ser preservadas na origem ou conciliadas manualmente. O gerador rejeita frames iniciais que não correspondam ao formato esperado.

## Plano de integração

| Etapa | Entrega e verificação |
| --- | --- |
| 1. Provar a navegação nativa | Em uma cópia de teste do RPF na pasta `mods`, substituir a home e adicionar `www_lumensante_com.gfx`. Com o jogo reiniciado no modo história, abrir a Internet pelo celular, clicar no link e confirmar a página com `www.lumensante.com` na barra do jogo. Repetir pela busca, por **Moda e Saúde**, por um computador nativo disponível e com Voltar, Avançar, Início e Fechar. Conferir que os sites originais continuam abrindo. |
| 2. Provar a comunicação com o plugin | Com a rota estável, enviar uma mensagem de teste para o filme e receber um clique identificável na outra direção. Os identificadores nativos de site e página podem ajudar a detectar o contexto. O acesso ao filme e o transporte dos eventos precisam de uma prova no build alvo. Fechar o navegador deve encerrar a sessão. |
| 3. Exibir dados do hospital | Projetar sala, listas, prontuários, laudos e agenda a partir de `HospitalShift`, com IDs estáveis e somente os campos já visíveis ao médico. Começar pela leitura, e atualizar laudos e status quando o plantão mudar. Fora do plantão, exibir a página pública do hospital. |
| 4. Integrar ações | Reusar os comandos do plantão para anotações, cancelamento, exames, medicamentos e tratamentos. A origem celular/computador amplia o caminho de acesso à página. As permissões continuam dependentes do perfil, da sala e do caso. O acesso pelo celular não concede a permissão de chamar paciente de uma cadeira de consulta. |
| 5. Distribuir e testar reversão | Empacotar só depois de validar instalação e remoção em uma cópia de teste. Documentar compatibilidade com modificações no mesmo RPF. Validar foco, pausa, morte, troca de médico, encerramento do plantão e recarga do plugin. |

## O que dá para reaproveitar

- **Integração direta.** As listas filtradas em [HospitalShift.cs](../lumen-sante/Gameplay/HospitalShift.cs), os IDs e estados de [ExamOrders.cs](../lumen-sante/Clinical/ExamOrders.cs) e as agendas de [MedicalAgenda.cs](../lumen-sante/Gameplay/MedicalAgenda.cs) já existem. Prontuários, laudos e agenda somente leitura são os primeiros candidatos.
- **Integração que exige adaptação.** [BedManagementPanel.cs](../lumen-sante/Interface/BedManagementPanel.cs) desenha sua própria interface e edita notas diretamente. A página precisa de componentes GFx próprios e de operações do plantão que possam ser chamadas por ID na `GameFiber`. Medicamentos, exames e tratamentos devem voltar com o resultado da validação clínica. Pacientes fora do perfil ativo não devem entrar na projeção.
- **Pontos em aberto.** Retratos `PEDHEADSHOT`, comportamento visual da logo convertida e paridade detalhada das telas precisam de validação no jogo.

Para alterações no C#, compilar com [a skill de build](../Skills/lumen-sante-build/SKILL.md) e rodar os validadores de agenda, perfis médicos e autoridade clínica de acordo com o fluxo alterado.
