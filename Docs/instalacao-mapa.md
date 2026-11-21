# Instalação do Central Los Santos Medical Center

1. Coloque o `dlc.rpf` preenchido em `Dependencies\gn_dual_mediccenter`.
2. Execute `.\Build-HospitalMapPackage.ps1` na raiz do projeto para gerar `Central-Los-Santos-Medical-Center-SP.oiv`.
3. No OpenIV, abra o pacote em **Tools → Package Installer** e escolha **Install to mods folder**. O pacote instala o DLC e registra `dlcpacks:/gn_dual_mediccenter/`.
4. Se o `phmc` anterior estiver ativo, desative sua entrada no `dlclist.xml` para testar.
5. No modo história, entre a pé no Central Los Santos Medical Center, confira a mobília e registre o ponto do plantão com **F6**.

Após a instalação do mapa, as atualizações do Lumen Santé trocam apenas `lumen-sante.dll` em `Plugins`.
