namespace lumen_sante.Clinical
{
    internal static class ExamFindings
    {
        internal static string Describe(ExamOrders admission, ExamOrder exam)
        {
            if (admission.DiseaseId == "flu-like")
            {
                bool complicated = exam.StageAtRequest >= 6;
                switch (exam.Id)
                {
                    case "x-ray": return complicated
                        ? "Opacidades pulmonares difusas, a correlacionar com o quadro respiratório."
                        : "Sem opacidades focais ou derrame pleural identificados.";
                    case "mri": return "Sem lesão torácica focal identificada nesta avaliação.";
                    case "mri-brain": return "Sem lesões focais intracranianas identificadas.";
                    case "mri-spine": return "Sem alterações focais ou compressão medular identificadas.";
                    case "cardiac": return complicated ? "Ritmo sinusal acelerado." : "Ritmo sinusal sem alterações relevantes.";
                    case "hematology": return "Hemoglobina preservada; alterações leucocitárias inespecíficas.";
                    case "renal": return "Marcadores renais dentro dos intervalos de referência.";
                    case "hepatic": return "Marcadores hepáticos sem alterações relevantes.";
                    case "electrolytes": return "Eletrólitos dentro dos intervalos de referência.";
                    case "blood-gas": return complicated ? "Oxigenação arterial reduzida."
                        : "Sem alteração relevante da oxigenação arterial.";
                    case "crp": return "Proteína C reativa discretamente elevada, achado inespecífico.";
                    case "coagulation": return "Tempos de coagulação dentro dos intervalos de referência.";
                    case "glucose": return "Glicemia dentro do intervalo de referência.";
                    case "urinalysis": return "Sem alterações relevantes na análise de urina.";
                    default: return exam.NormalFindings ?? "Sem alterações relevantes identificadas nesta avaliação.";
                }
            }
            if (admission.DiseaseId != "lung-cancer")
            {
                string symptomatic = DescribeSymptomatic(admission.DiseaseId, exam);
                if (symptomatic != null) return symptomatic;
                return exam.NormalFindings ?? "Sem achados específicos registrados neste exame.";
            }

            bool advanced = exam.StageAtRequest >= 6;
            switch (exam.Id)
            {
                case "x-ray":
                    return advanced
                        ? "Opacidade pulmonar irregular, com perda de volume na região adjacente."
                        : "Opacidade pulmonar focal de aspecto indeterminado.";
                case "mri":
                    return advanced
                        ? "Lesão torácica irregular em contato com estruturas adjacentes."
                        : "Alteração focal de sinal no tórax, de aspecto inespecífico.";
                case "mri-brain":
                    return "Sem lesões focais intracranianas identificadas nesta avaliação.";
                case "mri-spine":
                    return "Sem alterações focais ou compressão medular identificadas.";
                case "x-ray-ribs":
                    return "Arcos costais sem fraturas identificadas.";
                case "x-ray-pelvis":
                    return "Estruturas ósseas da pelve sem alterações focais identificadas.";
                case "x-ray-limbs":
                    return "Segmentos avaliados sem fraturas ou alterações ósseas focais.";
                case "blood-gas":
                    return advanced ? "Pressão parcial de oxigênio reduzida na amostra arterial."
                        : "Oxigenação e equilíbrio ácido-base sem alterações relevantes.";
                case "coagulation":
                    return "Tempos de coagulação dentro dos intervalos de referência.";
                case "glucose":
                    return "Glicemia dentro do intervalo de referência.";
                case "crp":
                    return advanced ? "Proteína C reativa elevada, achado inespecífico."
                        : "Proteína C reativa sem elevação relevante.";
                case "urinalysis":
                    return "Sem alterações relevantes na análise de urina.";
                case "cardiac":
                    return advanced
                        ? "Ritmo sinusal acelerado durante o registro."
                        : "Ritmo sinusal, sem alteração relevante no registro.";
                case "hematology":
                    return advanced
                        ? "Hemoglobina abaixo do intervalo de referência."
                        : "Sem alterações relevantes no painel hematológico.";
                case "renal":
                    return "Marcadores de função renal dentro dos intervalos de referência.";
                case "hepatic":
                    return "Marcadores hepáticos sem alterações relevantes.";
                case "electrolytes":
                    return "Eletrólitos dentro dos intervalos de referência.";
                default:
                    return exam.NormalFindings ?? "Sem achados específicos registrados neste exame.";
            }
        }

        private static string DescribeSymptomatic(string diseaseId, ExamOrder exam)
        {
            bool advanced = exam.StageAtRequest >= 6;
            switch (diseaseId)
            {
                case "bacterial-cystitis":
                    if (exam.Id == "urinalysis") return "Leucócitos e bactérias presentes na amostra de urina.";
                    if (exam.Id == "urine-culture") return "Crescimento bacteriano na amostra; correlacionar com os sintomas e a coleta.";
                    if (exam.Id == "hematology") return advanced ? "Leucocitose com predomínio de neutrófilos." : "Alterações leucocitárias discretas e inespecíficas.";
                    if (exam.Id == "crp") return advanced ? "Proteína C reativa elevada." : "Proteína C reativa discretamente elevada.";
                    break;
                case "acute-gastroenteritis":
                    if (exam.Id == "electrolytes") return advanced ? "Desequilíbrio hidroeletrolítico na amostra." : "Eletrólitos sem alteração relevante nesta coleta.";
                    if (advanced && (exam.Id == "renal" || exam.Id == "urea")) return "Elevação dos marcadores nitrogenados, a correlacionar com hidratação.";
                    break;
                case "acute-bronchospasm":
                    if (exam.Id == "blood-gas") return advanced ? "Oxigenação arterial reduzida, com comprometimento ventilatório." : "Oxigenação arterial discretamente reduzida.";
                    if (exam.Id == "x-ray") return "Sem opacidade pulmonar focal ou derrame pleural identificados.";
                    if (exam.Id == "cardiac") return advanced ? "Ritmo sinusal acelerado." : "Ritmo sinusal sem alterações relevantes.";
                    break;
                case "viral-pharyngitis":
                case "acute-sinusitis":
                    if (exam.Id == "crp") return "Elevação discreta de proteína C reativa, achado inespecífico.";
                    if (exam.Id == "hematology") return "Alterações leucocitárias inespecíficas, sem definição da causa neste exame.";
                    break;
                case "allergic-rhinitis":
                case "tension-headache":
                case "acid-reflux":
                case "contact-dermatitis":
                case "muscle-strain":
                    break;
                default:
                    return null;
            }
            return exam.NormalFindings ?? "Sem alterações relevantes identificadas nesta avaliação.";
        }
    }
}
