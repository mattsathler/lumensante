#Requires -Version 5.1
[CmdletBinding()]
param([string]$AssemblyPath)

$ErrorActionPreference = 'Stop'
if (!$AssemblyPath) { $AssemblyPath = Join-Path $PSScriptRoot '../../../lumen-sante/bin/Debug/lumen-sante.dll' }
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[void][Reflection.Assembly]::LoadFrom((Join-Path $repositoryRoot 'Dependencies/RagePluginHookSDK.dll'))
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$instance = [Reflection.BindingFlags]'Instance,NonPublic'
$static = [Reflection.BindingFlags]'Static,NonPublic'
$expectedIds = @(
    'community-pneumonia',
    'copd-exacerbation',
    'decompensated-heart-failure',
    'acute-myocardial-infarction',
    'unstable-angina',
    'rapid-atrial-fibrillation',
    'hypertensive-emergency',
    'acute-pulmonary-embolism',
    'extensive-proximal-dvt',
    'acute-ischemic-stroke',
    'intracerebral-hemorrhage',
    'recurrent-epileptic-seizures',
    'bacterial-meningitis',
    'complicated-pyelonephritis',
    'obstructing-ureteral-stone',
    'acute-kidney-injury',
    'decompensated-chronic-kidney-disease',
    'diabetic-ketoacidosis',
    'hyperosmolar-hyperglycemic-state',
    'severe-diabetic-hypoglycemia',
    'symptomatic-hyponatremia',
    'severe-hyperkalemia',
    'acute-pancreatitis',
    'acute-cholecystitis',
    'acute-cholangitis',
    'acute-appendicitis',
    'complicated-diverticulitis',
    'adhesive-bowel-obstruction',
    'bleeding-peptic-ulcer',
    'decompensated-cirrhosis',
    'severe-alcohol-associated-hepatitis',
    'severe-acute-hepatitis-b',
    'crohn-severe-flare',
    'acute-severe-ulcerative-colitis',
    'severe-c-difficile-colitis',
    'severe-cellulitis',
    'septic-knee-arthritis',
    'tibial-osteomyelitis',
    'infected-diabetic-foot',
    'severe-iron-deficiency-anemia',
    'severe-b12-deficiency-anemia',
    'sickle-cell-vaso-occlusive-crisis',
    'severe-asthma-exacerbation',
    'pericarditis-with-effusion',
    'spontaneous-pneumothorax',
    'guillain-barre-syndrome',
    'myasthenic-crisis',
    'immune-ttp',
    'acute-intermittent-porphyria',
    'gpa-pulmonary-renal'
)
$catalog = $assembly.GetType('lumen_sante.Clinical.DiseaseCatalog')
$all = $catalog.GetProperty('All', $static).GetValue($null)
$diagnostics = $catalog.GetProperty('LoadDiagnostics', $static).GetValue($null)
$failures = @($diagnostics | Where-Object { $_ -match 'rejeitad|inválid|erro|falha' })
if ($failures.Count) { throw ($failures -join [Environment]::NewLine) }
function Property($object, $name) { $object.GetType().GetProperty($name, $instance).GetValue($object) }
$newCases = @($all | Where-Object { (Property $_ 'Id') -in $expectedIds })
if ($newCases.Count -ne 50) { throw "Esperados 50 casos novos carregados, encontrados $($newCases.Count)." }
$allInpatients = @($all | Where-Object { (Property $_ 'Tags') -contains 'internacao' })
if ($allInpatients.Count -ne 50) { throw 'Grupo de internação com quantidade inesperada.' }
$medicationType = $assembly.GetType('lumen_sante.Clinical.MedicationClass')
$emptyMedications = [Array]::CreateInstance($medicationType, 0)
$evolve = $assembly.GetType('lumen_sante.Clinical.DiseaseDefinition').GetMethod('Evolve', $instance)
$answers = 0
$rare = 0
$weight = 0
foreach ($case in $newCases) {
    $id = Property $case 'Id'
    $sourcePath = Join-Path $repositoryRoot ("lumen-sante/Diseases/" + $id + '.json')
    $json = [IO.File]::ReadAllText($sourcePath) | ConvertFrom-Json
    if (!(Property $case 'RequiresSpecificTreatment') -or [string]::IsNullOrWhiteSpace((Property $case 'TreatmentName'))) {
        throw "Caso sem tratamento específico: $id"
    }
    $tags = Property $case 'Tags'
    $caseWeight = Property $case 'SelectionWeight'
    if ($tags -contains 'rara') {
        $rare++
        if ($caseWeight -ne 1) { throw "Peso raro incorreto: $id" }
    } elseif ($tags -notcontains 'frequente' -or $caseWeight -notin 8,12) {
        throw "Grupo frequente inválido: $id"
    }
    $weight += $caseWeight
    if ($json.Questions.Count -ne 2) { throw "Número inesperado de perguntas: $id" }
    foreach ($question in $json.Questions) {
        if ($question.AnswersByStage.Count -ne 9) { throw "Bancos incompletos: $id" }
        foreach ($bank in $question.AnswersByStage) {
            if ($bank.Count -ne 20 -or @($bank | Sort-Object -Unique).Count -ne 20) {
                throw "Banco sem 20 respostas únicas: $id"
            }
            $answers += $bank.Count
        }
    }
    $find = $case.GetType().GetMethod('FindExamResult', $instance)
    foreach ($stage in 1..9) {
        $results = @($json.Exams.PSObject.Properties | Where-Object {
            ![string]::IsNullOrWhiteSpace($find.Invoke($case, @([string]$_.Name, [int]$stage)))
        })
        if (!$results.Count) { throw "Estágio sem laudo informativo: $id, $stage" }
    }
    foreach ($exam in $json.Exams.PSObject.Properties) {
        if ($null -ne $find.Invoke($case, @([string]$exam.Name, [int]10))) {
            throw "Laudo no estágio terminal deveria estar indisponível: $id"
        }
    }
    # Test the actual evolution method, including every supported medication class.
    $allMedications = [Enum]::GetValues($medicationType)
    $canRecover = $false
    foreach ($seed in 0..99) {
        if ($evolve.Invoke($case, @([int]1, $true, $emptyMedications, [Random]::new($seed))) -eq 0) { $canRecover = $true }
        foreach ($medication in $allMedications) {
            $active = [Array]::CreateInstance($medicationType, 1)
            $active.SetValue($medication, 0)
            if ($evolve.Invoke($case, @([int]1, $false, $active, [Random]::new($seed))) -eq 0) {
                throw "Medicação isolada recuperou caso que exige tratamento: $id, $medication"
            }
        }
        foreach ($risk in 0..10) {
            $next = $evolve.Invoke($case, @([int]$risk, $true, $emptyMedications, [Random]::new($seed)))
            if ($next -lt 0 -or $next -gt 10 -or [Math]::Abs($next - $risk) -gt 1 -or
                (($risk -eq 0 -or $risk -eq 10) -and $next -ne $risk)) { throw "Transição inválida: $id, $risk" }
        }
        if ($evolve.Invoke($case, @([int]1, $false, $emptyMedications, [Random]::new($seed))) -eq 0) {
            throw "Recuperação sem tratamento: $id"
        }
    }
    if (!$canRecover) { throw "Tratamento correto nunca recupera o caso: $id" }
}
if ($rare -ne 5 -or $weight -ne 405 -or $answers -ne 18000) { throw 'Distribuição, pesos ou total de falas inesperados.' }
Write-Output '50 casos de internação validados: 45 frequentes, 5 raros, peso 405, 18.000 falas, laudos e evolução em 100 sementes.'
