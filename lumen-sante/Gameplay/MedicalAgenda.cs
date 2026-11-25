using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using lumen_sante.Clinical;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    internal sealed class MedicalAgenda
    {
        internal enum Status { Scheduled, Ready, Arrived, InCare, Completed, Pending, Cancelled, Missed }

        public sealed class Appointment
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public Guid PatientId { get; set; }
            public long Due { get; set; }
            public string Name { get; set; }
            public bool Female { get; set; }
            public bool FollowUp { get; set; }
            public Status State { get; set; }
            internal DateTime Date => new DateTime(Due);
        }

        public sealed class Record
        {
            public ExamOrders.Snapshot Orders { get; set; }
            public int Risk { get; set; }
            public long NextEvolutionTicks { get; set; }
            public SavedVitals Vitals { get; set; }
            public uint Model { get; set; }
            public int[] Outfit { get; set; }
            public bool Deceased { get; set; }
        }

        public sealed class SavedVitals
        {
            public int Pulse { get; set; }
            public int Systolic { get; set; }
            public int Diastolic { get; set; }
            public int RespiratoryRate { get; set; }
            public int OxygenSaturation { get; set; }
            public double? Temperature { get; set; }
        }

        public sealed class Data
        {
            public int Version { get; set; } = 1;
            public long GeneratedThrough { get; set; }
            public List<Appointment> Appointments { get; set; } = new List<Appointment>();
            public List<Record> Records { get; set; } = new List<Record>();
        }

        private readonly string path;
        private readonly Action<string> log;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private Data data = new Data();
        private string saved;
        internal bool Available { get; private set; } = true;
        internal IReadOnlyList<Appointment> Appointments => data.Appointments;
        internal IReadOnlyList<Record> Records => data.Records;

        internal MedicalAgenda(string path, Action<string> log)
        {
            this.path = path;
            this.log = log;
            try
            {
                if (!File.Exists(path)) return;
                saved = File.ReadAllText(path, Encoding.UTF8);
                Data loaded = json.Deserialize<Data>(saved);
                if (loaded == null || loaded.Version != 1 || loaded.Appointments == null || loaded.Records == null ||
                    loaded.GeneratedThrough < 0 || loaded.GeneratedThrough > DateTime.MaxValue.Ticks ||
                    loaded.Appointments.Any(item => item == null || item.Id == Guid.Empty ||
                        item.Due <= 0 || item.Due > DateTime.MaxValue.Ticks || !Enum.IsDefined(typeof(Status), item.State) ||
                        string.IsNullOrWhiteSpace(item.Name)) ||
                    loaded.Appointments.Select(item => item.Id).Distinct().Count() != loaded.Appointments.Count ||
                    loaded.Records.Any(record => record?.Orders == null || !record.Orders.IsValid() ||
                        record.NextEvolutionTicks < 0 || record.NextEvolutionTicks > DateTime.MaxValue.Ticks) ||
                    loaded.Records.Select(item => item.Orders.Id).Distinct().Count() != loaded.Records.Count ||
                    loaded.Appointments.Any(item => (item.FollowUp || item.PatientId != Guid.Empty) &&
                        !loaded.Records.Any(record => record.Orders.Id == item.PatientId)))
                    throw new InvalidDataException("Formato da agenda inválido.");
                data = loaded;
                foreach (Appointment item in Appointments.Where(item => item.State == Status.Arrived))
                    item.State = Status.Scheduled;
            }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is UnauthorizedAccessException ||
                error is ArgumentException || error is InvalidOperationException || error is FormatException)
            {
                Available = false;
                log("Agenda indisponível: " + error.Message);
            }
        }

        internal static DateTime ReadGameDate()
        {
            int year = NativeFunction.Natives.GET_CLOCK_YEAR<int>();
            int month = NativeFunction.Natives.GET_CLOCK_MONTH<int>() + 1;
            int day = NativeFunction.Natives.GET_CLOCK_DAY_OF_MONTH<int>();
            int hour = NativeFunction.Natives.GET_CLOCK_HOURS<int>();
            int minute = NativeFunction.Natives.GET_CLOCK_MINUTES<int>();
            if (year < 1 || year > 9998 || month < 1 || month > 12 || day < 1 ||
                day > DateTime.DaysInMonth(year, month) || hour < 0 || hour > 23 || minute < 0 || minute > 59)
                return default(DateTime);
            int second = NativeFunction.Natives.GET_CLOCK_SECONDS<int>();
            if (second < 0 || second > 59) return default(DateTime);
            return new DateTime(year, month, day, hour, minute, second);
        }

        internal void Generate(DateTime now, Random random)
        {
            if (!Available || now == default(DateTime)) return;
            DateTime first = new DateTime(Math.Max(now.Date.Ticks, data.GeneratedThrough));
            for (DateTime day = first; day < now.Date.AddDays(7); day = day.AddDays(1))
            {
                for (int hour = 9; hour <= 17; hour += 2)
                {
                    DateTime slot = day.AddHours(hour);
                    if (slot <= now || SlotOccupied(slot)) continue;
                    bool female = random.Next(2) == 0;
                    data.Appointments.Add(new Appointment { Due = slot.Ticks, Female = female, Name = PatientNames.Pick(random, female) });
                }
                data.GeneratedThrough = day.AddDays(1).Ticks;
            }
        }

        private bool SlotOccupied(DateTime slot) => Appointments.Any(item =>
            item.Due == slot.Ticks && item.State != Status.Cancelled);

        internal Appointment ScheduleReturn(DateTime now, Record record)
        {
            if (!Available || now == default(DateTime)) return null;
            DateTime slot = now.Date.AddDays(1).AddHours(9);
            while (SlotOccupied(slot))
                slot = slot.Hour >= 17 ? slot.Date.AddDays(1).AddHours(9) : slot.AddHours(1);
            UpdateRecord(record);
            var item = new Appointment
            {
                PatientId = record.Orders.Id, Name = record.Orders.Name, FollowUp = true, Due = slot.Ticks
            };
            data.Appointments.Add(item);
            return item;
        }

        internal void UpdateRecord(Record record)
        {
            int index = data.Records.FindIndex(item => item.Orders.Id == record.Orders.Id);
            if (index < 0) data.Records.Add(record);
            else data.Records[index] = record;
        }

        internal void Expire(Appointment item) => item.State = item.FollowUp ? Status.Pending : Status.Missed;

        internal static bool ShouldExpire(Appointment item, DateTime now, bool onDuty) =>
            now.Ticks - item.Due >= TimeSpan.TicksPerHour && (!onDuty || item.State == Status.Scheduled);

        internal bool Save()
        {
            if (!Available) return false;
            try
            {
                string current = json.Serialize(data);
                if (current == saved) return true;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, current, Encoding.UTF8);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                saved = current;
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                error is InvalidOperationException)
            {
                log("Falha ao salvar agenda: " + error.Message);
                return false;
            }
        }
    }
}
