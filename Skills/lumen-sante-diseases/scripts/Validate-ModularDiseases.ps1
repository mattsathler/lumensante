#Requires -Version 5.1
[CmdletBinding()]
param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../../../lumen-sante/bin/Debug/lumen-sante.dll'),
    [switch]$InstalledCheck
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repositoryRoot 'Dependencies/RagePluginHookSDK.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
if ($InstalledCheck) {
    $catalog = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog')
    if ($catalog.GetProperty('Count', $static).GetValue($null) -ne 13) { throw 'Catálogo instalado sem o 13º caso.' }
    $orders = $assembly.GetType('lumen_sante.Clinical.ExamOrders').GetConstructors($instance)[0].Invoke(@('Teste', 'Quarto', 'external-test', $null))
    $exam = @($orders.GetType().GetProperty('Items', $instance).GetValue($orders) | Where-Object {
        $_.GetType().GetProperty('Id', $instance).GetValue($_) -eq 'x-ray'
    })[0]
    [void]$exam.GetType().GetMethod('TryRequest', $instance).Invoke($exam, @([int]1))
    $findings = $assembly.GetType('lumen_sante.Clinical.ExamFindings').GetMethod('Describe', $static)
    if ($findings.Invoke($null, @($orders, $exam.PSObject.BaseObject)) -ne 'Laudo externo de teste') { throw 'Laudo externo não chegou ao consumidor do catálogo.' }
    Write-Output 'Descoberta pela pasta da DLL e laudo do caso externo validados no catálogo real.'
    return
}
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader').GetMethod('Read', $static)
$load = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalogLoader').GetMethod('Load', $static)
$template = [IO.File]::ReadAllText((Join-Path $repositoryRoot 'lumen-sante/Data/Doencas/flu-like.json'))
$script:messages = [Collections.Generic.List[string]]::new()
$logger = [Action[string]]{ param($message) $script:messages.Add($message) }
$folder = Join-Path ([IO.Path]::GetTempPath()) ('lumen-diseases-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($folder)
function Property($object, $name) { $object.GetType().GetProperty($name, $instance).GetValue($object) }
function Write-Disease($name, $data) {
    [IO.File]::WriteAllText((Join-Path $folder $name), ($data | ConvertTo-Json -Depth 35), [Text.UTF8Encoding]::new($false))
}
function Load-Cases { ,$load.Invoke($null, @([string]$folder, $logger)) }
function Assert-Invalid($data, $field) {
    try { $null = $reader.Invoke($null, @([string]($data | ConvertTo-Json -Depth 35))) }
    catch {
        if ($_.Exception.InnerException.Message -notlike "*$field*") { throw }
        return
    }
    throw "Arquivo inválido aceito: $field"
}
try {
    $official = Load-Cases
    if ($official.Count -ne 12) { throw 'Esperados doze casos oficiais.' }
    if (@($assembly.GetManifestResourceNames() | Where-Object { $_ -like '*.Data.Doencas.*.json' }).Count -ne 12) {
        throw 'Recursos oficiais ausentes.'
    }
    $data = $template | ConvertFrom-Json
    $data.Id = 'external-test'
    $data.Name = 'Doença externa de validação'
    $data.Exams.'x-ray'.Default = 'Laudo externo de teste'
    $data.Exams.'x-ray'.ByStage | Add-Member -MemberType NoteProperty -Name '2' -Value $null
    Write-Disease 'external.json' $data
    $installed = Join-Path $folder 'installed'
    [void][IO.Directory]::CreateDirectory((Join-Path $installed 'LumenSante/Doencas'))
    Copy-Item -LiteralPath $AssemblyPath -Destination (Join-Path $installed 'lumen-sante.dll')
    Copy-Item -LiteralPath (Join-Path $folder 'external.json') -Destination (Join-Path $installed 'LumenSante/Doencas/external.json')
    & powershell.exe -NoProfile -File $PSCommandPath -AssemblyPath (Join-Path $installed 'lumen-sante.dll') -InstalledCheck
    if ($LASTEXITCODE -ne 0) { throw 'Falha na validação da instalação externa.' }
    $loaded = Load-Cases
    $external = @($loaded | Where-Object { (Property $_ 'Id') -eq 'external-test' })
    if ($loaded.Count -ne 13 -or $external.Count -ne 1) { throw 'A doença externa não foi carregada.' }
    $definition = $external[0]
    $find = $definition.GetType().GetMethod('FindExamResult', $instance)
    if ($find.Invoke($definition, @('x-ray', [int]1)) -ne 'Laudo externo de teste' -or
        $null -ne $find.Invoke($definition, @('x-ray', [int]2)) -or
        $null -ne $find.Invoke($definition, @('unknown', [int]1))) { throw 'Falha no laudo padrão ou null explícito.' }
    $pick = $definition.GetType().GetMethod('PickAnswer', $instance)
    if ([string]::IsNullOrWhiteSpace($pick.Invoke($definition, @([int]0, [int]0, [Random]::new(42))))) { throw 'Resposta externa vazia.' }
    $sample = (Property $definition 'Stages')[0].GetType().GetMethod('SampleVitals', $instance)
    if ($sample.Invoke((Property $definition 'Stages')[0], @([Random]::new(42), $false)).Item6 -le 0) { throw 'Temperatura externa inválida.' }
    if ((Property $definition 'MedicationEffects').Count -eq 0) { throw 'Medicações externas ausentes.' }
    Write-Disease 'duplicate.json' $data
    if ((Load-Cases).Count -ne 12) { throw 'IDs externos duplicados não foram rejeitados.' }
    [IO.File]::Delete((Join-Path $folder 'duplicate.json'))
    $data.Id = 'flu-like'
    Write-Disease 'external.json' $data
    if ((Load-Cases).Count -ne 12) { throw 'ID oficial foi substituído.' }
    [IO.File]::Delete((Join-Path $folder 'external.json'))
    [IO.File]::WriteAllText((Join-Path $folder 'broken.json'), '{invalid')
    if ((Load-Cases).Count -ne 12) { throw 'JSON inválido interrompeu o catálogo.' }
    if (!$script:messages.Exists([Predicate[string]]{ param($message) $message.Contains('broken.json') })) { throw 'Log sem arquivo inválido.' }
    $data = $template | ConvertFrom-Json
    $data.SchemaVersion = 2
    Assert-Invalid $data 'SchemaVersion'
    $data = $template | ConvertFrom-Json
    $data.Stages[0].Pulse[0] = 80.5
    Assert-Invalid $data 'Pulse'
    $data = $template | ConvertFrom-Json
    $data.MedicationEffects[0].Medication = 'Unknown'
    Assert-Invalid $data 'Medication'
    $data = $template | ConvertFrom-Json
    $data.Exams | Add-Member -NotePropertyName 'unknown-exam' -NotePropertyValue $null
    Assert-Invalid $data 'unknown-exam'
    $data = $template | ConvertFrom-Json
    $data.Questions[0].AnswersByStage[0] = @('Única frase')
    Assert-Invalid $data 'AnswersByStage'
    $data = $template | ConvertFrom-Json
    $data.WorseningRisk = 2
    Assert-Invalid $data 'WorseningRisk'
    [IO.File]::Delete((Join-Path $folder 'broken.json'))
    if ((Load-Cases).Count -ne 12) { throw 'Remoção de arquivo não restaurou o catálogo.' }
    Write-Output 'Doze casos oficiais, 13º caso externo, respostas, vitais, laudos, duplicatas, remoção e arquivos inválidos validados.'
}
finally {
    # Delete only files created by this validation inside its unique temporary directory.
    foreach ($name in 'external.json', 'duplicate.json', 'broken.json') {
        $file = Join-Path $folder $name
        if ([IO.File]::Exists($file)) { [IO.File]::Delete($file) }
    }
    if ($installed -and [IO.Directory]::Exists($installed)) {
        foreach ($name in 'lumen-sante.dll', 'LumenSante/Doencas/external.json') {
            $file = Join-Path $installed $name
            if ([IO.File]::Exists($file)) { [IO.File]::Delete($file) }
        }
        [IO.Directory]::Delete((Join-Path $installed 'LumenSante/Doencas'))
        [IO.Directory]::Delete((Join-Path $installed 'LumenSante'))
        [IO.Directory]::Delete($installed)
    }
    [IO.Directory]::Delete($folder)
}
