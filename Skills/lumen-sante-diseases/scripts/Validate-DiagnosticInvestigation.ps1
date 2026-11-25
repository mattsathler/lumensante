#Requires -Version 5.1
param([string]$AssemblyPath = 'lumen-sante/bin/Debug/lumen-sante.dll')
$ErrorActionPreference = 'Stop'
[void][Reflection.Assembly]::LoadFrom((Resolve-Path 'Dependencies/RagePluginHookSDK.dll').Path)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$static = [Reflection.BindingFlags]'Static,NonPublic'
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader', $true).GetMethod('Read', $static)
$json = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/flu-like.json').Path, [Text.Encoding]::UTF8)
$rules = '"DiagnosticExclusions":[[{"Parameter":"oxygen-saturation","Min":0,"Max":60},{"Parameter":"pulse","Min":0,"Max":200}]],'
$json = [regex]::Replace($json, '(?m)^[ \t]*"DiagnosticExclusions":[^\r\n]*\r?\n', '')
$json = $json.Replace('"Complexity": "simple",', '"Complexity": "simple",' + $rules)
$disease = $reader.Invoke($null, @($json))
$investigation = $assembly.GetType('lumen_sante.Clinical.DiagnosticInvestigation', $true).GetMethod('Excluded', $static)
$values = [Collections.Generic.Dictionary[string,double]]::new([StringComparer]::Ordinal)
$values['oxygen-saturation'] = 55
if ($investigation.Invoke($null, @($disease, $values))) { throw 'An incomplete combination excluded a disease.' }
$values['pulse'] = 90
if (!$investigation.Invoke($null, @($disease, $values))) { throw 'A complete exclusion combination did not match.' }
$values['oxygen-saturation'] = 95
if ($investigation.Invoke($null, @($disease, $values))) { throw 'An unrelated measurement excluded a disease.' }
$badJson = $json.Replace('"Max":60', '"Max":100')
$rejected = $false
try { [void]$reader.Invoke($null, @($badJson)) } catch { $rejected = $true }
if (!$rejected) { throw 'A rule that can exclude its own generated values was accepted.' }
$gpaJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/gpa-pulmonary-renal.json').Path, [Text.Encoding]::UTF8)
$gpa = $reader.Invoke($null, @($gpaJson))
$values.Clear()
$values['sodium'] = 119
if ($investigation.Invoke($null, @($gpa, $values))) { throw 'Sodium alone must not satisfy the combined exclusion.' }
$values['creatinine'] = 1.0
if (!$investigation.Invoke($null, @($gpa, $values))) { throw 'Configured sodium and creatinine combination did not exclude the hypothesis.' }
$porphyriaJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/acute-intermittent-porphyria.json').Path, [Text.Encoding]::UTF8)
$porphyria = $reader.Invoke($null, @($porphyriaJson))
$values['sodium'] = 140
$values['creatinine'] = 3.5
if (!$investigation.Invoke($null, @($porphyria, $values))) { throw 'Configured creatinine and sodium combination did not exclude the hypothesis.' }
$values.Clear()
$values['urine-leukocytes'] = 120
if ($investigation.Invoke($null, @($gpa, $values))) { throw 'Urine leukocytes alone must not satisfy the combined exclusion.' }
$values['urine-erythrocytes'] = 5
if (!$investigation.Invoke($null, @($gpa, $values))) { throw 'Configured urinary combination did not exclude the hypothesis.' }
$cystitisJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/bacterial-cystitis.json').Path, [Text.Encoding]::UTF8)
$cystitis = $reader.Invoke($null, @($cystitisJson))
$values.Clear()
$values['urine-erythrocytes'] = 80
$values['creatinine'] = 4
if (!$investigation.Invoke($null, @($cystitis, $values))) { throw 'Configured hematuria and creatinine combination did not exclude the hypothesis.' }
$fluJson = [IO.File]::ReadAllText((Resolve-Path 'lumen-sante/Diseases/flu-like.json').Path, [Text.Encoding]::UTF8)
$flu = $reader.Invoke($null, @($fluJson))
$values.Clear()
$values['urine-pbg'] = 1.0
if ($investigation.Invoke($null, @($flu, $values))) { throw 'PBG alone must not satisfy the combined exclusion.' }
$values['sodium'] = 125
if (!$investigation.Invoke($null, @($flu, $values))) { throw 'Configured PBG and sodium combination did not exclude the hypothesis.' }
$values.Clear()
$values['urine-culture-count'] = 150000
$values['urine-leukocytes'] = 150
if (!$investigation.Invoke($null, @($flu, $values))) { throw 'Configured culture and urine-cell combination did not exclude the hypothesis.' }
Write-Output 'Diagnostic combinations, missing measurements and self-exclusion validation passed.'
