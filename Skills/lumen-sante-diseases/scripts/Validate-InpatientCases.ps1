#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$static = [Reflection.BindingFlags]'Static,NonPublic'
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$catalog = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog', $true)
$cases = $catalog.GetField('Cases', $static).GetValue($null)
$inpatients = @($cases | Where-Object { $_.GetType().GetProperty('Tags', $instance).GetValue($_) -contains 'internacao' })
if ($inpatients.Count -ne 2) { throw 'Expected two inpatient cases.' }
$expected = @('acute-intermittent-porphyria', 'gpa-pulmonary-renal')
foreach ($case in $inpatients) {
    $type = $case.GetType()
    $id = $type.GetProperty('Id', $instance).GetValue($case)
    if ($id -notin $expected -or $type.GetProperty('Complexity', $instance).GetValue($case) -ne 'complex' -or
        !$type.GetProperty('RequiresSpecificTreatment', $instance).GetValue($case) -or
        $type.GetProperty('SelectionWeight', $instance).GetValue($case) -ne 50 -or
        $type.GetProperty('LeukocyteRanges', $instance).GetValue($case).Count -ne 9) {
        throw "Invalid inpatient definition: $id"
    }
    $stages = $type.GetProperty('Stages', $instance).GetValue($case)
    if ($stages.Count -ne 10 -or $type.GetProperty('Prompts', $instance).GetValue($case).Count -eq 0) {
        throw "Missing inpatient stages or symptoms: $id"
    }
    $medicationType = $assembly.GetType('lumen_sante.Clinical.MedicationClass', $true)
    $empty = [Array]::CreateInstance($medicationType, 0)
    $evolve = $type.GetMethod('Evolve', $instance)
    for ($seed = 0; $seed -lt 100; $seed++) {
        if ($evolve.Invoke($case, @(1, $false, $empty, [Random]::new($seed))) -eq 0) { throw "Untreated inpatient recovered: $id" }
    }
}
Write-Output 'Two rare inpatient cases, numeric ranges and treatment requirement validated.'
