#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$HospitalPackage = (Join-Path $PSScriptRoot 'Dependencies\CLS-Medic-Final.oiv'),
    [string]$CoatPackage = (Join-Path $PSScriptRoot 'Lumen-Sante-Doctor-Coat-v2.oiv'),
    [string]$WebsiteDirectory = (Join-Path $PSScriptRoot 'lumen-sante\obj\Eyefind\native\scaleform_web'),
    [string]$OutputPath = (Join-Path $PSScriptRoot 'Lumen Santé - Resources.oiv')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
if ([IO.Path]::GetExtension($output) -ine '.oiv') {
    throw 'O arquivo de saída deve ter extensão .oiv.'
}
$websiteFiles = @('www_eyefind_info.gfx', 'www_lumensante_com.gfx')
foreach ($name in $websiteFiles) {
    if (!(Test-Path -LiteralPath (Join-Path $WebsiteDirectory $name) -PathType Leaf)) {
        throw "Arquivo do site ausente: $name. Gere os filmes com Tools/Eyefind/Build-NativeEyefind.ps1 antes de montar o pacote."
    }
}

$packages = @()
$staged = Join-Path ([IO.Path]::GetDirectoryName($output)) ([Guid]::NewGuid().ToString('N') + '.tmp')
try {
    foreach ($path in @($HospitalPackage, $CoatPackage)) {
        $inputPath = (Resolve-Path -LiteralPath $path).Path
        if ($inputPath -ieq $output) { throw 'O arquivo de saída não pode ser um dos pacotes de entrada.' }
        $packages += [IO.Compression.ZipFile]::OpenRead($inputPath)
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
        'Central Los Santos Medical Center, jalecos MP Male/MP Female e site do hospital no Eyefind. GTA V Legacy. Instale na pasta mods; o plugin é instalado separadamente.')) | Out-Null
    foreach ($node in $manifests[1].package.content.ChildNodes) {
        $manifest.package.content.AppendChild($manifest.ImportNode($node, $true)) | Out-Null
    }
    $updateArchive = $manifest.SelectSingleNode('/package/content/archive[translate(@path, "/", "\")="update\update.rpf"]')
    if (!$updateArchive) { throw 'O pacote do hospital não contém update\update.rpf.' }
    $webArchive = $manifest.CreateElement('archive')
    $webArchive.SetAttribute('path', 'x64\patch\data\cdimages\scaleform_web.rpf')
    $webArchive.SetAttribute('createIfNotExist', 'False')
    $webArchive.SetAttribute('type', 'RPF7')
    foreach ($name in $websiteFiles) {
        $add = $manifest.CreateElement('add')
        $add.SetAttribute('source', 'website/' + $name)
        $add.InnerText = $name
        [void]$webArchive.AppendChild($add)
    }
    [void]$updateArchive.AppendChild($webArchive)

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
        foreach ($name in $websiteFiles) {
            $entryName = 'content/website/' + $name
            if ($entryNames.ContainsKey($entryName)) { throw "Entrada duplicada: $entryName" }
            $entryNames[$entryName] = $true
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $WebsiteDirectory $name), $entryName) | Out-Null
        }
        foreach ($add in $manifest.SelectNodes('//add[@source]')) {
            if (!$entryNames.ContainsKey('content/' + $add.source.Replace('\', '/'))) {
                throw "Arquivo ausente: $($add.source)"
            }
        }
        $writer = [IO.StreamWriter]::new($zip.CreateEntry('assembly.xml').Open(), [Text.UTF8Encoding]::new($false))
        try { $writer.Write($manifest.OuterXml) } finally { $writer.Dispose() }
    } finally { $zip.Dispose() }
    if (Test-Path -LiteralPath $output) {
        $backup = $staged + '.bak'
        [IO.File]::Replace($staged, $output, $backup)
        Remove-Item -LiteralPath $backup
    } else {
        [IO.File]::Move($staged, $output)
    }
} finally {
    foreach ($package in $packages) { $package.Dispose() }
    if (Test-Path -LiteralPath $staged) { Remove-Item -LiteralPath $staged }
}
Write-Host "Pacote gerado em $output."


