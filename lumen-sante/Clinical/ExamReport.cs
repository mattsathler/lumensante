namespace lumen_sante.Clinical
{
    internal sealed class ExamReport
    {
        internal ExamReport(ExamOrders admission, ExamOrder exam)
        {
            Findings = ExamFindings.Describe(admission, exam);
            Protocol = admission.AdmissionId.ToString("N").Substring(0, 8).ToUpperInvariant() + " / " +
                exam.Id.ToUpperInvariant();

            switch (exam.Id)
            {
                case "mri":
                    Method = "Ressonância magnética torácica";
                    Unit = "Diagnóstico por Imagem";
                    Physician = "Dra. Maya Chen";
                    Sample = "Imagens digitais";
                    break;
                case "x-ray":
                    Method = "Radiografia digital de tórax";
                    Unit = "Diagnóstico por Imagem";
                    Physician = "Dra. Maya Chen";
                    Sample = "Imagens digitais";
                    break;
                case "cardiac":
                    Method = "Eletrocardiograma de 12 derivações";
                    Unit = "Unidade de Cardiologia";
                    Physician = "Dr. Adrian Brooks";
                    Sample = "Traçado digital";
                    break;
                default:
                    Method = exam.Id == "hematology" ? "Hematologia automatizada"
                        : exam.Id == "electrolytes" ? "Eletrodo íon-seletivo" : "Bioquímica automatizada";
                    Unit = "Laboratório Central";
                    Physician = "Dra. Elena Navarro";
                    Sample = exam.Id == "hematology" ? "Sangue total" : "Soro";
                    break;
            }
        }

        internal string Findings { get; }
        internal string Protocol { get; }
        internal string Method { get; }
        internal string Unit { get; }
        internal string Physician { get; }
        internal string Sample { get; }
    }
}
