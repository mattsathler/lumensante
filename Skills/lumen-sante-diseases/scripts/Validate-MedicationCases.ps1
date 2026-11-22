#Requires -Version 5.1
[CmdletBinding()]
param([string]$AssemblyPath = (Join-Path $PSScriptRoot '../../../lumen-sante/bin/Debug/lumen-sante.dll'))

$ErrorActionPreference = 'Stop'
$sdkPath = Join-Path $PSScriptRoot '../../../Dependencies/RagePluginHookSDK.dll'
if (Test-Path -LiteralPath $sdkPath) {
    [void][Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $sdkPath).Path)
}
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$cases = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog').GetField('Cases', $static).GetValue($null)
$medicationType = $assembly.GetType('lumen_sante.Clinical.MedicationClass')
$diseaseType = $assembly.GetType('lumen_sante.Clinical.DiseaseDefinition')
$evolve = $diseaseType.GetMethod('Evolve', $instance)
$empty = [Array]::CreateInstance($medicationType, 0)
$tested = 0
foreach ($disease in $cases) {
    if ($diseaseType.GetProperty('RequiresSpecificTreatment', $instance).GetValue($disease)) { continue }
    $id = $diseaseType.GetProperty('Id', $instance).GetValue($disease)
    $effects = $diseaseType.GetProperty('MedicationEffects', $instance).GetValue($disease)
    if ($effects.Count -eq 0) { throw "Caso sem medicação compatível: $id" }
    foreach ($effect in $effects) {
        $medication = $effect.GetType().GetProperty('Medication', $instance).GetValue($effect)
        $active = [Array]::CreateInstance($medicationType, 1)
        $active.SetValue($medication, 0)
        $recovered = $false
        for ($seed = 0; $seed -lt 100; $seed++) {
            $result = $evolve.Invoke($disease, @([int]1, $false, $active, [Random]::new($seed)))
            if ($result -eq 0) { $recovered = $true }
            if ($evolve.Invoke($disease, @([int]1, $true, $empty, [Random]::new($seed))) -eq 0) {
                throw "Tratamento específico substituiu medicação em $id"
            }
            foreach ($risk in 0..10) {
                $next = $evolve.Invoke($disease, @([int]$risk, $false, $active, [Random]::new($seed)))
                if ($next -lt 0 -or $next -gt 10 -or [Math]::Abs($next - $risk) -gt 1 -or
                    (($risk -eq 0 -or $risk -eq 10) -and $next -ne $risk)) {
                    throw "Transição inválida em $id, risco $risk"
                }
            }
        }
        if (!$recovered) { throw "Medicação $medication nunca recuperou o caso $id" }
    }
    $compatible = @($effects | ForEach-Object { $_.GetType().GetProperty('Medication', $instance).GetValue($_) })
    foreach ($medication in [Enum]::GetValues($medicationType)) {
        if ($compatible -contains $medication) { continue }
        $wrong = [Array]::CreateInstance($medicationType, 1)
        $wrong.SetValue($medication, 0)
        for ($seed = 0; $seed -lt 100; $seed++) {
            if ($evolve.Invoke($disease, @([int]1, $false, $wrong, [Random]::new($seed))) -eq 0) {
                throw "Medicação incompatível $medication curou $id"
            }
        }
    }
    $tested++
}
Write-Output "$tested casos de medicação validados: recuperação, incompatibilidade, limites e estados finais em 100 sementes."
