#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$HospitalPackage = (Join-Path $PSScriptRoot 'Dependencies\CLS-Medic-Final.oiv'),
    [string]$CoatPackage = (Join-Path $PSScriptRoot 'Lumen-Sante-Doctor-Coat-v2.oiv'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'Lumen Santé - Resources.oiv')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ([IO.Path]::GetExtension($output) -ine '.oiv') {
    throw 'O arquivo de saída deve ter extensão .oiv.'
}

$packages = @()
$staged = Join-Path ([IO.Path]::GetDirectoryName($output)) ([Guid]::NewGuid().ToString('N') + '.tmp')
try {
    foreach ($path in @($HospitalPackage, $CoatPackage)) {
        $packages += [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $path).Path)
    }
    $manifests = foreach ($package in $packages) {
        $reader = [IO.StreamReader]::new($package.GetEntry('assembly.xml').Open())
        try { [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    if ($manifests[1].SelectSingleNode('/package/content/add') -or
        $manifests[1].SelectSingleNode('/package/content/archive').path -ne 'update\x64\dlcpacks\mpimportexport\dlc.rpf') {
        throw 'Selecione o pacote v2 do jaleco, que substitui as peças Import/Export.'
    }

    $manifest = $manifests[0]
    $manifest.package.id = '{A12C4C4B-33E8-4C53-BBEC-A135276D9B89}'
    $manifest.package.metadata.name = 'Lumen Santé - Resources'
    $manifest.package.metadata.version.RemoveChild($manifest.package.metadata.version.SelectSingleNode('tag')) | Out-Null
    $description = $manifest.SelectSingleNode('/package/metadata/description')
    $description.RemoveAll()
    $description.AppendChild($manifest.CreateCDataSection(
        'Central Los Santos Medical Center e jalecos MP Male/MP Female. Os jalecos substituem o casaco Import/Export jbib_006 e a textura A. GTA V Legacy. Instale na pasta mods.')) | Out-Null
    foreach ($node in $manifests[1].package.content.ChildNodes) {
        $manifest.package.content.AppendChild($manifest.ImportNode($node, $true)) | Out-Null
    }

    $zip = [IO.Compression.ZipFile]::Open($staged, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $entryNames = @{}
        foreach ($package in $packages) {
            foreach ($entry in $package.Entries) {
                if ($entry.FullName -eq 'assembly.xml' -or !$entry.Name) { continue }
                if ($entryNames.ContainsKey($entry.FullName)) { throw "Entrada duplicada: $($entry.FullName)" }
                $entryNames[$entry.FullName] = $true
                $input = $entry.Open()
                $destination = $zip.CreateEntry($entry.FullName).Open()
                try { $input.CopyTo($destination) } finally { $input.Dispose(); $destination.Dispose() }
            }
        }
        foreach ($add in $manifest.SelectNodes('//add[@source]')) {
            if (!$entryNames.ContainsKey('content/' + $add.source.Replace('\', '/'))) {
                throw "Arquivo ausente: $($add.source)"
            }
        }
        $writer = [IO.StreamWriter]::new($zip.CreateEntry('assembly.xml').Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.Write($manifest.OuterXml) } finally { $writer.Dispose() }
    } finally { $zip.Dispose() }
    [IO.File]::Move($staged, $output)
} finally {
    foreach ($package in $packages) { $package.Dispose() }
    if (Test-Path -LiteralPath $staged) { Remove-Item -LiteralPath $staged }
}
Write-Host "Pacote gerado em $output."


