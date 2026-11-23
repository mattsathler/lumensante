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
    $snapshot = $ordersType.GetMethod('Capture', $instance).Invoke($orders, @([long]0, $now))
    $record = [Activator]::CreateInstance($recordType)
    $record.Orders = $snapshot
    $record.Risk = 2
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
    $restored = $ordersType.GetMethod('Restore', $static).Invoke($null, @($stored, [long]0, $now))
    Assert ($ordersType.GetProperty('AdmissionId', $instance).GetValue($restored) -eq $snapshot.Id) 'Record identity changed.'
    Assert ($ordersType.GetProperty('Notes', $instance).GetValue($restored) -eq 'Follow-up note') 'Record notes were lost.'

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
