#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader', $true).GetMethod('Read', $static)
$json = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/flu-like.json').Path, [Text.Encoding]::UTF8)
$disease = $reader.Invoke($null, @($json))
$snapshotType = $assembly.GetType('lumen_sante.Clinical.ExamOrders+Snapshot', $true)
$savedMedicationType = $assembly.GetType('lumen_sante.Clinical.ExamOrders+SavedMedication', $true)
$medicationType = $assembly.GetType('lumen_sante.Clinical.MedicationClass', $true)
$recordType = $assembly.GetType('lumen_sante.Gameplay.MedicalAgenda+Record', $true)
$advance = $assembly.GetType('lumen_sante.Gameplay.OutpatientEvolution', $true).GetMethod('Advance', $static)
$evolve = $disease.GetType().GetMethod('Evolve', $instance)
$now = [DateTime]::new(2014, 1, 3, 12, 0, 0)
$due = $now.AddMinutes(-1)
$medication = [Activator]::CreateInstance($savedMedicationType)
$medication.Class = [Enum]::Parse($medicationType, 'Analgesic')
$medication.Expires = $due.AddSeconds(30).Ticks
$snapshot = [Activator]::CreateInstance($snapshotType)
$snapshot.Discharged = $true
$snapshot.Medications = [Array]::CreateInstance($savedMedicationType, 1)
$snapshot.Medications[0] = $medication
$record = [Activator]::CreateInstance($recordType)
$record.Orders = $snapshot
$record.Risk = 5
$record.NextEvolutionTicks = $due.Ticks
$active = [Array]::CreateInstance($medicationType, 1)
$active.SetValue($medication.Class, 0)
$expected = $evolve.Invoke($disease, @(5, $false, $active, [Random]::new(9)))
$advance.Invoke($null, @($record, $disease, $now, [Random]::new(9)))
if ($record.Risk -ne $expected -or $record.NextEvolutionTicks -le $now.Ticks) { throw 'Historical medication or elapsed evolution was not applied correctly.' }
$risk = $record.Risk
$next = $record.NextEvolutionTicks
$advance.Invoke($null, @($record, $disease, $now, [Random]::new(11)))
if ($record.Risk -ne $risk -or $record.NextEvolutionTicks -ne $next) { throw 'Evolution was applied twice for the same interval.' }
Write-Output 'Outpatient elapsed evolution, medication expiry and idempotence passed.'
