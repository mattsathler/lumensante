#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$static = [Reflection.BindingFlags]'Static,NonPublic'
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$cases = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog').GetField('Cases', $static).GetValue($null)
$random = [Random]::new(42)
$changed = 0
$testedAntipyretic = $false
$testedOxygen = $false
foreach ($disease in $cases) {
    $stages = $disease.GetType().GetProperty('Stages', $instance).GetValue($disease)
    $stage = $stages[8]
    $sample = $stages[0].GetType().GetMethod('SampleVitals', $instance)
    $advance = $stage.GetType().GetMethod('AdvanceVitals', $instance)
    $temperatureRange = $stage.GetType().GetProperty('TemperatureTenths', $instance).GetValue($stage)
    $oxygenRange = $stage.GetType().GetProperty('OxygenSaturation', $instance).GetValue($stage)
    if (!$testedOxygen -and $oxygenRange.Item2 -lt 95) {
        $supportedAdvance = $stage.GetType().GetMethod('AdvanceSupportedVitals', $instance)
        $unsupported = $sample.Invoke($stage, @($random, $false))
        $stateType = $assembly.GetType('lumen_sante.Clinical.PatientClinicalState', $true)
        $state = [Activator]::CreateInstance($stateType, $true)
        $setVitals = $stateType.GetMethod('SetVitals', $instance)
        $intensity = $stateType.GetProperty('BreathlessnessIntensity', $instance)
        $setVitals.Invoke($state, [object[]]@($unsupported))
        $initialSymptom = $intensity.GetValue($state)
        if ($initialSymptom -le 0) { throw 'Low saturation did not produce breathlessness.' }
        $supported = $unsupported
        for ($step = 0; $step -lt 40; $step++) { $supported = $supportedAdvance.Invoke($stage, [object[]]@($supported, $false, $true)) }
        if ($supported.Item5 -lt 95 -or $supported.Item5 -le $unsupported.Item5) { throw 'Oxygen support did not improve saturation.' }
        $setVitals.Invoke($state, [object[]]@($supported))
        if ($intensity.GetValue($state) -ne 0) { throw 'Breathlessness did not improve with oxygen support.' }
        for ($step = 0; $step -lt 40; $step++) { $supported = $advance.Invoke($stage, [object[]]@($supported)) }
        if ($supported.Item5 -gt $oxygenRange.Item2) { throw 'Saturation did not return after oxygen withdrawal.' }
        $setVitals.Invoke($state, [object[]]@($supported))
        if ($intensity.GetValue($state) -le 0) { throw 'Breathlessness did not return after oxygen withdrawal.' }
        $testedOxygen = $true
    }
    if (!$testedAntipyretic -and $temperatureRange.Item1 -gt 374) {
        $treatedAdvance = $stage.GetType().GetMethod('AdvanceTreatedVitals', $instance)
        $fever = $sample.Invoke($stage, @($random, $false))
        $treated = $fever
        for ($step = 0; $step -lt 40; $step++) { $treated = $treatedAdvance.Invoke($stage, [object[]]@($treated, $true)) }
        if ($treated.Item6 -gt 37.4 -or $treated.Item6 -ge $fever.Item6) { throw 'Antipyretic did not lower fever.' }
        for ($step = 0; $step -lt 40; $step++) { $treated = $advance.Invoke($stage, [object[]]@($treated)) }
        if ($treated.Item6 -lt $temperatureRange.Item1 / 10.0) { throw 'Temperature did not return after treatment withdrawal.' }
        $testedAntipyretic = $true
    }
    $current = $sample.Invoke($stages[0], @($random, $false))
    for ($step = 0; $step -lt 40; $step++) {
        $next = $advance.Invoke($stage, [object[]]@($current))
        $ranges = @('Pulse', 'Systolic', 'Diastolic', 'RespiratoryRate', 'OxygenSaturation', 'TemperatureTenths')
        for ($i = 0; $i -lt $ranges.Count; $i++) {
            $range = $stage.GetType().GetProperty($ranges[$i], $instance).GetValue($stage)
            $before = [double]$current.('Item' + ($i + 1))
            $after = [double]$next.('Item' + ($i + 1))
            if ($i -eq 5) { $before *= 10; $after *= 10 }
            $target = [Math]::Max($range.Item1, [Math]::Min($range.Item2, $before))
            if ([Math]::Abs($after - $target) -gt [Math]::Abs($before - $target) + 0.00001) { throw 'Vital moved away from its target.' }
            if ([Math]::Abs($after - $before) -gt 0.00001) { $changed++ }
        }
        $current = $next
    }
    $stable = $advance.Invoke($stage, [object[]]@($current))
    if (!$current.Equals($stable)) { throw 'Vitals did not converge to the stage ranges.' }
    $recoveredStage = $stages[0]
    $recover = $recoveredStage.GetType().GetMethod('AdvanceRecoveredVitals', $instance)
    for ($step = 0; $step -lt 40; $step++) { $current = $recover.Invoke($recoveredStage, [object[]]@($current)) }
    $stable = $recover.Invoke($recoveredStage, [object[]]@($current))
    if (!$current.Equals($stable) -or $current.Item6 -lt 36 -or $current.Item6 -gt 37.4) {
        throw 'Recovered vital signs did not converge.'
    }
    $terminal = $advance.Invoke($stages[9], [object[]]@($current))
    if ($terminal.Item1 -ne 0 -or $null -ne $terminal.Item6) { throw 'Terminal stage did not clear vital signs.' }
}
if ($changed -eq 0) { throw 'No vital values evolved.' }
if (!$testedAntipyretic) { throw 'No febrile stage was tested.' }
if (!$testedOxygen) { throw 'No hypoxemic stage was tested.' }
Write-Host 'Vital values approach stage ranges gradually and remain stable within them.'
