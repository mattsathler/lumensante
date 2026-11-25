#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader', $true).GetMethod('Read', $static)
$json = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/gpa-pulmonary-renal.json').Path, [Text.Encoding]::UTF8)
$disease = $reader.Invoke($null, @($json))
$stateType = $assembly.GetType('lumen_sante.Clinical.PatientClinicalState', $true)
$state = [Activator]::CreateInstance($stateType, $true)
$stateType.GetMethod('InitializeLeukocytes', $instance).Invoke($state, @($disease, 0, [Random]::new(42)))
$value = $stateType.GetProperty('Leukocytes', $instance)
$initial = $value.GetValue($state)
if ($initial -lt 9000 -or $initial -gt 12500) { throw 'Initial leukocytes outside configured range.' }
$catalog = $assembly.GetType('lumen_sante.Clinical.ClinicalParameterCatalog', $true)
$parameters = $catalog.GetProperty('Hematology', $static).GetValue($null)
$capture = $assembly.GetType('lumen_sante.Clinical.VitalMeasurements', $true).GetMethod('CaptureParameters', $static)
$first = $capture.Invoke($null, @($state, $parameters))
$second = $capture.Invoke($null, @($state, $parameters))
if ($first[0].Id -ne 'leukocytes' -or $first[0].Value -ne $initial -or $second[0].Value -ne $initial) { throw 'Repeated collection changed patient values.' }
$advance = $stateType.GetMethod('AdvanceLeukocytes', $instance)
for ($i = 0; $i -lt 40; $i++) { $advance.Invoke($state, @($disease, 8, $false)) }
if ($value.GetValue($state) -lt 15000 -or $first[0].Value -ne $initial) { throw 'Lab progression or collection history failed.' }
for ($i = 0; $i -lt 40; $i++) { $advance.Invoke($state, @($disease, 0, $true)) }
if ($value.GetValue($state) -gt 11000) { throw 'Recovered lab value did not return to reference range.' }
$initializeChemistry = $stateType.GetMethod('InitializeChemistry', $instance)
$advanceChemistry = $stateType.GetMethod('AdvanceChemistry', $instance)
$parameterValue = $stateType.GetMethod('GetParameter', $instance)
$initializeChemistry.Invoke($state, @($disease, 0, [Random]::new(42)))
$initialCreatinine = $parameterValue.Invoke($state, @('creatinine'))
if ($initialCreatinine -lt 1.0 -or $initialCreatinine -gt 1.4) { throw 'Initial creatinine outside configured range.' }
for ($i = 0; $i -lt 40; $i++) { $advanceChemistry.Invoke($state, @($disease, 8, $false)) }
if ($parameterValue.Invoke($state, @('creatinine')) -lt 3.2) { throw 'Creatinine did not progress.' }
if ($parameterValue.Invoke($state, @('lung-opacities')) -lt 4 -or $parameterValue.Invoke($state, @('lung-nodules')) -lt 3) { throw 'Thoracic imaging features did not progress.' }
$imagingParameters = $catalog.GetMethod('ForExam', $static).Invoke($null, @('x-ray'))
if (@($imagingParameters).Count -ne 2) { throw 'Chest imaging should capture both configured features.' }
for ($i = 0; $i -lt 40; $i++) { $advanceChemistry.Invoke($state, @($disease, 0, $true)) }
if ($parameterValue.Invoke($state, @('creatinine')) -gt 1.3) { throw 'Creatinine did not return to the reference range.' }
$porphyriaJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/acute-intermittent-porphyria.json').Path, [Text.Encoding]::UTF8)
$porphyria = $reader.Invoke($null, @($porphyriaJson))
$sodiumState = [Activator]::CreateInstance($stateType, $true)
$initializeChemistry.Invoke($sodiumState, @($porphyria, 0, [Random]::new(7)))
for ($i = 0; $i -lt 40; $i++) { $advanceChemistry.Invoke($sodiumState, @($porphyria, 8, $false)) }
if ($parameterValue.Invoke($sodiumState, @('sodium')) -gt 130) { throw 'Sodium did not progress.' }
if ($parameterValue.Invoke($sodiumState, @('urine-pbg')) -lt 4.0) { throw 'Urinary porphobilinogen did not progress.' }
$pbgParameters = $catalog.GetMethod('ForExam', $static).Invoke($null, @('urine-pbg'))
$pbgResult = $capture.Invoke($null, @($sodiumState, $pbgParameters))
$pbgValue = $pbgResult[0].Value
$stateType.GetMethod('SetChemistry', $instance).Invoke($sodiumState, @('urine-pbg', [double]0.1))
if ($pbgResult[0].Value -ne $pbgValue) { throw 'Historical PBG measurement changed with patient state.' }
$cystitisJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/bacterial-cystitis.json').Path, [Text.Encoding]::UTF8)
$cystitis = $reader.Invoke($null, @($cystitisJson))
$urineState = [Activator]::CreateInstance($stateType, $true)
$initializeChemistry.Invoke($urineState, @($cystitis, 0, [Random]::new(5)))
for ($i = 0; $i -lt 40; $i++) { $advanceChemistry.Invoke($urineState, @($cystitis, 8, $false)) }
if ($parameterValue.Invoke($urineState, @('urine-leukocytes')) -lt 150) { throw 'Urinary leukocytes did not progress.' }
if ($parameterValue.Invoke($urineState, @('urine-culture-count')) -lt 100000) { throw 'Urinary culture count did not progress.' }
$urineParameters = $catalog.GetMethod('ForExam', $static).Invoke($null, @('urinalysis'))
$urineResult = $capture.Invoke($null, @($urineState, $urineParameters))
if ($urineResult.Count -ne 2 -or $urineResult[0].Id -ne 'urine-leukocytes' -or $urineResult[1].Id -ne 'urine-erythrocytes') { throw 'Urinalysis did not capture both parameters.' }
$renalParameters = $catalog.GetMethod('ForExam', $static).Invoke($null, @('renal'))
if (@($renalParameters).Count -ne 2) { throw 'Renal examination should measure creatinine and blood urea nitrogen.' }
$numericExamIds = @('mri','mri-brain','mri-spine','cardiac','blood-gas')
foreach ($examId in $numericExamIds) {
    if (@($catalog.GetMethod('ForExam', $static).Invoke($null, @($examId))).Count -eq 0) { throw "Legacy textual exam $examId was not migrated." }
}
$gasParameters = $catalog.GetMethod('ForExam', $static).Invoke($null, @('blood-gas'))
$gasState = [Activator]::CreateInstance($stateType, $true)
$gasVitals = [Activator]::CreateInstance($stateType.GetMethod('SetVitals', $instance).GetParameters()[0].ParameterType, [object[]]@(80, 120, 80, 18, 90, [double]37.0))
$stateType.GetMethod('SetVitals', $instance).Invoke($gasState, [object[]]@($gasVitals))
$gasResult = $capture.Invoke($null, @($gasState, $gasParameters))
if ($gasResult[0].Id -ne 'arterial-pao2' -or $gasResult[0].Value -ne 60) { throw 'Arterial oxygen did not track oxygen saturation.' }
Write-Output 'Lab generation, repeated collection, progression and recovery passed.'
