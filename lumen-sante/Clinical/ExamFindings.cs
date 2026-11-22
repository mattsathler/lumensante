namespace lumen_sante.Clinical
{
    internal static class ExamFindings
    {
        internal static string Describe(ExamOrders admission, ExamOrder exam)
        {
            if (admission.DiseaseId != "lung-cancer")
            {
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
    }
}
