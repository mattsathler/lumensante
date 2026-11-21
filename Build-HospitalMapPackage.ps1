#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$DlcPath = (Join-Path $PSScriptRoot 'Dependencies\gn_dual_mediccenter'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'Central-Los-Santos-Medical-Center-SP.oiv'),
    [switch]$UpdateOnly,
    [switch]$PillboxOnly
)

$ErrorActionPreference = 'Stop'
$source = Get-Item -LiteralPath $DlcPath
$dlc = Join-Path $source.FullName 'dlc.rpf'
if (!$source.PSIsContainer -or !(Test-Path -LiteralPath $dlc -PathType Leaf)) {
    throw 'Selecione a pasta do mapa que contém dlc.rpf.'
}

$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ([IO.Path]::GetExtension($output) -ine '.oiv') {
    throw 'O arquivo de saída deve ter extensão .oiv.'
}

$manifest = @'
<?xml version="1.0" encoding="UTF-8"?>
<package version="2.2" id="{84C5103B-3DFE-4B7C-AF7E-89856AEA35C5}" target="Five">
  <metadata>
    <name>Central Los Santos Medical Center SP</name>
    <version><major>1</major><minor>0</minor><tag>Teste</tag></version>
    <author><displayName>G&amp;N Studio (mapa) / Lumen Santé (pacote)</displayName></author>
    <description footerLink="" footerLinkTitle=""><![CDATA[Mapa para teste no GTA V Legacy. Selecione a instalação na pasta mods.]]></description>
  </metadata>
  <colors>
    <headerBackground useBlackTextColor="False">$FF23366A</headerBackground>
    <iconBackground>$FF3B5998</iconBackground>
  </colors>
  <content>
    <archive path="update\update.rpf" createIfNotExist="False" type="RPF7">
      <xml path="common\data\dlclist.xml">
        <add append="Last" xpath="/SMandatoryPacksData/Paths[not(Item[normalize-space(.)='dlcpacks:/gn_dual_mediccenter/' or normalize-space(.)='dlcpacks:\gn_dual_mediccenter\'])]">
          <Item>dlcpacks:/gn_dual_mediccenter/</Item>
        </add>
      </xml>
    </archive>
    <add source="dlc.rpf">update\x64\dlcpacks\gn_dual_mediccenter\dlc.rpf</add>
  </content>
</package>
'@

if ($PillboxOnly) {
    $manifest = $manifest.Replace('Central Los Santos Medical Center SP', 'Pillbox Hill Medical Center SP (G&N)'.Replace('&', '&amp;'))
}

if ($UpdateOnly) {
    $manifest = [regex]::Replace($manifest, '(?ms)^    <archive .*?^    </archive>\r?\n', '')
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$file = [IO.File]::Open($output, [IO.FileMode]::CreateNew)
try {
    $zip = [IO.Compression.ZipArchive]::new($file, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        $entry = $zip.CreateEntry('assembly.xml')
        $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
        try {
            $writer.Write($manifest)
        } finally {
            $writer.Dispose()
        }

        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $dlc, 'content/dlc.rpf') | Out-Null
    } finally {
        $zip.Dispose()
    }
} catch {
    $file.Dispose()
    Remove-Item -LiteralPath $output
    throw
} finally {
    $file.Dispose()
}

Write-Host "Pacote gerado em $output. No OpenIV, escolha a pasta mods como destino."
