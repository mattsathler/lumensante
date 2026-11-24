#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Release/lumen-sante.dll')

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repositoryRoot 'Dependencies/RagePluginHookSDK.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'NonPublic, Instance'
$static = [Reflection.BindingFlags]'NonPublic, Static'
$profilesType = $assembly.GetType('lumen_sante.Gameplay.DoctorProfiles', $true)
$agendaType = $assembly.GetType('lumen_sante.Gameplay.MedicalAgenda', $true)
$outfitType = $assembly.GetType('lumen_sante.Gameplay.DoctorProfiles+Outfit', $true)
$root = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'lumen-doctors-' + [Guid]::NewGuid().ToString('N'))
$log = [Action[string]]{ param($message) Write-Host $message }

function Assert($condition, $message) { if (!$condition) { throw $message } }
function Open-Profiles { $profilesType.GetConstructors($instance)[0].Invoke(@($root, $log)) }
function Open-Agenda([string]$path) { $agendaType.GetConstructors($instance)[0].Invoke(@($path, $log)) }
function Call($target, [string]$method, [object[]]$values) {
    $arguments = New-Object 'object[]' $values.Length
    for ($index = 0; $index -lt $values.Length; $index++) {
        $arguments[$index] = $values[$index].PSObject.BaseObject
    }
    $target.GetType().GetMethod($method, $instance).Invoke($target, $arguments)
}
function Read-Value($target, [string]$property) {
    $target.GetType().GetProperty($property, $instance).GetValue($target)
}

try {
    $startupType = $assembly.GetType('lumen_sante.Gameplay.StartupReadiness', $true)
    $startup = [Activator]::CreateInstance($startupType, $true)
    Assert (!(Call $startup Observe @($false, 1, [single]0, [single]0, [single]0, [long]0))) 'Loading state must not be ready.'
    Assert (!(Call $startup Observe @($true, 1, [single]0, [single]0, [single]0, [long]100))) 'First observation must not be ready.'
    Assert (!(Call $startup Observe @($true, 2, [single]0, [single]0, [single]0, [long]3000))) 'Changing the player must restart the wait.'
    Assert (!(Call $startup Observe @($true, 2, [single]10, [single]0, [single]0, [long]4000))) 'Changing position must restart the wait.'
    Assert (!(Call $startup Observe @($false, 2, [single]10, [single]0, [single]0, [long]7000))) 'Returning to a loading state must reset the wait.'
    Assert (!(Call $startup Observe @($true, 2, [single]10, [single]0, [single]0, [long]8000))) 'A new stable period must start after loading.'
    Assert (Call $startup Observe @($true, 2, [single]10, [single]0, [single]0, [long]12000)) 'A stable player should become ready.'
    $profiles = Open-Profiles
    $create = $profilesType.GetMethod('Create', $static)
    $one = $create.Invoke($null, @('Alex', $false))
    $two = $create.Invoke($null, @('Morgan', $true))
    $one.ResidenceId = 'grapeseed-1893'
    $two.ResidenceId = 'paleto-procopio-4401'
    $one.Face.Features[3] = 35
    foreach ($profile in @($one, $two)) {
        Assert ($profile.Outfits.Count -eq 2) 'Every doctor needs two outfits.'
        Assert (@($profile.Outfits | Where-Object Duty).Count -eq 1) 'A single starter outfit must be for duty.'
        Assert (Call $profiles Put @($profile)) 'Could not save doctor.'
    }
    Assert (Call $profiles SaveProfile @($one, $true)) 'Could not save the first default in the editor.'
    Assert (Call $profiles SaveProfile @($two, $true)) 'Could not switch default in the editor.'
    Assert (Call $profiles SaveProfile @($one, $false)) 'Could not save a non-default doctor.'
    Assert ((Read-Value $profiles DefaultId) -eq $two.Id) 'Editing another doctor cleared the default.'
    Assert (Call $profiles RememberCasual @($one.Id, $one.Outfits[1].Id)) 'Could not remember casual outfit.'

    $profiles = Open-Profiles
    Assert ((Read-Value $profiles DefaultId) -eq $two.Id) 'Only the last elected doctor should be default.'
    $restored = (Read-Value $profiles Items) | Where-Object Id -eq $one.Id
    $restoredTwo = (Read-Value $profiles Items) | Where-Object Id -eq $two.Id
    Assert ($restored.ResidenceId -eq 'grapeseed-1893' -and $restoredTwo.ResidenceId -eq 'paleto-procopio-4401') 'Residence selections did not persist separately.'
    Assert ($restored.Face.Features[3] -eq 35) 'Face features were lost.'
    Assert ($restored.LastCasual -eq $one.Outfits[1].Id) 'Last casual outfit was lost.'
    for ($index = 3; $index -le 21; $index++) {
        $outfit = [Activator]::CreateInstance($outfitType)
        $outfit.Name = 'Outfit ' + $index
        $restored.Outfits.Add($outfit)
        if ($index -eq 20) { Assert (Call $profiles Put @($restored)) 'Twenty outfits should be accepted.' }
    }
    Assert (!(Call $profiles Put @($restored))) 'An outfit above the limit should be refused.'
    $profiles = Open-Profiles
    $restored = (Read-Value $profiles Items) | Where-Object Id -eq $one.Id
    Assert ($restored.Outfits.Count -eq 20) 'The failed edit was written.'

    $legacyPath = [IO.Path]::Combine($root, 'agenda.json')
    $legacy = Open-Agenda $legacyPath
    $now = [DateTime]::new(2013, 12, 31, 14, 0, 0)
    Call $legacy Generate @($now, [Random]::new(4))
    $ordersType = $assembly.GetType('lumen_sante.Clinical.ExamOrders', $true)
    $profileType = $assembly.GetType('lumen_sante.Clinical.PatientProfile', $true)
    $recordType = $assembly.GetType('lumen_sante.Gameplay.MedicalAgenda+Record', $true)
    $patientProfile = $profileType.GetConstructor($instance, $null, [Type[]]@([Random]), $null).Invoke(@([Random]::new(7)))
    $orders = $ordersType.GetConstructors($instance)[0].Invoke(@('Patient', 'Reception', 'flu-like', $patientProfile))
    $ordersType.GetProperty('Notes', $instance).SetValue($orders, 'Keep this note with the return.')
    $record = [Activator]::CreateInstance($recordType)
    $record.Orders = Call $orders Capture @([long]0, $now)
    $record.Risk = 2
    $return = Call $legacy ScheduleReturn @($now, $record)
    Call $legacy Expire @($return)
    Assert (Call $legacy Save @()) 'Could not save legacy agenda.'
    $legacyContents = [IO.File]::ReadAllText($legacyPath)
    foreach ($profile in @($one, $two)) {
        $oldDirectory = [IO.Path]::Combine($root, 'Doctors', $profile.Id.ToString('N'))
        [void][IO.Directory]::CreateDirectory($oldDirectory)
        [IO.File]::Copy($legacyPath, [IO.Path]::Combine($oldDirectory, 'agenda.json'))
    }
    $firstPath = Call $profiles AgendaPath @($one.Id)
    $secondPath = Call $profiles AgendaPath @($two.Id)
    Assert ($firstPath -ne $secondPath) 'Both profiles share a save path.'
    $firstAgenda = Open-Agenda $firstPath
    $secondAgenda = Open-Agenda $secondPath
    Assert (@(Read-Value $firstAgenda Appointments).Count -eq 0) 'Old first agenda was loaded.'
    Assert (@(Read-Value $secondAgenda Appointments).Count -eq 0) 'Old second agenda was loaded.'
    Assert (@(Read-Value $firstAgenda Records).Count -eq 0) 'An old record survived the reset.'
    Call $firstAgenda Generate @($now, [Random]::new(9))
    Assert (Call $firstAgenda Save @()) 'Could not save the new agenda.'
    $profiles = Open-Profiles
    $firstAgenda = Open-Agenda $firstPath
    Assert (@(Read-Value $firstAgenda Appointments).Count -eq 13) 'New appointments did not survive reload.'
    Assert (@(Read-Value (Open-Agenda $secondPath) Appointments).Count -eq 0) 'The new agenda leaked to another doctor.'
    Assert ([IO.File]::ReadAllText($legacyPath) -eq $legacyContents) 'The previous shared file was changed.'
    Assert (Call $profiles Remove @($two.Id)) 'Could not delete the default doctor.'
    Assert ((Read-Value (Open-Profiles) DefaultId) -eq [Guid]::Empty) 'Removed doctor remained the default.'
    Assert (Call $profiles Remove @($one.Id)) 'Could not delete the first doctor.'
    Assert ([IO.File]::Exists($firstPath)) 'Deleting the doctor erased the archived agenda.'
    Write-Host 'Doctor defaults, outfits, fresh agendas, isolation and archive passed.'
}
finally {
    if ([IO.Directory]::Exists($root)) {
        $resolved = (Resolve-Path -LiteralPath $root).ProviderPath
        if ($resolved -eq $root -and [IO.Path]::GetDirectoryName($resolved) -eq [IO.Path]::GetTempPath().TrimEnd('\') -and
            [IO.Path]::GetFileName($resolved) -match '^lumen-doctors-[0-9a-f]{32}$') {
            [IO.Directory]::Delete($resolved, $true)
        }
    }
}
