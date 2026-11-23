using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using lumen_sante.Gameplay;

namespace lumen_sante.Interface
{
    internal sealed partial class BedManagementPanel
    {
        private enum AgendaView { Day, Week, Pending }
        private const int AgendaPageSize = 5;
        private static readonly CultureInfo AgendaCulture = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly RectangleF AgendaPrevious = new RectangleF(218, 157, 42, 34);
        private static readonly RectangleF AgendaNext = new RectangleF(1054, 157, 42, 34);
        private static readonly RectangleF AgendaPageUp = new RectangleF(998, 538, 42, 34);
        private static readonly RectangleF AgendaPageDown = new RectangleF(1054, 538, 42, 34);
        private static readonly string[] AgendaTabs = { "Dia", "Semana", "Retornos Pendentes" };
        private MedicalAgenda agenda;
        private Func<DateTime> agendaDate;
        private Func<Guid, bool> cancelPending;
        private AgendaView agendaView;
        private int agendaDateOffset;
        private int agendaPageIndex;
        private Guid confirmCancellation;
        private string agendaFeedback;

        internal void ConfigureAgenda(MedicalAgenda service, Func<DateTime> date, Func<Guid, bool> cancel)
        {
            if (agenda != service)
            {
                agendaView = AgendaView.Day;
                agendaDateOffset = agendaPageIndex = 0;
                confirmCancellation = Guid.Empty;
                agendaFeedback = null;
            }
            agenda = service;
            agendaDate = date;
            cancelPending = cancel;
        }

        private static RectangleF AgendaTab(int index) => new RectangleF(218 + index * 178, 103, 170, 40);
        private static RectangleF AgendaRow(int index) => new RectangleF(218, 202 + index * 65, 878, 58);
        private static RectangleF AgendaDelete(int index) => new RectangleF(904, AgendaRow(index).Y + 10, 180, 38);

        private sealed class AgendaFrame
        {
            internal bool Available;
            internal AgendaView View;
            internal string Heading;
            internal AgendaLine[] Lines = Array.Empty<AgendaLine>();
            internal int PageIndex;
            internal int PageCount;
            internal Guid Confirmation;
            internal string Feedback;
        }

        private sealed class AgendaLine
        {
            internal Guid Id;
            internal Guid PatientId;
            internal string Name;
            internal string Detail;
            internal bool CanOpen;
        }

        private AgendaFrame BuildAgendaFrame()
        {
            DateTime now = agendaDate?.Invoke() ?? default(DateTime);
            var result = new AgendaFrame
            {
                Available = agenda?.Available == true && now != default(DateTime),
                View = agendaView, Confirmation = confirmCancellation, Feedback = agendaFeedback
            };
            if (!result.Available) return result;
            DateTime start = now.Date;
            int days = agendaView == AgendaView.Week ? 7 : 1;
            if (agendaView == AgendaView.Week) start = start.AddDays(-(((int)start.DayOfWeek + 6) % 7));
            start = start.AddDays(agendaDateOffset * days);
            DateTime end = start.AddDays(days);
            result.Heading = agendaView == AgendaView.Pending ? "Retornos perdidos" :
                agendaView == AgendaView.Week ? start.ToString("dd/MM/yyyy") + " a " + end.AddDays(-1).ToString("dd/MM/yyyy") :
                start.ToString("dddd, dd/MM/yyyy", AgendaCulture);
            var entries = agenda.Appointments.Where(item => agendaView == AgendaView.Pending
                ? item.State == MedicalAgenda.Status.Pending
                : item.State != MedicalAgenda.Status.Cancelled && item.Due >= start.Ticks && item.Due < end.Ticks)
                .OrderBy(item => item.Due).ToArray();
            result.PageCount = Math.Max(1, (entries.Length + AgendaPageSize - 1) / AgendaPageSize);
            result.PageIndex = Math.Min(agendaPageIndex, result.PageCount - 1);
            result.Lines = entries.Skip(result.PageIndex * AgendaPageSize).Take(AgendaPageSize).Select(item => new AgendaLine
            {
                Id = item.Id, PatientId = item.PatientId, Name = item.Name,
                CanOpen = patients.Any(patient => patient.Orders.AdmissionId == item.PatientId),
                Detail = item.Date.ToString("ddd dd/MM · HH:mm", AgendaCulture) + " · " +
                    (item.FollowUp ? "Retorno" : "Consulta") + " · " + AgendaStatus(item.State)
            }).ToArray();
            return result;
        }

        private static string AgendaStatus(MedicalAgenda.Status state)
        {
            switch (state)
            {
                case MedicalAgenda.Status.Ready: return "Aguardando entrada";
                case MedicalAgenda.Status.Arrived: return "Na recepção";
                case MedicalAgenda.Status.InCare: return "Em atendimento";
                case MedicalAgenda.Status.Completed: return "Atendido";
                case MedicalAgenda.Status.Pending: return "Pendente";
                case MedicalAgenda.Status.Missed: return "Consulta perdida";
                default: return "Agendado";
            }
        }

        private void HandleAgendaClick(PointF cursor)
        {
            for (int index = 0; index < AgendaTabs.Length; index++)
            {
                if (!AgendaTab(index).Contains(cursor)) continue;
                agendaView = (AgendaView)index;
                agendaDateOffset = agendaPageIndex = 0;
                confirmCancellation = Guid.Empty;
                agendaFeedback = null;
                return;
            }
            AgendaFrame view = BuildAgendaFrame();
            if (!view.Available) return;
            if (agendaView != AgendaView.Pending && (AgendaPrevious.Contains(cursor) || AgendaNext.Contains(cursor)))
            {
                agendaDateOffset = Math.Max(-52, Math.Min(52, agendaDateOffset + (AgendaPrevious.Contains(cursor) ? -1 : 1)));
                agendaPageIndex = 0;
                return;
            }
            if (AgendaPageUp.Contains(cursor) || AgendaPageDown.Contains(cursor))
            {
                agendaPageIndex = Math.Max(0, Math.Min(view.PageCount - 1,
                    view.PageIndex + (AgendaPageUp.Contains(cursor) ? -1 : 1)));
                confirmCancellation = Guid.Empty;
                return;
            }
            for (int index = 0; index < view.Lines.Length; index++)
            {
                if (!AgendaRow(index).Contains(cursor)) continue;
                AgendaLine line = view.Lines[index];
                if (agendaView == AgendaView.Pending && AgendaDelete(index).Contains(cursor))
                {
                    if (confirmCancellation != line.Id)
                    {
                        confirmCancellation = line.Id;
                        agendaFeedback = null;
                    }
                    else
                    {
                        agendaFeedback = cancelPending(line.Id) ? "Acompanhamento encerrado." : "Não foi possível gravar a alteração.";
                        confirmCancellation = Guid.Empty;
                    }
                    return;
                }
                if (!line.CanOpen) return;
                SelectPatient(patients.First(patient => patient.Orders.AdmissionId == line.PatientId));
                recordOrigin = Page.Agenda;
                page = Page.Records;
                recordTab = RecordTab.Exams;
                timelineOffset = 0;
                return;
            }
        }

        private static void DrawAgenda(Frame frame, Action<RectangleF, Color> box,
            Action<string, float, float, float, Color> label,
            Action<RectangleF> drawPrevious, Action<RectangleF> drawNext)
        {
            AgendaFrame view = frame.Agenda;
            if (view == null || !view.Available)
            {
                label("Agenda indisponível.", 218, 114, 19, Muted);
                return;
            }
            void Button(RectangleF bounds, string caption, bool selected = false, ActionTone tone = ActionTone.Neutral)
            {
                bool highlighted = selected || bounds.Contains(frame.Cursor);
                box(bounds, tone == ActionTone.Neutral
                    ? (highlighted ? Highlight : Card) : MenuPalette.ActionBackground(tone, highlighted));
                label(caption, bounds.X + 10, bounds.Y + 8, 16, MenuPalette.ActionText(tone));
            }
            for (int index = 0; index < AgendaTabs.Length; index++)
                Button(AgendaTab(index), AgendaTabs[index], (int)view.View == index);
            if (view.View != AgendaView.Pending)
            {
                drawPrevious(AgendaPrevious);
                drawNext(AgendaNext);
            }
            label(view.Heading, 278, 165, 19, Muted);
            if (view.Lines.Length == 0) label("Nenhuma consulta nesta lista.", 233, 222, 19, Ink);
            for (int index = 0; index < view.Lines.Length; index++)
            {
                AgendaLine line = view.Lines[index];
                RectangleF row = AgendaRow(index);
                box(row, line.CanOpen && row.Contains(frame.Cursor) ? Highlight : Card);
                label(line.Name, 233, row.Y + 6, 19, Ink);
                label(line.Detail, 233, row.Y + 32, 15, Muted);
                if (view.View == AgendaView.Pending)
                    Button(AgendaDelete(index), view.Confirmation == line.Id ? "Confirmar exclusão" : "Excluir retorno",
                        selected: view.Confirmation == line.Id, tone: ActionTone.Destructive);
                else if (line.CanOpen)
                    label("Prontuário ›", 974, row.Y + 18, 16, Green);
            }
            if (view.PageIndex > 0) drawPrevious(AgendaPageUp);
            if (view.PageIndex + 1 < view.PageCount) drawNext(AgendaPageDown);
            if (view.View == AgendaView.Pending && view.Feedback != null)
                label(view.Feedback, 233, 546, 16, Muted);
        }
    }
}
