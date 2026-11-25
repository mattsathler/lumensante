#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader', $true).GetMethod('Read', $static)
$json = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/acute-gastroenteritis.json').Path, [Text.Encoding]::UTF8)
$disease = $reader.Invoke($null, @($json))
$stateType = $assembly.GetType('lumen_sante.Clinical.PatientClinicalState', $true)
$medicationType = $assembly.GetType('lumen_sante.Clinical.MedicationClass', $true)
$state = [Activator]::CreateInstance($stateType, $true)
$initialize = $stateType.GetMethod('InitializeSymptoms', $instance)
$advance = $stateType.GetMethod('AdvanceSymptoms', $instance)
$pain = $stateType.GetProperty('PainIntensity', $instance)
$nausea = $stateType.GetProperty('NauseaIntensity', $instance)
$initialize.Invoke($state, @($disease, 5, $false))
$analgesic = [Array]::CreateInstance($medicationType, 1)
$analgesic.SetValue([Enum]::Parse($medicationType, 'Analgesic'), 0)
for ($i = 0; $i -lt 4; $i++) { $advance.Invoke($state, @($disease, 5, $false, $analgesic)) }
if ($pain.GetValue($state) -ne 3 -or $nausea.GetValue($state) -ne 7) { throw 'Analgesic did not selectively reduce pain.' }
$antiemetic = [Array]::CreateInstance($medicationType, 1)
$antiemetic.SetValue([Enum]::Parse($medicationType, 'Antiemetic'), 0)
for ($i = 0; $i -lt 4; $i++) { $advance.Invoke($state, @($disease, 5, $false, $antiemetic)) }
if ($pain.GetValue($state) -ne 7 -or $nausea.GetValue($state) -ne 3) { throw 'Antiemetic did not selectively reduce nausea.' }
$empty = [Array]::CreateInstance($medicationType, 0)
for ($i = 0; $i -lt 4; $i++) { $advance.Invoke($state, @($disease, 5, $false, $empty)) }
if ($nausea.GetValue($state) -ne 7) { throw 'Symptoms did not return after symptomatic treatment withdrawal.' }
$rhinitisJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/allergic-rhinitis.json').Path, [Text.Encoding]::UTF8)
$rhinitis = $reader.Invoke($null, @($rhinitisJson))
$nasalState = [Activator]::CreateInstance($stateType, $true)
$initialize.Invoke($nasalState, @($rhinitis, 5, $false))
$congestion = $stateType.GetProperty('NasalCongestionIntensity', $instance)
$antihistamine = [Array]::CreateInstance($medicationType, 1)
$antihistamine.SetValue([Enum]::Parse($medicationType, 'Antihistamine'), 0)
for ($i = 0; $i -lt 4; $i++) { $advance.Invoke($nasalState, @($rhinitis, 5, $false, $antihistamine)) }
if ($congestion.GetValue($nasalState) -ne 3) { throw 'Antihistamine did not reduce nasal congestion.' }
for ($i = 0; $i -lt 4; $i++) { $advance.Invoke($nasalState, @($rhinitis, 5, $false, $empty)) }
if ($congestion.GetValue($nasalState) -ne 7) { throw 'Nasal congestion did not return after withdrawal.' }
Write-Output 'Symptom relief and treatment withdrawal passed.'
