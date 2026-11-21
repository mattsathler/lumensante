using System;

namespace lumen_sante.Clinical
{
    internal enum ExamStatus
    {
        Available,
        Pending,
        Completed
    }

    internal sealed class ExamOrders
    {
        internal ExamOrders(string patientName, string room, string diseaseId, PatientProfile profile)
        {
            AdmissionId = Guid.NewGuid();
            PatientName = patientName;
            Room = room;
            DiseaseId = diseaseId;
            Profile = profile;
        }

        internal Guid AdmissionId { get; }
        internal string PatientName { get; }
        internal string Room { get; private set; }
        internal bool IsInpatient { get; private set; }
        internal void AdmitTo(string room)
        {
            Room = room;
            IsInpatient = true;
        }
        internal string DiseaseId { get; }
        internal PatientProfile Profile { get; }
        internal string Notes { get; set; } = string.Empty;

        internal ExamOrder[] Items { get; } =
        {
            new ExamOrder("mri", "Ressonância magnética (MRI)", 60000),
            new ExamOrder("x-ray", "Radiografia (RX)", 45000),
            new ExamOrder("cardiac", "Exames cardíacos", 45000),
            new ExamOrder("hematology", "Painel hematológico", 30000),
            new ExamOrder("renal", "Função renal", 30000),
            new ExamOrder("hepatic", "Função hepática", 30000),
            new ExamOrder("electrolytes", "Eletrólitos", 30000)
        };
    }

    internal sealed class ExamOrder
    {
        private readonly int delayMilliseconds;
        private long readyAtMilliseconds;

        internal ExamOrder(string id, string name, int delayMilliseconds)
        {
            Id = id;
            Name = name;
            this.delayMilliseconds = delayMilliseconds;
        }

        internal string Id { get; }
        internal string Name { get; }
        internal ExamStatus Status { get; private set; }
        internal int StageAtRequest { get; private set; }
        internal long CompletedAtMilliseconds { get; private set; }

        internal bool TryRequest(long elapsedMilliseconds, int stage)
        {
            if (Status != ExamStatus.Available)
            {
                return false;
            }

            StageAtRequest = stage;
            readyAtMilliseconds = elapsedMilliseconds + delayMilliseconds;
            Status = ExamStatus.Pending;
            return true;
        }

        internal bool TryComplete(long elapsedMilliseconds)
        {
            if (Status != ExamStatus.Pending || elapsedMilliseconds < readyAtMilliseconds)
            {
                return false;
            }

            CompletedAtMilliseconds = elapsedMilliseconds;
            Status = ExamStatus.Completed;
            return true;
        }
    }
}
