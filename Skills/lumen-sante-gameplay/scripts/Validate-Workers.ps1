#Requires -Version 5.1
[CmdletBinding()]
param([string]$AssemblyPath)

$ErrorActionPreference = 'Stop'
if (!$AssemblyPath) { $AssemblyPath = Join-Path $PSScriptRoot '../../../lumen-sante/bin/Debug/lumen-sante.dll' }
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repositoryRoot 'Dependencies/RagePluginHookSDK.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic,Public'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$cases = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog').GetField('Cases', $static).GetValue($null)
$rulesType = $assembly.GetType('lumen_sante.Clinical.WorkerDiagnosticRules')
$stateType = $assembly.GetType('lumen_sante.Clinical.WorkerDiagnosis')
$orderType = $assembly.GetType('lumen_sante.Clinical.ExamOrders')
$examType = $assembly.GetType('lumen_sante.Clinical.ExamOrder')
$statusType = $assembly.GetType('lumen_sante.Clinical.ExamStatus')
$inProgress = [Enum]::Parse($statusType, 'InProgress')
$emptyExams = [Array]::CreateInstance($examType, 0)
function Assert($condition, $message) { if (!$condition) { throw $message } }
function Property($object, $name) { $object.GetType().GetProperty($name, $instance).GetValue($object) }
function Invoke-Method($object, $name, [object[]]$arguments) { $object.GetType().GetMethod($name, $instance).Invoke($object, $arguments) }
function New-Rules($definitions) {
    $arguments = [object[]]::new(1)
    $arguments[0] = $definitions
    $rulesType.GetConstructors($instance)[0].Invoke($arguments)
}
function New-Orders { $orderType.GetConstructors($instance)[0].Invoke(@('Teste', 'Quarto', 'flu-like', $null)) }
function New-Exam($id) {
    $orders = New-Orders
    foreach ($exam in (Property $orders 'Items')) {
        if ((Property $exam 'Id') -eq $id) { return $exam }
    }
    throw "Exame ausente: $id"
}
function Complete-Exam($exam) {
    [void](Invoke-Method $exam 'TrySetStatus' @($inProgress))
    Assert (Invoke-Method $exam 'TryComplete' @([long]100)) 'Não foi possível concluir o exame de teste.'
}

$rules = New-Rules $cases
$choose = $rulesType.GetMethod('Choose', $instance)
$tested = 0
foreach ($disease in $cases) {
    $id = Property $disease 'Id'
    $accuracy = Invoke-Method $rules 'Accuracy' @($disease)
    $correct = 0
    for ($seed = 0; $seed -lt 1000; $seed++) {
        $hypothesis = $choose.Invoke($rules, @($disease, [Random]::new($seed), [double]0))
        if ($hypothesis -eq $disease) { $correct++ }
        elseif ($null -ne $hypothesis) {
            $shared = @((Property $disease 'Tags') | Where-Object { (Property $hypothesis 'Tags') -contains $_ })
            Assert ($shared.Count -gt 0) "Hipótese incompatível com as tags de $id"
        }
    }
    Assert ([Math]::Abs($correct / 1000.0 - $accuracy) -lt 0.04) "Distribuição de diagnóstico incompatível com a raridade de $id"
    $tested++
}
$flu = @($cases | Where-Object { (Property $_ 'Id') -eq 'flu-like' })[0].PSObject.BaseObject
$pharyngitis = @($cases | Where-Object { (Property $_ 'Id') -eq 'viral-pharyngitis' })[0].PSObject.BaseObject
$sinusitis = @($cases | Where-Object { (Property $_ 'Id') -eq 'acute-sinusitis' })[0].PSObject.BaseObject
# Exercise all frequency bands in a controlled catalog, independent of installed case count.
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader').GetMethod('Read', $static)
$frequencyCases = [Array]::CreateInstance($flu.GetType(), 3)
$frequencyIds = @('flu-like', 'viral-pharyngitis', 'gpa-pulmonary-renal')
$frequencyWeights = @(90, 8, 2)
foreach ($index in 0..2) {
    $fixture = Get-Content (Join-Path $repositoryRoot ('lumen-sante/Diseases/' + $frequencyIds[$index] + '.json')) -Raw -Encoding UTF8 | ConvertFrom-Json
    $fixture.SelectionWeight = $frequencyWeights[$index]
    $frequencyCases.SetValue($reader.Invoke($null, @([string]($fixture | ConvertTo-Json -Depth 35))), $index)
}
$frequencyRules = New-Rules $frequencyCases
Assert ((Invoke-Method $frequencyRules 'Accuracy' @($frequencyCases.GetValue(0))) -eq 0.95) 'Faixa comum incorreta.'
Assert ((Invoke-Method $frequencyRules 'Accuracy' @($frequencyCases.GetValue(1))) -eq 0.85) 'Faixa intermediária incorreta.'
Assert ((Invoke-Method $frequencyRules 'Accuracy' @($frequencyCases.GetValue(2))) -eq 0.65) 'Faixa rara incorreta.'
$improved = 0
for ($seed = 0; $seed -lt 1000; $seed++) {
    if ($choose.Invoke($frequencyRules, @($frequencyCases.GetValue(2), [Random]::new($seed), [double]0.15)) -eq $frequencyCases.GetValue(2)) { $improved++ }
}
Assert ([Math]::Abs($improved / 1000.0 - 0.80) -lt 0.04) 'Bônus de evidência não aumentou o acerto da doença rara.'
$crp = New-Exam 'crp'
[void](Invoke-Method $crp 'TryRequest' @([int]1))
Assert (!(Invoke-Method $rules 'Distinguishes' @($pharyngitis, $sinusitis, $crp.PSObject.BaseObject))) 'Laudos iguais distinguiram duas doenças.'
Assert ((Invoke-Method $rules 'ExamPriority' @($flu, 'x-ray-ankle', [int]1)) -eq 0) 'Exame inconclusivo ganhou prioridade.'
$authoredExam = New-Exam 'x-ray'
Assert (Invoke-Method $authoredExam 'TryRequestByWorker' @([int]1)) 'Pedido do Doctor falhou.'
Assert (!(Property $authoredExam 'RequestedByPlayer')) 'Pedido do Doctor foi atribuído ao jogador.'
[void](Invoke-Method $authoredExam 'Cancel' @())
Assert (Invoke-Method $authoredExam 'TryRequest' @([int]2)) 'Pedido do jogador após cancelamento falhou.'
Assert (Property $authoredExam 'RequestedByPlayer') 'Pedido do jogador não preservou sua autoria.'
Assert (!(Invoke-Method $authoredExam 'TryRequestByWorker' @([int]2))) 'Doctor duplicou pedido do jogador.'
Assert (Property $authoredExam 'RequestedByPlayer') 'Tentativa rejeitada do Doctor alterou autoria do pedido.'
[void](Invoke-Method $authoredExam 'Cancel' @())
Assert (Invoke-Method $authoredExam 'TryRequestByWorker' @([int]3)) 'Pedido do Doctor após cancelamento falhou.'
Assert (!(Property $authoredExam 'RequestedByPlayer')) 'Novo pedido do Doctor herdou notificações do jogador.'

$allergicRhinitis = @($cases | Where-Object { (Property $_ 'Id') -eq 'allergic-rhinitis' })[0].PSObject.BaseObject
$state = [Activator]::CreateInstance($stateType, $true)
$stateType.GetProperty('HasAssessment', $instance).SetValue($state, $true)
$stateType.GetProperty('Hypothesis', $instance).SetValue($state, $allergicRhinitis)
$originalHypothesis = Property $state 'Hypothesis'
$random = [Random]::new(123)
Assert (!(Invoke-Method $state 'Assess' @($rules, $flu, $emptyExams, $random))) 'Visita repetida gerou novo diagnóstico.'
Assert ($random.NextDouble() -eq [Random]::new(123).NextDouble()) 'Visita repetida consumiu novo sorteio.'
Assert ((Property $state 'Hypothesis') -eq $originalHypothesis) 'Troca de visita mudou a hipótese sem evidência.'

$inconclusive = New-Exam 'x-ray-ankle'
[void](Invoke-Method $inconclusive 'TryRequest' @([int]1))
Complete-Exam $inconclusive
$single = [Array]::CreateInstance($examType, 1)
$single.SetValue($inconclusive.PSObject.BaseObject, 0)
Assert (!(Invoke-Method $state 'Assess' @($rules, $flu, $single, [Random]::new(456)))) 'Laudo inconclusivo gerou revisão.'

$evidence = New-Exam 'crp'
Assert (Invoke-Method $evidence 'TryRequest' @([int]1)) 'Não foi possível solicitar o exame diferenciador.'
$single.SetValue($evidence, 0)
Assert (!(Invoke-Method $state 'Assess' @($rules, $flu, $single, [Random]::new(789)))) 'Pedido pendente gerou revisão.'
Complete-Exam $evidence
$savedMeasurementType = $assembly.GetType('lumen_sante.Clinical.ExamOrders+SavedMeasurement')
$evidenceMeasurement = [Activator]::CreateInstance($savedMeasurementType)
$evidenceMeasurement.Id = 'crp'
$evidenceMeasurement.Name = 'Proteína C reativa'
$evidenceMeasurement.Unit = 'mg/L'
$evidenceMeasurement.Minimum = 0
$evidenceMeasurement.Maximum = 4.9
$evidenceMeasurement.Decimals = 1
$evidenceMeasurement.Value = 8
$evidenceMeasurements = [Array]::CreateInstance($savedMeasurementType, 1)
$evidenceMeasurements[0] = $evidenceMeasurement
$evidence.GetType().GetMethod('RecordMeasurements', $instance).Invoke($evidence, [object[]](,$evidenceMeasurements))
Invoke-Method $evidence 'RecordFindings' @('PCR: 8 mg/L')
Assert (Invoke-Method $state 'Assess' @($rules, $flu, $single, [Random]::new(789))) 'Exame concluído não liberou revisão.'
Assert (!(Invoke-Method $state 'Assess' @($rules, $flu, $single, [Random]::new(790)))) 'O mesmo resultado foi usado duas vezes.'
$confirmed = [Activator]::CreateInstance($stateType, $true)
[void](Invoke-Method $confirmed 'Assess' @($rules, $flu, $emptyExams, [Random]::new(1)))
Assert ((Property $confirmed 'Hypothesis') -eq $flu) 'Semente de confirmação gerou hipótese inesperada.'
Complete-Exam $crp
$crp.GetType().GetMethod('RecordMeasurements', $instance).Invoke($crp, [object[]](,$evidenceMeasurements))
$crp.GetType().GetMethod('RecordFindings', $instance).Invoke($crp, @('PCR: 8 mg/L'))
$single.SetValue($crp.PSObject.BaseObject, 0)
Assert (!(Invoke-Method $confirmed 'Assess' @($rules, $flu, $single, [Random]::new(791)))) 'Evidência alterou uma hipótese já correta.'
Assert (Property $confirmed 'HasConfirmingExam') 'Hipótese correta com exame informativo não encerrou a investigação.'

# A numeric result is assessed through the configured exclusion rules, not through legacy text.
$gpa = @($cases | Where-Object { (Property $_ 'Id') -eq 'gpa-pulmonary-renal' })[0].PSObject.BaseObject
$porphyria = @($cases | Where-Object { (Property $_ 'Id') -eq 'acute-intermittent-porphyria' })[0].PSObject.BaseObject
Assert ((Invoke-Method $rules 'ExamPriority' @($flu, 'urine-culture', [int]1)) -gt 0) 'Numeric exclusion criteria did not prioritize urine culture.'
Assert ((Invoke-Method $rules 'ExamPriority' @($gpa, 'electrolytes', [int]1)) -gt 0) 'Combined criteria did not prioritize sodium.'
Assert ((Invoke-Method $rules 'ExamPriority' @($porphyria, 'urine-pbg', [int]1)) -gt 0) 'Configured porphobilinogen ranges did not prioritize the examination.'
$numericState = [Activator]::CreateInstance($stateType, $true)
$stateType.GetProperty('HasAssessment', $instance).SetValue($numericState, $true)
$stateType.GetProperty('Hypothesis', $instance).SetValue($numericState, $flu)
$numericExam = New-Exam 'urine-culture'
[void](Invoke-Method $numericExam 'TryRequest' @([int]1))
Complete-Exam $numericExam
$measurement = [Activator]::CreateInstance($savedMeasurementType)
$measurement.Id = 'urine-culture-count'
$measurement.Value = 150000
$measurements = [Array]::CreateInstance($savedMeasurementType, 1)
$measurements[0] = $measurement
$numericExam.GetType().GetMethod('RecordMeasurements', $instance).Invoke($numericExam, [object[]](,$measurements))
$numericExam.GetType().GetMethod('RecordFindings', $instance).Invoke($numericExam, @('150000'))
$numericArray = [Array]::CreateInstance($examType, 2)
$numericArray.SetValue($numericExam.PSObject.BaseObject, 0)
Assert (!(Invoke-Method $numericState 'Assess' @($rules, $gpa, ([Array]::CreateInstance($examType, 0)), [Random]::new(99)))) 'Empty evidence changed the hypothesis.'
$singleNumeric = [Array]::CreateInstance($examType, 1)
$singleNumeric.SetValue($numericExam.PSObject.BaseObject, 0)
Assert (!(Invoke-Method $numericState 'Assess' @($rules, $gpa, $singleNumeric, [Random]::new(99)))) 'Partial numeric evidence changed the hypothesis.'
$urineExam = New-Exam 'urinalysis'
[void](Invoke-Method $urineExam 'TryRequest' @([int]1))
Complete-Exam $urineExam
$urineMeasurement = [Activator]::CreateInstance($savedMeasurementType)
$urineMeasurement.Id = 'urine-leukocytes'
$urineMeasurement.Value = 150
$urineValues = [Array]::CreateInstance($savedMeasurementType, 1)
$urineValues[0] = $urineMeasurement
$urineExam.GetType().GetMethod('RecordMeasurements', $instance).Invoke($urineExam, [object[]](,$urineValues))
$urineExam.GetType().GetMethod('RecordFindings', $instance).Invoke($urineExam, @('150'))
$numericArray.SetValue($urineExam.PSObject.BaseObject, 1)
Assert (Invoke-Method $numericState 'Assess' @($rules, $gpa, $numericArray, [Random]::new(100))) 'Numeric evidence did not revise an excluded hypothesis.'
Assert ((Property $numericState 'Hypothesis') -ne $flu) 'Doctor retained a numerically excluded hypothesis.'
Assert (!(Property $numericState 'HasConfirmingExam')) 'Compatible numeric findings must not automatically confirm a disease.'
Assert (!(Invoke-Method $numericState 'Assess' @($rules, $gpa, $numericArray, [Random]::new(101)))) 'The same numeric sample was reviewed twice.'

$data = Get-Content (Join-Path $repositoryRoot 'lumen-sante/Diseases/flu-like.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$data.Id = 'worker-isolated-test'
$data.Tags = @('isolated-test')
$data.SelectionWeight = 1
$reader = $assembly.GetType('lumen_sante.Clinical.DiseaseJsonReader').GetMethod('Read', $static)
$extra = $reader.Invoke($null, @([string]($data | ConvertTo-Json -Depth 35)))
$expanded = [Array]::CreateInstance($extra.GetType(), $cases.Count + 1)
[Array]::Copy($cases, $expanded, $cases.Count)
$expanded.SetValue($extra, $cases.Count)
$expandedRules = New-Rules $expanded
$openDiagnosis = $false
for ($seed = 0; $seed -lt 1000; $seed++) {
    $result = $choose.Invoke($expandedRules, @($extra, [Random]::new($seed), [double]0))
    Assert ($null -eq $result -or $result -eq $extra) 'Doença externa recebeu alternativa sem tags compartilhadas.'
    if ($null -eq $result) { $openDiagnosis = $true }
}
Assert $openDiagnosis 'Erro sem alternativa plausível não deixou o diagnóstico em aberto.'

$reservations = [Activator]::CreateInstance($assembly.GetType('lumen_sante.Gameplay.WorkerReservations'), $true)
Assert (Invoke-Method $reservations 'TryAcquire' @('doctor-1', [string[]]@('patient:a', 'station:0'))) 'Primeira reserva falhou.'
Assert (!(Invoke-Method $reservations 'TryAcquire' @('doctor-2', [string[]]@('station:1', 'patient:a')))) 'Paciente reservado por dois Doctors.'
Assert (!(Invoke-Method $reservations 'IsReserved' @('station:1'))) 'Reserva rejeitada deixou aquisição parcial.'
Assert (!(Invoke-Method $reservations 'TryAcquire' @('doctor-2', [string[]]@('patient:b', 'station:0')))) 'Computador reservado por dois Doctors.'
Assert (!(Invoke-Method $reservations 'IsReserved' @('patient:b'))) 'Conflito de estação deixou paciente bloqueado.'
[void](Invoke-Method $reservations 'Release' @('doctor-3'))
Assert (Invoke-Method $reservations 'IsReserved' @('patient:a')) 'Profissional diferente liberou uma reserva.'
[void](Invoke-Method $reservations 'Release' @('doctor-1'))
[void](Invoke-Method $reservations 'Release' @('doctor-1'))
Assert (Invoke-Method $reservations 'TryAcquire' @('doctor-2', [string[]]@('patient:a', 'station:0'))) 'Recursos não foram liberados após cancelamento.'
Write-Output "$tested doenças em 1000 sementes cada, tags, caso externo, estabilidade de hipóteses, evidência única e reservas atômicas validados."
