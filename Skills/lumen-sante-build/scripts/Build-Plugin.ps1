#Requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$MSBuildPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if (!$MSBuildPath) {
    $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        $MSBuildPath = $command.Source
    } else {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $MSBuildPath = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        }
    }
}
if (!$MSBuildPath -or !(Test-Path -LiteralPath $MSBuildPath)) {
    throw 'MSBuild ausente. Informe -MSBuildPath ou instale as ferramentas de compilação do Visual Studio.'
}

$project = Join-Path $repositoryRoot 'lumen-sante/lumen-sante.csproj'
& $MSBuildPath $project /nologo /verbosity:minimal "/p:Configuration=$Configuration"
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild falhou com código $LASTEXITCODE."
}
