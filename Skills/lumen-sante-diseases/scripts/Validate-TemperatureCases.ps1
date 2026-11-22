#Requires -Version 5.1
[CmdletBinding()]
param([string]$AssemblyPath = (Join-Path $PSScriptRoot '../../../lumen-sante/bin/Debug/lumen-sante.dll'))

$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$cases = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog').GetField('Cases', $static).GetValue($null)
$febrileCases = @('flu-like', 'viral-pharyngitis', 'acute-sinusitis', 'bacterial-cystitis', 'acute-gastroenteritis')
$random = [Random]::new(42)
foreach ($disease in $cases) {
    $id = $disease.GetType().GetProperty('Id', $instance).GetValue($disease)
    $stages = $disease.GetType().GetProperty('Stages', $instance).GetValue($disease)
    $canHaveFever = $false
    for ($index = 0; $index -lt $stages.Count; $index++) {
        $stage = $stages[$index]
        $range = $stage.GetType().GetProperty('TemperatureTenths', $instance).GetValue($stage)
        $sample = $stage.GetType().GetMethod('SampleVitals', $instance)
        $terminal = $index -eq 9
        if ($range.Item1 -gt $range.Item2 -or (!$terminal -and ($range.Item1 -lt 300 -or $range.Item2 -gt 430))) {
            throw "Faixa de temperatura invalida: $id, estagio $($index + 1)"
        }
        if (!$terminal -and $range.Item2 -ge 380) { $canHaveFever = $true }
        for ($attempt = 0; $attempt -lt 100; $attempt++) {
            $vitals = $sample.Invoke($stage, @($random, $false))
            if ($terminal) {
                if ($null -ne $vitals.Item6) { throw "Obito com leitura de temperatura: $id" }
                continue
            }
            $temperature = $vitals.Item6
            if ($null -eq $temperature -or $temperature -lt $range.Item1 / 10.0 -or $temperature -gt $range.Item2 / 10.0 -or
                [Math]::Abs($temperature * 10 - [Math]::Round($temperature * 10)) -gt 0.000001) {
                throw "Amostra de temperatura invalida: $id, estagio $($index + 1)"
            }
            $recovered = $sample.Invoke($stage, @($random, $true))
            if ($recovered.Item6 -lt 36 -or $recovered.Item6 -gt 37.4) {
                throw "Recuperacao sem normalizar a temperatura: $id"
            }
        }
        if (!$terminal -and $id -notin $febrileCases -and ($range.Item1 -ne 360 -or $range.Item2 -ne 374)) {
            throw "Febre indevida em caso sem alteracao termica: $id"
        }
        if ($id -eq 'bacterial-cystitis' -and $index -lt 3 -and $range.Item2 -ge 380) {
            throw 'Cistite inicial com febre de complicacao.'
        }
    }
    if (($id -in $febrileCases) -ne $canHaveFever) { throw "Perfil febril incorreto: $id" }
    [pscustomobject]@{ Doenca = $id; Estagios = $stages.Count; PodeTerFebre = $canHaveFever }
}
