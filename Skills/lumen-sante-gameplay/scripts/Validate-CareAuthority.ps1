#Requires -Version 5.1
[CmdletBinding()]
param([string]$AssemblyPath)

$ErrorActionPreference = 'Stop'
if (!$AssemblyPath) { $AssemblyPath = Join-Path $PSScriptRoot '../../../lumen-sante/bin/Debug/lumen-sante.dll' }
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repositoryRoot 'Dependencies/RagePluginHookSDK.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$ordersType = $assembly.GetType('lumen_sante.Clinical.ExamOrders')
$authorType = $assembly.GetType('lumen_sante.Clinical.CareAuthor')
$medicationType = $assembly.GetType('lumen_sante.Clinical.MedicationClass')
$player = $authorType.GetField('Player', $static).GetValue($null)
$doctor = $authorType.GetMethod('Doctor', $static).Invoke($null, @('doctor-1', 'Dra. Ana'))
$medication = [Enum]::Parse($medicationType, 'Analgesic')
function Property($object, $name) { $object.GetType().GetProperty($name, $instance).GetValue($object) }
function New-Orders { $ordersType.GetConstructors($instance)[0].Invoke(@('Paciente', 'Quarto', 'lung-cancer', $null)) }
function Invoke-Care($orders, $name, [object[]]$arguments) { $ordersType.GetMethod($name, $instance).Invoke($orders, $arguments) }
function Assert($condition, $message) { if (!$condition) { throw $message } }

$orders = New-Orders
Assert (!(Property $orders 'IsCareProtected')) 'Prontuário novo protegido antes da atuação do jogador.'
Assert (Invoke-Care $orders 'PrescribeMedication' @($medication, [long]1000, $doctor, [long]1, [long]0)) 'Doctor não conseguiu prescrever.'
$revision = [long](Property $orders 'CareRevision')
Assert (!(Invoke-Care $orders 'StopMedication' @($medication, $doctor, [long]2, [long]0))) 'Plano antigo foi aplicado.'
Assert ((Property $orders 'CareRevision') -eq $revision) 'Ação rejeitada alterou a revisão.'
Assert (Invoke-Care $orders 'StopMedication' @($medication, $player, [long]3, $revision)) 'Jogador não conseguiu retirar medicação.'
Assert (Property $orders 'IsCareProtected') 'Retirada do jogador não protegeu o plano.'
$revision = [long](Property $orders 'CareRevision')
Assert (!(Invoke-Care $orders 'PrescribeMedication' @($medication, [long]2000, $doctor, [long]4, $revision))) 'Doctor reintroduziu medicação retirada.'
Assert (!(Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $doctor, [long]4, $revision))) 'Doctor alterou tratamento protegido.'
Assert (!(Invoke-Care $orders 'Discharge' @([long]4, $doctor, $revision))) 'Doctor deu alta em plano protegido.'
Assert (Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $player, [long]5, $revision)) 'Jogador não conseguiu alterar seu plano.'
$revision = [long](Property $orders 'CareRevision')
Assert (Invoke-Care $orders 'Discharge' @([long]6, $player, $revision)) 'Alta do jogador bloqueada.'
Invoke-Care $orders 'Readmit' @('Consulta')
Assert (Property $orders 'IsCareProtected') 'Retorno apagou a proteção do episódio.'
Assert (!(Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $doctor, [long]7, [long](Property $orders 'CareRevision')))) 'Doctor alterou plano após retorno.'

$orders = New-Orders
Assert (Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $doctor, [long]1, [long]0)) 'Tratamento inicial do Doctor falhou.'
Assert (Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $player, [long]2, [long](Property $orders 'CareRevision'))) 'Confirmação do mesmo tratamento não transferiu a autoridade.'
Assert (Property $orders 'IsCareProtected') 'Tratamento confirmado pelo jogador sem proteção.'
Assert (!(Invoke-Care $orders 'ClearTreatment' @($doctor, [long]3, [long](Property $orders 'CareRevision')))) 'Doctor retirou tratamento protegido.'
[void](Invoke-Care $orders 'RecordCoronerRequest' @($doctor, [long]3))
Assert (Property $orders 'IsCareProtected') 'Registro de Coroners removeu proteção clínica.'
$history = Property $orders 'CareHistory'
Assert ($history.Count -eq 3) 'Histórico não preservou as condutas e a solicitação de Coroners.'
Assert (!(Property (Property $history[0] 'Author') 'IsPlayer')) 'Autoria do Doctor incorreta.'
Assert (Property (Property $history[1] 'Author') 'IsPlayer') 'Autoria do jogador incorreta.'
Assert ((Property $history[1] 'At') -eq 2) 'Horário não registrado.'

$orders = New-Orders
$revision = [long](Property $orders 'CareRevision')
Invoke-Care $orders 'RegisterPlayerInteraction' @([long]1)
Assert (!(Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $doctor, [long]2, $revision))) 'Interação do jogador não invalidou ação pendente.'
Assert (!(Property $orders 'IsCareProtected')) 'Avaliação isolada protegeu plano ainda não definido.'

$orders = New-Orders
Assert (Invoke-Care $orders 'PrescribeMedication' @($medication, [long]10, $player, [long]1, [long]0)) 'Prescrição do jogador falhou.'
Assert (@(Invoke-Care $orders 'ActiveMedications' @([long]11)).Count -eq 0) 'Proteção impediu expiração da medicação.'
Assert (Property $orders 'IsCareProtected') 'Expiração removeu a proteção do plano.'

$orders = New-Orders
Assert (Invoke-Care $orders 'Discharge' @([long]1, $doctor, [long]0)) 'Alta autônoma bloqueada em plano livre.'
Assert (!(Invoke-Care $orders 'Discharge' @([long]2, $doctor, [long](Property $orders 'CareRevision')))) 'Alta duplicada aceita.'
Write-Output 'Autoria, revisão, proteção do jogador, confirmação de tratamento, retirada, expiração, retorno e alta validados.'

$orders = New-Orders
$ordersType.GetProperty('Notes', $instance).SetValue($orders, 'Anotacao mantida no historico')
$identity = Property $orders 'AdmissionId'
$exam = (Property $orders 'Items')[0]
[void]$exam.GetType().GetMethod('TryRequest', $instance).Invoke($exam, @([int]1))
$statusType = $assembly.GetType('lumen_sante.Clinical.ExamStatus')
[void]$exam.GetType().GetMethod('TrySetStatus', $instance).Invoke($exam, @([Enum]::Parse($statusType, 'InProgress')))
[void]$exam.GetType().GetMethod('TryComplete', $instance).Invoke($exam, @([long]100))
Assert (Invoke-Care $orders 'SelectTreatment' @('lung-cancer', $doctor, [long]1, [long]0)) 'Tratamento do histórico não foi registrado.'
Assert (Invoke-Care $orders 'Discharge' @([long]2, $doctor, [long](Property $orders 'CareRevision'))) 'Alta do histórico falhou.'
Assert ((Property $orders 'Notes') -eq 'Anotacao mantida no historico') 'Alta apagou anotações.'
Assert ((Property $orders 'TreatmentDiseaseId') -eq 'lung-cancer') 'Alta apagou tratamento.'
Assert ((Property $orders 'CareHistory').Count -eq 2) 'Alta apagou histórico de condutas.'
Assert ((Property $exam 'Status').ToString() -eq 'Completed') 'Alta apagou resultado concluído.'
Invoke-Care $orders 'Readmit' @('Recepção')
Invoke-Care $orders 'Readmit' @('Consulta')
Assert ((Property $orders 'AdmissionId') -eq $identity) 'Retorno criou outra identidade de prontuário.'
Assert ((Property $orders 'VisitCount') -eq 2) 'Retorno duplicou a contagem de atendimentos.'
Write-Output 'Histórico em memória preserva identidade, anotações, exames, tratamento e condutas após alta e retorno.'

$orders = New-Orders
$exam = (Property $orders 'Items')[0]
$examType = $exam.GetType()
Assert ($examType.GetMethod('TryRequestByNamedWorker', $instance).Invoke($exam, @([int]2, $doctor))) 'Pedido do Doctor identificado falhou.'
Assert ((Property $exam 'RequestedBy') -eq 'Dra. Ana') 'Nome do Doctor ausente no pedido.'
$statusType = $assembly.GetType('lumen_sante.Clinical.ExamStatus')
[void]$examType.GetMethod('TrySetStatus', $instance).Invoke($exam, @([Enum]::Parse($statusType, 'InProgress')))
Assert ($examType.GetMethod('TryComplete', $instance).Invoke($exam, @([long]100))) 'Exame identificado não terminou.'
[void]$examType.GetMethod('RecordPerformer', $instance).Invoke($exam, @('Enf. Beatriz Silva'))
Assert ((Property $exam 'PerformedBy') -eq 'Enf. Beatriz Silva') 'Nome da Nurse ausente no exame.'
$nurse = $authorType.GetMethod('Nurse', $static).Invoke($null, @('nurse-1', 'Enf. Beatriz Silva'))
Invoke-Care $orders 'RecordNurseProcedure' @($nurse, [long]101, 'Visita de tratamento')
Assert ((Property $orders 'CareHistory').Count -eq 1) 'Visita de enfermagem não entrou no histórico.'
$procedureHistory = Property $orders 'CareHistory'
Assert ((Property (Property $procedureHistory[0] 'Author') 'Name') -eq 'Enf. Beatriz Silva') 'Visita sem autoria da Nurse.'
Assert (!(Property $orders 'IsCareProtected')) 'Visita de enfermagem protegeu o plano do jogador.'
Write-Output 'Nomes de Doctor e Nurse preservados no exame e na visita de tratamento.'

$catalogType = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog')
$disease = $catalogType.GetMethod('Find', $static).Invoke($null, @('flu-like'))
$orders = New-Orders
$random = [Random]::new(24)
Invoke-Care $orders 'RecordInitialComplaint' @($disease, [int]3, $random)
Assert (!(Property $orders 'HasStartedConversation')) 'Queixa inicial liberou a árvore de conversa.'
Assert ((Property $orders 'SymptomRevision') -eq 1) 'Queixa inicial não entrou no prontuário.'
$complaints = @(Invoke-Care $orders 'SymptomHistory' @($disease))
Assert ($complaints.Count -eq 1) 'Histórico inicial não contém exatamente uma queixa.'
$answer = Invoke-Care $orders 'GetSymptomAnswer' @($disease, [int]0, [int]3, $random)
Assert ($answer -eq $complaints[0].Item2) 'Conversa não reutilizou a queixa inicial.'
Assert (Property $orders 'HasStartedConversation') 'Pergunta inicial não liberou a conversa.'
Assert ((Property $orders 'SymptomRevision') -eq 1) 'Repetição da queixa duplicou o histórico.'
Write-Output 'Queixa inicial usa o estágio recebido, permanece estável e preserva a conversa em árvore.'
