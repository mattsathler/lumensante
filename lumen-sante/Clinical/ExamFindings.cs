namespace lumen_sante.Clinical
{
    internal static class ExamFindings
    {
        internal static string Describe(ExamOrders admission, ExamOrder exam)
        {
            if (admission.DiseaseId != "lung-cancer")
            {
                return "Sem achados específicos registrados neste exame.";
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
                    return "Sem achados específicos registrados neste exame.";
            }
        }
    }
}
