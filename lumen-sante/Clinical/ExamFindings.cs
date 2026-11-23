namespace lumen_sante.Clinical
{
    internal static class ExamFindings
    {
        internal static string Describe(ExamOrders admission, ExamOrder exam) =>
            exam.SavedFindings ?? DiseaseCatalog.Find(admission.DiseaseId)?.FindExamResult(exam.Id, exam.StageAtRequest)
            ?? "Exame inconclusivo";
    }
}
