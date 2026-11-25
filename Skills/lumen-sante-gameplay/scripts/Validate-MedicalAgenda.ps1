#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Release/lumen-sante.dll')

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repositoryRoot 'Dependencies/RagePluginHookSDK.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'NonPublic, Instance'
$static = [Reflection.BindingFlags]'NonPublic, Static'
$agendaType = $assembly.GetType('lumen_sante.Gameplay.MedicalAgenda', $true)
$ordersType = $assembly.GetType('lumen_sante.Clinical.ExamOrders', $true)
$profileType = $assembly.GetType('lumen_sante.Clinical.PatientProfile', $true)
$recordType = $assembly.GetType('lumen_sante.Gameplay.MedicalAgenda+Record', $true)
$vitalsType = $assembly.GetType('lumen_sante.Gameplay.MedicalAgenda+SavedVitals', $true)
$agendaConstructor = $agendaType.GetConstructors($instance)[0]
$path = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'lumen-agenda-' + [Guid]::NewGuid() + '.json')
$log = [Action[string]]{ param($message) Write-Host $message }
$now = [DateTime]::new(2013, 12, 31, 14, 0, 0)
$random = [Random]::new(42)

function Assert($condition, $message) { if (!$condition) { throw $message } }
function Open-Agenda { $agendaConstructor.Invoke([object[]]@($path, $log)) }

try {
    $agenda = Open-Agenda
    $generate = $agendaType.GetMethod('Generate', $instance)
    $save = $agendaType.GetMethod('Save', $instance)
    $appointments = $agendaType.GetProperty('Appointments', $instance)
    $generate.Invoke($agenda, @($now, $random))
    Assert ($appointments.GetValue($agenda).Count -eq 13) 'Expected thirteen future slots.'

    $profile = $profileType.GetConstructor($instance, $null, [Type[]]@([Random]), $null).Invoke(@($random))
    $orders = $ordersType.GetConstructors($instance)[0].Invoke(@('Test Patient', 'Reception', 'flu-like', $profile))
    $ordersType.GetProperty('Notes', $instance).SetValue($orders, 'Follow-up note')
    $clinicalState = $ordersType.GetProperty('ClinicalState', $instance).GetValue($orders)
    $clinicalState.GetType().GetMethod('SetLeukocytes', $instance).Invoke($clinicalState, @(14800))
    $clinicalState.GetType().GetMethod('SetNaturalRecoveryProgress', $instance).Invoke($clinicalState, @(2))
    $clinicalState.GetType().GetMethod('RestoreSymptoms', $instance).Invoke($clinicalState, @([Nullable[int]]3, [Nullable[int]]5, [Nullable[int]]4))
    $clinicalState.GetType().GetMethod('SetChemistry', $instance).Invoke($clinicalState, @('sodium', [double]130))
    $setVitals = $clinicalState.GetType().GetMethod('SetVitals', $instance)
    $tupleType = $setVitals.GetParameters()[0].ParameterType
    $vitals = [Activator]::CreateInstance($tupleType, [object[]]@(91, 119, 79, 17, 95, [double]38.1))
    $setVitals.Invoke($clinicalState, [object[]]@($vitals))
    $exam = $ordersType.GetProperty('Items', $instance).GetValue($orders) | Where-Object {
        $_.GetType().GetProperty('Id', $instance).GetValue($_) -eq 'vital-signs'
    }
    $examType = $exam.GetType()
    [void]$examType.GetMethod('TryRequest', $instance).Invoke($exam, @(2))
    $statusType = $assembly.GetType('lumen_sante.Clinical.ExamStatus', $true)
    [void]$examType.GetMethod('TrySetStatus', $instance).Invoke($exam, @([Enum]::Parse($statusType, 'InProgress')))
    [void]$examType.GetMethod('TryComplete', $instance).Invoke($exam, @([long]123))
    $ordersType.GetMethod('RecordExamCompletion', $instance).Invoke($orders, [object[]]@($exam.PSObject.BaseObject))
    $measurements = $assembly.GetType('lumen_sante.Clinical.VitalMeasurements', $true)
    $findings = $measurements.GetMethod('Describe', $static).Invoke($null, @($clinicalState))
    $sample = $measurements.GetMethod('Capture', $static).Invoke($null, @($clinicalState))
    $examType.GetMethod('RecordMeasurements', $instance).Invoke($exam, [object[]](,$sample))
    $examType.GetMethod('RecordFindings', $instance).Invoke($exam, @($findings))
    $vitals = [Activator]::CreateInstance($tupleType, [object[]]@(100, 119, 79, 17, 95, [double]38.1))
    $setVitals.Invoke($clinicalState, [object[]]@($vitals))
    Assert ($examType.GetMethod('TryRequest', $instance).Invoke($exam, @(2))) 'Could not repeat vital measurements.'
    [void]$examType.GetMethod('TrySetStatus', $instance).Invoke($exam, @([Enum]::Parse($statusType, 'InProgress')))
    [void]$examType.GetMethod('TryComplete', $instance).Invoke($exam, @([long]456))
    $ordersType.GetMethod('RecordExamCompletion', $instance).Invoke($orders, [object[]]@($exam.PSObject.BaseObject))
    $newFindings = $measurements.GetMethod('Describe', $static).Invoke($null, @($clinicalState))
    $sample = $measurements.GetMethod('Capture', $static).Invoke($null, @($clinicalState))
    $examType.GetMethod('RecordMeasurements', $instance).Invoke($exam, [object[]](,$sample))
    $examType.GetMethod('RecordFindings', $instance).Invoke($exam, @($newFindings))
    $catalog = $assembly.GetType('lumen_sante.Clinical.ClinicalParameterCatalog', $true)
    $captureParameters = $measurements.GetMethod('CaptureParameters', $static)
    $savedMeasurementType = $assembly.GetType('lumen_sante.Clinical.ExamOrders+SavedMeasurement')
    $setChemistry = $clinicalState.GetType().GetMethod('SetChemistry', $instance)
    $completeNumeric = {
        param($numericExam, [long]$at)
        [void]$examType.GetMethod('TryRequest', $instance).Invoke($numericExam, @(2))
        [void]$examType.GetMethod('TrySetStatus', $instance).Invoke($numericExam, @([Enum]::Parse($statusType, 'InProgress')))
        [void]$examType.GetMethod('TryComplete', $instance).Invoke($numericExam, @($at))
        $ordersType.GetMethod('RecordExamCompletion', $instance).Invoke($orders, @($numericExam))
        $examParameters = $catalog.GetMethod('ForExam', $static).Invoke($null, @($examType.GetProperty('Id', $instance).GetValue($numericExam)))
        $valueList = @($captureParameters.Invoke($null, @($clinicalState, $examParameters)))
        $values = [Array]::CreateInstance($savedMeasurementType, $valueList.Count)
        for ($index = 0; $index -lt $valueList.Count; $index++) { $values.SetValue($valueList[$index], $index) }
        $measurementArguments = [object[]]::new(1)
        $measurementArguments[0] = $values
        $examType.GetMethod('RecordMeasurements', $instance).Invoke($numericExam, $measurementArguments)
        $findings = $measurements.GetMethod('FormatMeasurements', $static).Invoke($null, $measurementArguments)
        $examType.GetMethod('RecordFindings', $instance).Invoke($numericExam, [object[]]@([string]$findings))
    }
    $creatinineExam = $ordersType.GetProperty('Items', $instance).GetValue($orders) | Where-Object {
        $_.GetType().GetProperty('Id', $instance).GetValue($_) -eq 'creatinine'
    }
    $renalExam = $ordersType.GetProperty('Items', $instance).GetValue($orders) | Where-Object {
        $_.GetType().GetProperty('Id', $instance).GetValue($_) -eq 'renal'
    }
    $setChemistry.Invoke($clinicalState, @('creatinine', [double]1.1))
    & $completeNumeric $creatinineExam.PSObject.BaseObject ([long]500)
    $setChemistry.Invoke($clinicalState, @('creatinine', [double]2.2))
    & $completeNumeric $renalExam.PSObject.BaseObject ([long]600)
    $snapshot = $ordersType.GetMethod('Capture', $instance).Invoke($orders, @([long]0, $now))
    $record = [Activator]::CreateInstance($recordType)
    $record.Orders = $snapshot
    $record.Risk = 2
    $record.NextEvolutionTicks = $now.AddHours(2).Ticks
    $record.Vitals = [Activator]::CreateInstance($vitalsType)
    $record.Vitals.Pulse = 92
    $record.Vitals.Systolic = 120
    $record.Vitals.Diastolic = 80
    $record.Vitals.RespiratoryRate = 18
    $record.Vitals.OxygenSaturation = 96
    $record.Vitals.Temperature = 38.2
    $appointment = $agendaType.GetMethod('ScheduleReturn', $instance).Invoke($agenda, @($now, $record))
    Assert ($appointment.Due -eq [DateTime]::new(2014, 1, 1, 9, 0, 0).Ticks) 'Return must cross the year boundary.'
    $deadline = [DateTime]::new($appointment.Due).AddHours(1)
    $expires = $agendaType.GetMethod('ShouldExpire', $static)
    Assert (!$expires.Invoke($null, @($appointment, $deadline.AddMinutes(-1), $false))) 'Tolerance ended early.'
    Assert ($expires.Invoke($null, @($appointment, $deadline, $false))) 'Absent player passed the deadline.'
    $appointment.State = 'Ready'
    Assert (!$expires.Invoke($null, @($appointment, $deadline, $true))) 'Full reception must not expire a checked slot.'
    $agendaType.GetMethod('Expire', $instance).Invoke($agenda, @($appointment))
    Assert ($save.Invoke($agenda, @())) 'Could not save schedule.'

    $agenda = Open-Agenda
    $generate.Invoke($agenda, @($now, $random))
    Assert ($appointments.GetValue($agenda).Count -eq 14) 'Reload generated duplicate appointments.'
    $loaded = $appointments.GetValue($agenda) | Where-Object Id -eq $appointment.Id
    Assert ($loaded.State.ToString() -eq 'Pending') 'Pending state was lost.'
    Assert ($loaded.PatientId -eq $snapshot.Id) 'Patient link was lost.'
    $stored = $agendaType.GetProperty('Records', $instance).GetValue($agenda)[0].Orders
    Assert ($agendaType.GetProperty('Records', $instance).GetValue($agenda)[0].NextEvolutionTicks -eq $record.NextEvolutionTicks) 'Evolution deadline was lost.'
    $storedVitals = $agendaType.GetProperty('Records', $instance).GetValue($agenda)[0].Vitals
    Assert ($storedVitals.Pulse -eq 92 -and $storedVitals.OxygenSaturation -eq 96 -and $storedVitals.Temperature -eq 38.2) 'Vitals were lost on reload.'
    $restored = $ordersType.GetMethod('Restore', $static).Invoke($null, @($stored, [long]0, $now))
    Assert ($ordersType.GetProperty('AdmissionId', $instance).GetValue($restored) -eq $snapshot.Id) 'Record identity changed.'
    Assert ($ordersType.GetProperty('Notes', $instance).GetValue($restored) -eq 'Follow-up note') 'Record notes were lost.'
    $restoredClinical = $ordersType.GetProperty('ClinicalState', $instance).GetValue($restored)
    Assert ($restoredClinical.GetType().GetProperty('Leukocytes', $instance).GetValue($restoredClinical) -eq 14800) 'Lab value was not restored.'
    Assert ($restoredClinical.GetType().GetProperty('NaturalRecoveryProgress', $instance).GetValue($restoredClinical) -eq 2) 'Natural recovery progress was not restored.'
    Assert ($restoredClinical.GetType().GetProperty('PainIntensity', $instance).GetValue($restoredClinical) -eq 3 -and $restoredClinical.GetType().GetProperty('NauseaIntensity', $instance).GetValue($restoredClinical) -eq 5 -and $restoredClinical.GetType().GetProperty('NasalCongestionIntensity', $instance).GetValue($restoredClinical) -eq 4) 'Symptom intensities were not restored.'
    Assert ($restoredClinical.GetType().GetMethod('GetParameter', $instance).Invoke($restoredClinical, @('sodium')) -eq 130) 'Chemistry value was not restored.'
    $restoredVitals = $restoredClinical.GetType().GetProperty('Vitals', $instance).GetValue($restoredClinical)
    Assert ($restoredVitals.Item1 -eq 100 -and $restoredVitals.Item6 -eq 38.1) 'Clinical state was not restored with the record.'
    $restoredExam = $ordersType.GetProperty('Items', $instance).GetValue($restored) | Where-Object {
        $_.GetType().GetProperty('Id', $instance).GetValue($_) -eq 'vital-signs'
    }
    $allResults = @($examType.GetProperty('Results', $instance).GetValue($restoredExam))
    Assert ($allResults.Count -eq 2) 'Repeated measurements were not preserved.'
    $oldFindings = $examType.GetProperty('SavedFindings', $instance).GetValue($allResults[0])
    $newFindings = $examType.GetProperty('SavedFindings', $instance).GetValue($allResults[1])
    Assert ($oldFindings -match 'Pulso: 91 bpm' -and $oldFindings -match '60.*100 bpm' -and $newFindings -match 'Pulso: 100 bpm') 'Measured findings changed or lost their reference after reload.'
    $oldValues = $examType.GetProperty('Measurements', $instance).GetValue($allResults[0])
    $newValues = $examType.GetProperty('Measurements', $instance).GetValue($allResults[1])
    Assert ($oldValues.Count -eq 6 -and $oldValues[0].Id -eq 'pulse' -and $oldValues[0].Value -eq 91 -and $newValues[0].Value -eq 100) 'Structured measurements were not preserved independently.'
    $latestMeasurements = $assembly.GetType('lumen_sante.Clinical.DiagnosticInvestigation', $true).GetMethod('LatestMeasurements', $static).Invoke($null, @($restored))
    Assert ($latestMeasurements['pulse'] -eq 100) 'The most recent repeated measurement was not selected after reload.'
    Assert ($latestMeasurements['creatinine'] -eq 2.2) 'A later overlapping renal panel lost priority to an older single-analyte test.'
    $firstSequence = $examType.GetProperty('Sequence', $instance).GetValue($allResults[0])
    $secondSequence = $examType.GetProperty('Sequence', $instance).GetValue($allResults[1])
    Assert ($firstSequence -gt 0 -and $secondSequence -gt $firstSequence) 'Exam completion sequence was not persisted.'

    # Older version 1 records have no Vitals field and must remain readable.
    $legacy = [IO.File]::ReadAllText($path) -replace '"Vitals":\{[^}]*\},', ''
    [IO.File]::WriteAllText($path, $legacy)
    $legacyAgenda = Open-Agenda
    Assert ($agendaType.GetProperty('Available', $instance).GetValue($legacyAgenda)) 'Legacy agenda was rejected.'
    Assert ($null -eq $agendaType.GetProperty('Records', $instance).GetValue($legacyAgenda)[0].Vitals) 'Legacy vitals should be initialized on return.'

    $loaded.State = 'Cancelled'
    Assert ($save.Invoke($agenda, @())) 'Could not replace the schedule file.'
    $loaded = $appointments.GetValue((Open-Agenda)) | Where-Object Id -eq $appointment.Id
    Assert ($loaded.State.ToString() -eq 'Cancelled') 'Cancellation was lost.'

    $invalid = '{"Version":999}'
    [IO.File]::WriteAllText($path, $invalid)
    Assert (!$save.Invoke((Open-Agenda), @())) 'Invalid version must disable writes.'
    Assert ([IO.File]::ReadAllText($path) -eq $invalid) 'Invalid file was overwritten.'
    Write-Host 'Agenda persistence, year rollover, deduplication and cancellation passed.'
}
finally {
    foreach ($file in @($path, ($path + '.tmp'), ($path + '.bak'))) {
        if ([IO.File]::Exists($file)) { [IO.File]::Delete($file) }
    }
}
