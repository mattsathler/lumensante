# Jaleco médico no EUP

O pacote atual é Lumen-Sante-Doctor-Coat-v2.oiv. Ele substitui o casaco Import/Export jbib_006 e a textura A para MP Male e MP Female, seguindo a indicação do autor.

Abra Tools → Package Installer no OpenIV, selecione o pacote e escolha Install to mods folder. No EUP, procure o casaco Import/Export em Top e selecione a primeira textura. O índice global depende da versão do jogo e dos DLCs.

A versão anterior, que adicionava o DLC lumen_doctor_coat, causou crash durante o carregamento. A entrada desse DLC foi desativada no jogo. A versão 2 usa os slots existentes do Import/Export.

A versão 2 teve o manifesto e os quatro arquivos conferidos. A instalação e o teste dentro do jogo ainda estão pendentes.

Para reconstruir, use o SDK .NET 8 e execute dotnet run --project Tools/DoctorCoat -- Dependencies/Doctor_Coat Lumen-Sante-Doctor-Coat-v2.oiv na raiz do repositório. Escolha um nome de saída novo se o pacote já existir.

Para reverter a substituição, restaure o backup do DLC mpimportexport da pasta mods ou a cópia original desse DLC.
