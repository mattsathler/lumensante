namespace lumen_sante.Clinical
{
    internal sealed class ExamReport
    {
        internal ExamReport(ExamOrders admission, ExamOrder exam)
        {
            Findings = ExamFindings.Describe(admission, exam);
            Protocol = admission.AdmissionId.ToString("N").Substring(0, 8).ToUpperInvariant() + " / " +
                exam.Id.ToUpperInvariant();

            switch (exam.Kind)
            {
                case ExamKind.Mri:
                    Method = exam.Name;
                    Unit = "Diagnóstico por Imagem";
                    Physician = "Dra. Maya Chen";
                    Sample = "Imagens digitais";
                    break;
                case ExamKind.XRay:
                    Method = exam.Name;
                    Unit = "Diagnóstico por Imagem";
                    Physician = "Dra. Maya Chen";
                    Sample = "Imagens digitais";
                    break;
                case ExamKind.Bedside:
                    Method = "Eletrocardiograma de 12 derivações";
                    Unit = exam.Id == "vital-signs" ? "Avaliação clínica" : "Unidade de Cardiologia";
                    Physician = "Dr. Adrian Brooks";
                    Sample = "Traçado digital";
                    break;
                default:
                    Method = exam.Id == "hematology" ? "Hematologia automatizada"
                        : exam.Id == "electrolytes" ? "Eletrodo íon-seletivo" : "Bioquímica automatizada";
                    Unit = "Laboratório Central";
                    Physician = "Dra. Elena Navarro";
                    Sample = exam.Id == "hematology" ? "Sangue total" : "Soro";
                    switch (exam.Id)
                    {
                        case "blood-gas": Method = "Análise de gases sanguíneos"; Sample = "Sangue arterial"; break;
                        case "coagulation": Method = "Coagulometria"; Sample = "Plasma citratado"; break;
                        case "crp": Method = "Imunoturbidimetria"; break;
                        case "urinalysis": Method = "Análise físico-química e microscópica"; Sample = "Urina"; break;
                    }
                    break;
            }
            Method = exam.Method ?? Method;
            Sample = exam.Sample ?? Sample;
            Physician = exam.RequestedBy ?? Physician;
            PerformedBy = exam.PerformedBy ?? "Equipe de enfermagem";
        }

        internal string Findings { get; }
        internal string Protocol { get; }
        internal string Method { get; }
        internal string Unit { get; }
        internal string Physician { get; private set; }
        internal string PerformedBy { get; }
        internal string Sample { get; }
    }
}
