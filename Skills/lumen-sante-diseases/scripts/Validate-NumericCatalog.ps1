#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$catalog = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog', $true)
$cases = $catalog.GetField('Cases', $static).GetValue($null)
$parameterCatalog = $assembly.GetType('lumen_sante.Clinical.ClinicalParameterCatalog', $true)
$parameters = @()
foreach ($group in @('Vitals','Hematology','Chemistry')) { $parameters += @($parameterCatalog.GetProperty($group, $static).GetValue($null)) }
$examMap = $parameterCatalog.GetMethod('ForExam', $static)
foreach ($exam in @('vital-signs','hematology','electrolytes','creatinine','urinalysis','crp','urea','renal','glucose','urine-culture','urine-pbg','x-ray','hepatic','coagulation','albumin','bilirubin','mri','mri-brain','mri-spine','cardiac','blood-gas')) {
    if (@($examMap.Invoke($null, @($exam))).Count -eq 0) { throw "No numerical parameters for $exam." }
}
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
foreach ($diseaseFile in Get-ChildItem (Join-Path $repositoryRoot 'lumen-sante/Diseases') -Filter '*.json') {
    $definition = Get-Content -LiteralPath $diseaseFile.FullName -Raw | ConvertFrom-Json
    if ($definition.PSObject.Properties.Name -contains 'Exams') {
        throw "The standard definition $($definition.Id) still contains the legacy textual Exams payload."
    }
    foreach ($entry in $definition.Exams.PSObject.Properties) {
        if ($null -eq $entry.Value) { continue }
        $legacy = $entry.Value
        $hasText = $null -ne $legacy.Default -or @($legacy.ByStage.PSObject.Properties | Where-Object { $null -ne $_.Value }).Count -gt 0
        if ($hasText -and @($examMap.Invoke($null, @($entry.Name))).Count -eq 0) {
            throw "Legacy textual result '$($entry.Name)' in $($definition.Id) has no numeric exam mapping."
        }
    }
}
$stateType = $assembly.GetType('lumen_sante.Clinical.PatientClinicalState', $true)
$excluded = $assembly.GetType('lumen_sante.Clinical.DiagnosticInvestigation', $true).GetMethod('Excluded', $static)
$tested = 0
foreach ($disease in $cases) {
    $stages = $disease.GetType().GetProperty('Stages', $instance).GetValue($disease)
    foreach ($stageIndex in 0..8) {
        foreach ($seed in 0..4) {
            $random = [Random]::new($seed)
            $state = [Activator]::CreateInstance($stateType, $true)
            $stage = $stages[$stageIndex]
            $vitals = $stage.GetType().GetMethod('SampleVitals', $instance).Invoke($stage, @($random, $false))
            $stateType.GetMethod('SetVitals', $instance).Invoke($state, [object[]]@($vitals))
            $stateType.GetMethod('InitializeLeukocytes', $instance).Invoke($state, @($disease, $stageIndex, $random))
            $stateType.GetMethod('InitializeChemistry', $instance).Invoke($state, @($disease, $stageIndex, $random))
            for ($update = 0; $update -lt 5; $update++) {
                $vitals = $stage.GetType().GetMethod('AdvanceSupportedVitals', $instance).Invoke($stage, [object[]]@($vitals, $true, $true))
                $stateType.GetMethod('SetVitals', $instance).Invoke($state, [object[]]@($vitals))
                $stateType.GetMethod('AdvanceLeukocytes', $instance).Invoke($state, @($disease, $stageIndex, $false))
                $stateType.GetMethod('AdvanceChemistry', $instance).Invoke($state, @($disease, $stageIndex, $false))
            }
            $values = [Collections.Generic.Dictionary[string,double]]::new([StringComparer]::Ordinal)
            foreach ($parameter in $parameters) {
                $id = $parameter.GetType().GetProperty('Id', $instance).GetValue($parameter)
                $value = $stateType.GetMethod('GetParameter', $instance).Invoke($state, @($id))
                if ($null -eq $value) { throw "Missing $id for generated patient." }
                $values[$id] = $value
            }
            if ($excluded.Invoke($null, @($disease, $values))) {
                throw "Generated or supported values excluded the real disease at stage $stageIndex."
            }
            $tested++
        }
    }
}
Write-Output "Validated $tested generated presentations and numerical exam mappings."
