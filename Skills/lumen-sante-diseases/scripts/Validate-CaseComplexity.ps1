#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$static = [Reflection.BindingFlags]'Static,NonPublic'
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$catalog = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog', $true)
$cases = $catalog.GetField('Cases', $static).GetValue($null)
if ($cases.Count -ne 10) { throw 'The standard catalog must contain exactly ten diseases.' }
$weights = @{}
$counts = @{}
foreach ($case in $cases) {
    $type = $case.GetType()
    $kind = $type.GetProperty('Complexity', $instance).GetValue($case)
    $weight = $type.GetProperty('SelectionWeight', $instance).GetValue($case)
    if ($kind -notin @('simple', 'investigative', 'complex')) { throw 'Invalid complexity.' }
    if (!$weights.ContainsKey($kind)) { $weights[$kind] = 0 }
    if (!$counts.ContainsKey($kind)) { $counts[$kind] = 0 }
    $counts[$kind]++
    $weights[$kind] += $weight
    if ($type.GetProperty('LeukocyteRanges', $instance).GetValue($case).Count -ne 9) { throw 'Disease lacks numeric leukocyte ranges.' }
}
if ($counts['simple'] -ne 8 -or $counts['complex'] -ne 2) { throw 'Expected eight simple and two complex diseases.' }
$total = ($weights.Values | Measure-Object -Sum).Sum
if (!$weights.ContainsKey('simple') -or $weights['simple'] / $total -le 0.8) { throw 'Simple case weight is not above 80%.' }
$pick = $catalog.GetMethod('Pick', $static)
$random = [Random]::new(1701)
$simple = 0
for ($i = 0; $i -lt 20000; $i++) {
    $case = $pick.Invoke($null, @($random))
    if ($case.GetType().GetProperty('Complexity', $instance).GetValue($case) -eq 'simple') { $simple++ }
}
if ($simple / 20000.0 -le 0.8) { throw 'Sample did not contain more than 80% simple cases.' }
Write-Output "Simple cases: $simple / 20000; configured share: $($weights['simple'] / $total)."
