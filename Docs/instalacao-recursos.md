# Instalação dos recursos

`Lumen Santé - Resources.oiv` reúne o interior do hospital, os jalecos e o site no navegador do GTA V Legacy. No OpenIV, abra **Tools → Package Installer**, selecione o pacote e escolha **Install to mods folder**. Reinicie o jogo após a instalação. A DLL e a pasta de doenças do plugin são instaladas separadamente.

Para reconstruir o pacote:

1. Tenha `Dependencies/CLS-Medic-Final.oiv` e `Lumen-Sante-Doctor-Coat-v2.oiv`. As fontes do jaleco são descritas em [instalação do jaleco](instalacao-jaleco.md).
2. Gere os dois filmes do site a partir da cópia local do Eyefind, seguindo [as instruções do site](computador-eyefind.md). Repita a geração quando os scripts ou imagens do site mudarem ou o jogo for atualizado. Use a versão do filme correspondente ao jogo e às modificações locais, pois a instalação substitui a home do Eyefind.
3. Execute `./Build-ResourcesPackage.ps1`. O arquivo `Lumen Santé - Resources.oiv` existente é substituído após a montagem bem-sucedida. O gerador aceita `-HospitalPackage`, `-CoatPackage`, `-WebsiteDirectory` e `-OutputPath` para outras localizações.

O pacote altera `update.rpf` (lista de DLCs e filmes do Eyefind) e o DLC `mpimportexport` (jalecos). Faça backup da pasta `mods` antes de instalar, especialmente se outros mods alterarem esses mesmos arquivos. A instalação do OIV unificado ainda deve ser conferida no jogo.
