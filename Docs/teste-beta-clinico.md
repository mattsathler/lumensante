# Teste beta do sistema clínico

## Instalação

1. Faça backup de `Plugins/LumenSante/Diseases` e `Plugins/LumenSante/Agendas` da instalação de teste. Os prontuários antigos podem apontar para doenças removidas do catálogo.
2. Substitua `Plugins/lumen-sante.dll` pela DLL deste pacote e use a pasta `Plugins/LumenSante/Diseases` fornecida, com os dez JSONs. Remova da pasta ativa as definições antigas caso queira testar exatamente o catálogo padrão.
3. No OpenIV, instale `Lumen-Sante-Resources-Beta.oiv` na pasta `mods`. O pacote contém o interior, os jalecos e os filmes atualizados do Eyefind e do celular. Ele substitui arquivos que outros mods também podem alterar; use uma cópia de teste e preserve seu backup.
4. Reinicie o GTA V Legacy e o RAGE Plugin Hook. A instalação e a configuração das dependências de jogo continuam necessárias.

## Roteiro sugerido

- Inicie um plantão, chame um paciente, converse e consulte **Guia clínico** no menu do paciente.
- Solicite sinais vitais, hemograma, PCR, função renal, glicemia casual, urocultura, porfobilinogênio urinário, gasometria, ECG, MRI de tórax/crânio/coluna ou RX de tórax. Confira se o paciente vai à recepção quando há vaga, se a equipe o atende e se o retorno fica disponível ao término.
- Registre uma medição, aguarde evolução ou administre suporte, repita o exame e confira os dois resultados no prontuário. Compare exames sobrepostos, como creatinina isolada e painel renal, para verificar se o valor mais recente prevalece. Feche e reabra o plugin com retorno agendado para verificar persistência.
- Compare sintomas antes e depois de analgésico, antiemético, antialérgico, antitérmico ou oxigenoterapia. Verifique também o retorno do sintoma quando o suporte termina.
- Confira os dez casos, a distribuição, os dois casos raros e os tratamentos específicos. No histórico, examine os resultados numéricos e as hipóteses que as regras configuradas descartam.
- Teste recepção cheia, interrupção de exame, perda de NPC, transferência para leito, alta e retorno agendado.

## Limitações conhecidas

- Os exames relacionados aos dez casos padrão usam medições numéricas, incluindo sinais vitais, hemograma, sódio, função renal, glicemia casual, PCR, células urinárias, urocultura, porfobilinogênio, painel hepático, INR, gasometria, ECG e achados de MRI/RX utilizados pelo catálogo. Laudos textuais de casos externos antigos continuam aceitos para compatibilidade; resultados antigos já salvos também não são convertidos retroativamente. Exames sem parâmetros numéricos configurados continuam sem base quantitativa.
- As faixas e regras diagnósticas são parâmetros de jogo provisórios, sujeitos a revisão clínica e balanceamento. Uma hipótese não descartada não é um diagnóstico confirmado.
- O GTA V é necessário para validar caminhos, animações, interação, apresentação visual e carregamento dos filmes GFX. Os validadores automatizados não cobrem esses comportamentos.
