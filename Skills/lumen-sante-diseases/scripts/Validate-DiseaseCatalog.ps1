#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\..\..\lumen-sante\bin\Debug\lumen-sante.dll')
)

$ErrorActionPreference = 'Stop'
$sdkPath = Join-Path $PSScriptRoot '..\..\..\Dependencies\RagePluginHookSDK.dll'
if (Test-Path -LiteralPath $sdkPath) {
    [void][Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $sdkPath).Path)
}

$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$catalog = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog', $true)
$cases = $catalog.GetField('Cases', [Reflection.BindingFlags]'Static,NonPublic').GetValue($null)
if (!$cases -or $cases.Length -eq 0) {
    throw 'O catálogo precisa de pelo menos uma doença.'
}

$ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$members = [Reflection.BindingFlags]'Instance,NonPublic'
$weightTotal = 0
foreach ($case in $cases) {
    $definition = $case.GetType()
    $id = $definition.GetProperty('Id', $members).GetValue($case)
    if ([string]::IsNullOrWhiteSpace($id) -or !$ids.Add($id)) {
        throw "ID vazio ou duplicado: $id"
    }

    $weight = $definition.GetProperty('SelectionWeight', $members).GetValue($case)
    $weightTotal += $weight
    $stages = $definition.GetProperty('Stages', $members).GetValue($case)
    $prompts = $definition.GetProperty('Prompts', $members).GetValue($case)
    if ($weight -le 0 -or $stages.Count -ne 10 -or $prompts.Count -eq 0 -or
        $definition.GetProperty('MinimumStageDurationMilliseconds', $members).GetValue($case) -le 0 -or
        $definition.GetProperty('MaximumStageDurationMilliseconds', $members).GetValue($case) -lt
            $definition.GetProperty('MinimumStageDurationMilliseconds', $members).GetValue($case)) {
        throw "Definição incompleta: $id"
    }

    for ($index = 0; $index -lt $stages.Count; $index++) {
        $stage = $stages[$index]
        foreach ($vital in 'Pulse', 'Systolic', 'Diastolic', 'RespiratoryRate', 'OxygenSaturation', 'TemperatureTenths') {
            $range = $stage.GetType().GetProperty($vital, $members).GetValue($stage)
            if ($range.Item1 -lt 0 -or $range.Item1 -gt $range.Item2 -or
                ($index -eq 9 -and ($range.Item1 -ne 0 -or $range.Item2 -ne 0))) {
                throw "Faixa inválida em $id, estágio $($index + 1), $vital."
            }
        }
    }

    # Inicializar o catálogo também executa a validação de 20 respostas por pergunta e estágio vivo.
    [pscustomobject]@{ Doenca = $id; Peso = $weight; Estagios = $stages.Count; Perguntas = $prompts.Count }
}

if ($weightTotal -le 0) {
    throw 'A soma dos pesos do catálogo precisa ser positiva.'
}
