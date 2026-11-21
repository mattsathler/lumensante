using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using Rage;
using Rage.Native;
using lumen_sante.Clinical;
using Graphics = Rage.Graphics;

namespace lumen_sante.Interface
{
    internal sealed class BedManagementPanel
    {
        private const int Width = 1120;
        private const int Height = 640;
        private const int InputGroup = 0;
        private const int FrontendGroup = 2;
        private const int CursorAccept = 237;
        private const int CursorX = 239;
        private const int CursorY = 240;
        private const int FrontendCancel = 202;

        private const int VisibleTimelineRows = 4;
        private const int CursorScrollUp = 241;
        private const int CursorScrollDown = 242;
        private enum Page { Rooms, Patients, Exams, Records }
        private enum RecordTab { Exams, Notes, History }

        private static readonly int ProcessId = Process.GetCurrentProcess().Id;
        private static readonly string[] PageNames = { "Rooms", "Pacientes", "Exames" };
        private static readonly string[] RecordTabs = { "Exames", "Anotações", "Histórico médico" };
        private static readonly string[] RecordIcons = { "exams", "notes", "history" };
        private static readonly string[] SidebarIcons = { "bed", "person", "exams" };
        private static readonly RectangleF CloseButton = new RectangleF(1056, 22, 40, 38);
        private static readonly RectangleF ReturnButton = new RectangleF(218, 24, 40, 38);
        private static readonly RectangleF NotesField = new RectangleF(218, 275, 878, 276);
        private static readonly RectangleF TimelineArea = new RectangleF(218, 259, 878, 308);
        private static readonly RectangleF TimelineUp = new RectangleF(1008, 271, 32, 26);
        private static readonly RectangleF TimelineDown = new RectangleF(1048, 271, 32, 26);
        private static readonly RectangleF PortraitBounds = new RectangleF(236, 123, 64, 64);
        private static readonly RectangleF BackButton = new RectangleF(218, 105, 116, 33);
        private static readonly Color Ink = Color.FromArgb(12, 47, 40);
        private static readonly Color Muted = Color.FromArgb(74, 94, 86);
        private static readonly Color Green = Color.FromArgb(17, 83, 66);
        private static readonly Color Dark = Color.FromArgb(10, 52, 42);
        private static readonly Color Gold = Color.FromArgb(194, 156, 83);
        private static readonly Color Paper = Color.FromArgb(244, 240, 233);
        private static readonly Color Card = Color.FromArgb(252, 250, 246);
        private static readonly Color Border = Color.FromArgb(225, 219, 207);
        private static readonly Color Highlight = Color.FromArgb(224, 237, 227);

        private readonly Func<bool> roomOccupied;
        private readonly Func<string> roomPatientName;
        private string roomName;
        private readonly TerminalKeyboard keyboard = new TerminalKeyboard();
        private ExamOrders admission;
        private PatientEntry[] patients = Array.Empty<PatientEntry>();
        private Ped portraitPatient;
        private int portraitHandle = -1;
        private bool portraitRequested;
        private ExamLine[] examLines = Array.Empty<ExamLine>();
        private ExamLine[] timeline = Array.Empty<ExamLine>();
        private bool deceased;
        private Page page;
        private RecordTab recordTab;
        private int selectedExam = -1;
        private bool returnToRecord;
        private bool editingNotes;
        private int timelineOffset;

        private bool subscribed;
        private FocusWatch focusWatch;
        private volatile Frame frame;
        private Texture cursorTexture;
        private readonly Texture[] sidebarTextures = new Texture[SidebarIcons.Length];
        private readonly Texture[] recordTextures = new Texture[RecordIcons.Length];
        private Texture closeTexture;
        private Texture returnTexture;
        private bool cursorInitialized;

        private static RectangleF NavigationRow(int index) => new RectangleF(12, 187 + index * 48, 170, 42);

        private static RectangleF ExamRow(int index) => new RectangleF(218, 179 + index * 49, 878, 46);

        private static RectangleF PatientRow(int index) => new RectangleF(218, 162 + index * 66, 878, 58);

        private static RectangleF RecordTabBounds(int index) => new RectangleF(218 + index * 178, 208, 172, 36);

        private static RectangleF TimelineRow(int index) => new RectangleF(235, 308 + index * 61, 844, 55);

        internal BedManagementPanel(Func<bool> roomOccupied, Func<string> roomPatientName)
        {
            this.roomOccupied = roomOccupied;
            this.roomPatientName = roomPatientName;
        }

        internal void Open(string diagnosticRoom)
        {
            roomName = diagnosticRoom;
            if (!cursorInitialized)
            {
                cursorInitialized = true;
                try
                {
                    cursorTexture = CreateCursorTexture();
                }
                catch (Exception error)
                {
                    Game.LogTrivial("[Central do Hospital] Falha ao carregar o cursor: " + error.Message);
                }

                for (int index = 0; index < SidebarIcons.Length; index++)
                {
                    sidebarTextures[index] = LoadIcon(SidebarIcons[index]);
                }

                for (int index = 0; index < RecordIcons.Length; index++)
                {
                    recordTextures[index] = LoadIcon(RecordIcons[index], Ink);
                }

                closeTexture = LoadIcon("close", Ink);
                returnTexture = LoadIcon("return", Ink);
            }

            page = Page.Rooms;
            selectedExam = -1;
            returnToRecord = false;
            timelineOffset = 0;
            focusWatch = new FocusWatch();
            Game.RawFrameRender += Draw;
            subscribed = true;
        }

        internal void Close()
        {
            frame = null;
            editingNotes = false;
            ReleasePortrait();
            focusWatch?.Dispose();
            focusWatch = null;

            if (subscribed)
            {
                Game.RawFrameRender -= Draw;
                subscribed = false;
            }
        }

        internal void SetPatients(PatientEntry[] updatedPatients)
        {
            patients = updatedPatients;
            int selectedIndex = Array.FindIndex(patients, patient => patient.Orders == admission);
            if (selectedIndex >= 0)
            {
                deceased = patients[selectedIndex].Deceased;
                return;
            }

            SelectPatient(patients.Length == 0 ? default(PatientEntry) : patients[0]);
            if (page == Page.Records)
            {
                page = Page.Patients;
            }
        }

        private void SelectPatient(PatientEntry patient)
        {
            ReleasePortrait();
            portraitPatient = patient.Ped;
            admission = patient.Orders;
            selectedExam = -1;
            returnToRecord = false;
            editingNotes = false;
            deceased = patient.Deceased;
            RefreshExams(admission);
        }

        internal void RefreshExams(ExamOrders orders)
        {
            if (orders != admission)
            {
                return;
            }

            examLines = admission?.Items
                .Where(order => order.Status != ExamStatus.Available)
                .Select(order => new ExamLine(order.Name, order.Status,
                    order.Status == ExamStatus.Completed ? new ExamReport(admission, order) : null,
                    order.CompletedAtMilliseconds))
                .ToArray() ?? Array.Empty<ExamLine>();
            var updatedTimeline = examLines.Where(exam => exam.Status == ExamStatus.Completed)
                .OrderByDescending(exam => exam.CompletedAtMilliseconds).ToArray();
            if (updatedTimeline.Length != timeline.Length)
            {
                timelineOffset = 0;
            }
            timeline = updatedTimeline;
        }

        internal bool Update()
        {
            if (focusWatch.LostFocus || !TerminalKeyboard.IsGameFocused(ProcessId) || Game.IsPaused ||
                NativeFunction.Natives.IS_PAUSE_MENU_ACTIVE<bool>())
            {
                return false;
            }

            NativeFunction.Natives.DISABLE_ALL_CONTROL_ACTIONS(InputGroup);
            if (Game.Console.IsOpen)
            {
                return true;
            }

            NativeFunction.CallByHash<uint>(0xAAE7CE1D63167423UL); // SET_MOUSE_CURSOR_THIS_FRAME
            NativeFunction.Natives.SET_INPUT_EXCLUSIVE(FrontendGroup, CursorX);
            NativeFunction.Natives.SET_INPUT_EXCLUSIVE(FrontendGroup, CursorY);
            NativeFunction.Natives.SET_INPUT_EXCLUSIVE(FrontendGroup, CursorAccept);
            NativeFunction.Natives.SET_INPUT_EXCLUSIVE(FrontendGroup, CursorScrollUp);
            NativeFunction.Natives.SET_INPUT_EXCLUSIVE(FrontendGroup, CursorScrollDown);

            Size resolution = Game.Resolution;
            if (resolution.Width <= 0 || resolution.Height <= 0)
            {
                return true;
            }

            float scale = Math.Min(resolution.Width / 1300f, resolution.Height / 760f);
            float left = (resolution.Width - Width * scale) / 2f;
            float top = (resolution.Height - Height * scale) / 2f;
            var cursor = new PointF(
                (NativeFunction.Natives.GET_CONTROL_NORMAL<float>(FrontendGroup, CursorX) * resolution.Width - left) / scale,
                (NativeFunction.Natives.GET_CONTROL_NORMAL<float>(FrontendGroup, CursorY) * resolution.Height - top) / scale);
            bool click = NativeFunction.Natives.IS_CONTROL_JUST_PRESSED<bool>(FrontendGroup, CursorAccept);

            if (Game.IsKeyDown(Keys.Escape) ||
                (!editingNotes && NativeFunction.Natives.IS_DISABLED_CONTROL_JUST_PRESSED<bool>(InputGroup, FrontendCancel)) ||
                (click && CloseButton.Contains(cursor)))
            {
                return false;
            }

            for (int index = 0; click && index < PageNames.Length; index++)
            {
                if (NavigationRow(index).Contains(cursor))
                {
                    if (page == Page.Records)
                    {
                        ReleasePortrait();
                    }

                    page = (Page)index;
                    selectedExam = -1;
                    returnToRecord = false;
                    editingNotes = false;
                    click = false;
                }
            }

            if (page == Page.Records && recordTab == RecordTab.Exams)
            {
                UpdateTimelineScroll(cursor, click);
            }

            if (editingNotes && (Game.IsKeyDown(Keys.Enter) || (click && !NotesField.Contains(cursor))))
            {
                editingNotes = false;
            }

            if (click)
            {
                HandleContentClick(cursor);
            }

            if (editingNotes)
            {
                admission.Notes = keyboard.Update(admission.Notes);
            }

            bool portraitVisible = page == Page.Records && DrawPortrait(resolution, scale, left, top);
            bool occupied = roomOccupied();
            frame = new Frame(resolution, scale, left, top, cursor, page, selectedExam,
                recordTab, admission?.PatientName, admission?.Profile, admission?.Room ?? roomName,
                roomName, occupied, roomPatientName(), patients,
                admission?.IsInpatient == true, deceased, admission?.Notes,
                editingNotes ? keyboard.Caret : -1,
                examLines, timeline, timelineOffset, portraitVisible, returnToRecord);
            return true;
        }

        private bool DrawPortrait(Size resolution, float scale, float left, float top)
        {
            if (!portraitRequested && portraitPatient != null && portraitPatient.Exists())
            {
                portraitRequested = true;
                portraitHandle = NativeFunction.Natives.REGISTER_PEDHEADSHOT<int>(portraitPatient);
            }

            if (portraitHandle < 0 || !NativeFunction.Natives.IS_PEDHEADSHOT_VALID<bool>(portraitHandle) ||
                !NativeFunction.Natives.IS_PEDHEADSHOT_READY<bool>(portraitHandle))
            {
                return false;
            }

            string texture = NativeFunction.Natives.GET_PEDHEADSHOT_TXD_STRING<string>(portraitHandle);
            if (string.IsNullOrEmpty(texture))
            {
                return false;
            }

            NativeFunction.Natives.DRAW_SPRITE(texture, texture,
                (left + (PortraitBounds.X + PortraitBounds.Width / 2) * scale) / resolution.Width,
                (top + (PortraitBounds.Y + PortraitBounds.Height / 2) * scale) / resolution.Height,
                PortraitBounds.Width * scale / resolution.Width, PortraitBounds.Height * scale / resolution.Height,
                0f, 255, 255, 255, 255, false);
            return true;
        }

        private void ReleasePortrait()
        {
            if (portraitHandle >= 0)
            {
                NativeFunction.Natives.UNREGISTER_PEDHEADSHOT(portraitHandle);
                portraitHandle = -1;
            }

            portraitRequested = false;
        }

        private void UpdateTimelineScroll(PointF cursor, bool click)
        {
            int maxOffset = Math.Max(0, timeline.Length - VisibleTimelineRows);
            if (maxOffset == 0)
            {
                return;
            }

            bool overTimeline = TimelineArea.Contains(cursor);
            if ((click && TimelineUp.Contains(cursor)) || Game.IsKeyDown(Keys.PageUp) ||
                (overTimeline && NativeFunction.Natives.IS_CONTROL_JUST_PRESSED<bool>(FrontendGroup, CursorScrollUp)))
            {
                timelineOffset = Math.Max(0, timelineOffset - 1);
            }
            else if ((click && TimelineDown.Contains(cursor)) || Game.IsKeyDown(Keys.PageDown) ||
                (overTimeline && NativeFunction.Natives.IS_CONTROL_JUST_PRESSED<bool>(FrontendGroup, CursorScrollDown)))
            {
                timelineOffset = Math.Min(maxOffset, timelineOffset + 1);
            }
        }

        private void HandleContentClick(PointF cursor)
        {
            if (page == Page.Patients)
            {
                for (int index = 0; index < patients.Length; index++)
                {
                    if (PatientRow(index).Contains(cursor))
                    {
                        SelectPatient(patients[index]);
                        page = Page.Records;
                        recordTab = RecordTab.Exams;
                        timelineOffset = 0;
                        break;
                    }
                }
            }
            else if (page == Page.Records)
            {
                if (ReturnButton.Contains(cursor))
                {
                    page = Page.Patients;
                    editingNotes = false;
                    ReleasePortrait();
                    return;
                }

                for (int index = 0; index < RecordTabs.Length; index++)
                {
                    if (RecordTabBounds(index).Contains(cursor))
                    {
                        recordTab = (RecordTab)index;
                        editingNotes = false;
                        return;
                    }
                }

                if (recordTab == RecordTab.Notes && admission != null && !editingNotes && NotesField.Contains(cursor))
                {
                    keyboard.Begin(admission.Notes);
                    editingNotes = true;
                }
                else if (recordTab == RecordTab.Exams)
                {
                    int visible = Math.Min(VisibleTimelineRows, timeline.Length - timelineOffset);
                    for (int index = 0; index < visible; index++)
                    {
                        if (TimelineRow(index).Contains(cursor))
                        {
                            selectedExam = Array.IndexOf(examLines, timeline[timelineOffset + index]);
                            returnToRecord = true;
                            page = Page.Exams;
                            ReleasePortrait();
                            break;
                        }
                    }
                }
            }
            else if (page == Page.Exams && selectedExam >= 0 && BackButton.Contains(cursor))
            {
                selectedExam = -1;
                if (returnToRecord && admission != null)
                {
                    page = Page.Records;
                }

                returnToRecord = false;
            }
            else if (page == Page.Exams && selectedExam < 0)
            {
                for (int index = 0; index < examLines.Length; index++)
                {
                    if (ExamRow(index).Contains(cursor) && examLines[index].Status == ExamStatus.Completed)
                    {
                        selectedExam = index;
                        break;
                    }
                }
            }

        }

        private void Draw(object sender, GraphicsEventArgs eventArgs)
        {
            Frame current = frame;
            if (current == null)
            {
                return;
            }

            Graphics graphics = eventArgs.Graphics;
            RectangleF ToScreen(RectangleF bounds) =>
                new RectangleF(current.Left + bounds.X * current.Scale, current.Top + bounds.Y * current.Scale,
                    bounds.Width * current.Scale, bounds.Height * current.Scale);

            // O retrato é desenhado pelo jogo antes da camada RawFrameRender.
            RectangleF portrait = ToScreen(PortraitBounds);
            void Background(RectangleF bounds, Color color)
            {
                RectangleF hole = RectangleF.Intersect(bounds, portrait);
                if (!current.PortraitVisible || hole.Width <= 0 || hole.Height <= 0)
                {
                    graphics.DrawRectangle(bounds, color);
                    return;
                }

                graphics.DrawRectangle(RectangleF.FromLTRB(bounds.Left, bounds.Top, bounds.Right, hole.Top), color);
                graphics.DrawRectangle(RectangleF.FromLTRB(bounds.Left, hole.Bottom, bounds.Right, bounds.Bottom), color);
                graphics.DrawRectangle(RectangleF.FromLTRB(bounds.Left, hole.Top, hole.Left, hole.Bottom), color);
                graphics.DrawRectangle(RectangleF.FromLTRB(hole.Right, hole.Top, bounds.Right, hole.Bottom), color);
            }

            Background(new RectangleF(0, 0, current.Resolution.Width, current.Resolution.Height),
                Color.FromArgb(140, 10, 24, 22));

            void Fill(RectangleF bounds, Color color) => graphics.DrawRectangle(ToScreen(bounds), color);

            void Label(string value, float x, float y, float size, Color color, string font = "Segoe UI") =>
                graphics.DrawText(value, font, size * 0.85f * current.Scale,
                    new PointF(current.Left + x * current.Scale, current.Top + y * current.Scale), color);

            void Box(RectangleF bounds, Color color)
            {
                Fill(bounds, Border);
                Fill(RectangleF.Inflate(bounds, -1, -1), color);
            }

            void Icon(Texture texture, float x, float y, float size)
            {
                if (texture != null)
                {
                    graphics.DrawTexture(texture, ToScreen(new RectangleF(x, y, size, size)));
                }
            }

            void IconButton(RectangleF bounds, Texture texture, string fallback)
            {
                if (texture == null)
                {
                    Label(fallback, bounds.X + 8, bounds.Y + 5, 22, Ink);
                    return;
                }

                Icon(texture, bounds.X + 8, bounds.Y + 7, 24);
            }

            void Paragraph(string value, float x, float y, int charactersPerLine, float lineHeight = 24)
            {
                string remaining = value.Trim();
                while (remaining.Length > charactersPerLine)
                {
                    int split = remaining.LastIndexOf(' ', charactersPerLine);
                    if (split <= 0)
                    {
                        split = charactersPerLine;
                    }

                    Label(remaining.Substring(0, split), x, y, 18, Ink);
                    remaining = remaining.Substring(split).TrimStart();
                    y += lineHeight;
                }

                Label(remaining, x, y, 18, Ink);
            }

            Background(ToScreen(new RectangleF(0, 0, Width, Height)), Paper);
            Fill(new RectangleF(0, 0, 194, Height), Dark);
            Fill(new RectangleF(24, 34, 30, 3), Gold);
            Label("Central Los Santos", 24, 55, 20, Color.White, "Georgia");
            Label("Medical Center", 24, 82, 20, Color.White, "Georgia");
            Fill(new RectangleF(24, 155, 145, 1), Color.FromArgb(83, 121, 107));

            for (int index = 0; index < PageNames.Length; index++)
            {
                RectangleF row = NavigationRow(index);
                bool active = (int)(current.Page == Page.Records || current.ReturnToRecord
                    ? Page.Patients : current.Page) == index;
                if (active || row.Contains(current.Cursor))
                {
                    Fill(row, Color.FromArgb(38, 91, 75));
                }

                if (active)
                {
                    Fill(new RectangleF(row.X, row.Y, 3, row.Height), Gold);
                }

                Icon(sidebarTextures[index], row.X + 12, row.Y + 10, 21);
                Label(PageNames[index], row.X + 42, row.Y + 10, 16, Color.White);
            }

            if (current.Page == Page.Records)
            {
                IconButton(ReturnButton, returnTexture, "<");
                Label("Prontuário", 274, 22, 37, Ink, "Georgia");
            }
            else
            {
                Label(PageNames[(int)current.Page], 218, 22, 37, Ink, "Georgia");
            }
            Fill(new RectangleF(218, 77, 58, 2), Gold);
            IconButton(CloseButton, closeTexture, "X");

            bool occupied = current.PatientName != null;
            string patientStatus = current.Deceased ? "Óbito registrado" :
                current.IsInpatient ? "Internado" : "Em atendimento";
            switch (current.Page)
            {
                case Page.Rooms:
                    void Metric(float x, string title, string value)
                    {
                        Box(new RectangleF(x, 108, 282, 72), Card);
                        Label(title, x + 15, 120, 15, Muted);
                        Label(value, x + 15, 142, 24, Ink, "Georgia");
                    }

                    Metric(218, "Total de Rooms", "1");
                    Metric(516, "Ocupados", current.DiagnosticRoomOccupied ? "1" : "0");
                    Metric(814, "Disponíveis", current.DiagnosticRoomOccupied ? "0" : "1");
                    Box(new RectangleF(218, 204, 878, 131), Card);
                    Label("Ocupação", 235, 219, 23, Ink, "Georgia");
                    Fill(new RectangleF(235, 262, 844, 52), current.DiagnosticRoomOccupied ? Highlight : Paper);
                    Label(current.DiagnosticRoom, 250, 276, 18, Ink);
                    string diagnosticPatient = current.DiagnosticRoomOccupied
                        ? current.DiagnosticPatientName ?? "Paciente a caminho" : "—";
                    Label(diagnosticPatient, 510, 276, 18, Ink);
                    Label(current.DiagnosticRoomOccupied ? "Ocupado" : "Disponível", 915, 276, 17, Green);
                    break;
                case Page.Patients:
                    Label("Pacientes internados", 218, 114, 19, Muted);
                    if (current.Patients.Length == 0)
                    {
                        Label("Nenhum paciente internado.", 218, 180, 20, Ink);
                        break;
                    }

                    for (int index = 0; index < current.Patients.Length; index++)
                    {
                        PatientEntry patient = current.Patients[index];
                        RectangleF row = PatientRow(index);
                        Box(row, row.Contains(current.Cursor) ? Highlight : Card);
                        Label(patient.Orders.PatientName, 233, row.Y + 18, 19, Ink);
                        Label(patient.Orders.Room + (patient.Deceased ? " · Óbito registrado" : " · Internado"),
                            585, row.Y + 19, 17, Muted);
                        Label("Prontuário ›", 974, row.Y + 19, 16, Green);
                    }
                    break;
                case Page.Exams:
                    if (current.SelectedExam >= 0)
                    {
                        ExamLine exam = current.Exams[current.SelectedExam];
                        IconButton(BackButton, returnTexture, "<");
                        Box(new RectangleF(218, 155, 878, 401), Card);
                        Label(exam.Name, 239, 181, 27, Ink, "Georgia");
                        Label(current.PatientName + "  ·  " + current.Room + "  ·  Realizado", 239, 221, 17, Green);
                        ExamReport report = exam.Report;

                        void ReportField(string label, string value, float x, float y)
                        {
                            Label(label, x, y, 15, Muted);
                            Label(value, x, y + 21, 18, Ink);
                        }

                        ReportField("Método utilizado", report.Method, 239, 258);
                        ReportField("Médico responsável", report.Physician, 680, 258);
                        ReportField("Laboratório / setor", report.Unit, 239, 314);
                        ReportField("Material / registro", report.Sample, 680, 314);
                        ReportField("Protocolo", report.Protocol, 239, 370);
                        ReportField("Liberação no plantão",
                            TimeSpan.FromMilliseconds(exam.CompletedAtMilliseconds).ToString(@"hh\:mm\:ss"),
                            680, 370);
                        Fill(new RectangleF(239, 431, 835, 1), Border);
                        Label("Achados do exame", 239, 448, 20, Ink);
                        Paragraph(report.Findings, 239, 481, 89);
                        break;
                    }

                    int completed = 0;
                    foreach (ExamLine exam in current.Exams)
                    {
                        if (exam.Status == ExamStatus.Completed)
                        {
                            completed++;
                        }
                    }

                    Label((current.Exams.Length - completed) + " aguardando  ·  " + completed + " realizados", 218, 108, 19, Muted);
                    if (current.Exams.Length == 0)
                    {
                        Label("Nenhum exame solicitado.", 218, 179, 20, Ink);
                        break;
                    }

                    Label("Exame", 232, 150, 15, Muted);
                    Label("Paciente", 585, 150, 15, Muted);
                    Label("Situação", 850, 150, 15, Muted);
                    for (int index = 0; index < current.Exams.Length; index++)
                    {
                        ExamLine exam = current.Exams[index];
                        RectangleF row = ExamRow(index);
                        bool ready = exam.Status == ExamStatus.Completed;
                        Box(row, ready && row.Contains(current.Cursor) ? Highlight : Card);
                        Label(exam.Name, 232, row.Y + 11, 17, Ink);
                        Label(current.PatientName, 585, row.Y + 11, 17, Ink);
                        Label(ready ? "Realizado · Ver resultado ›" : "Aguardando realização",
                            850, row.Y + 11, 16, ready ? Green : Color.FromArgb(126, 82, 28));
                    }
                    break;
                case Page.Records:
                    if (!occupied)
                    {
                        Label("Nenhum prontuário disponível.", 218, 140, 20, Ink);
                        break;
                    }

                    Background(ToScreen(new RectangleF(218, 112, 878, 86)), Dark);
                    if (!current.PortraitVisible)
                    {
                        Fill(PortraitBounds, Green);
                        Label(current.PatientName.Substring(0, 1), 258, 137, 27, Color.White, "Georgia");
                    }
                    Label(current.PatientName, 317, 128, 31, Color.White, "Georgia");
                    Label(current.Room + " · " + patientStatus, 318, 166, 17, Color.White);
                    Label(current.Profile.Age + " anos", 641, 129, 18, Color.White);
                    Label(current.Profile.Phone, 641, 162, 17, Color.White);
                    Label("Tipo sanguíneo", 911, 128, 16, Color.White);
                    Label(current.Profile.BloodType, 911, 158, 22, Color.White);

                    for (int index = 0; index < RecordTabs.Length; index++)
                    {
                        RectangleF tab = RecordTabBounds(index);
                        bool active = (int)current.RecordTab == index;
                        Fill(tab, active || tab.Contains(current.Cursor) ? Highlight : Card);
                        Icon(recordTextures[index], tab.X + 10, tab.Y + 8, 20);
                        Label(RecordTabs[index], tab.X + 38, tab.Y + 8, 17, Ink);
                        if (active)
                        {
                            Fill(new RectangleF(tab.X, tab.Bottom - 2, tab.Width, 2), Gold);
                        }
                    }

                    if (current.RecordTab == RecordTab.Notes)
                    {
                        bool editing = current.NotesCaret >= 0;
                        Box(NotesField, editing || NotesField.Contains(current.Cursor) ? Highlight : Card);
                        if (editing || !string.IsNullOrWhiteSpace(current.Notes))
                        {
                            string notes = editing ? current.Notes.Insert(current.NotesCaret, "|") : current.Notes;
                            Paragraph(notes, 235, 293, 80, 22);
                        }
                        else
                        {
                            Label("Clique para escrever uma anotação", 235, 293, 17, Muted);
                        }
                        break;
                    }

                    if (current.RecordTab == RecordTab.History)
                    {
                        Box(TimelineArea, Card);
                        Label("Exames anteriores à internação", 235, 272, 21, Ink, "Georgia");
                        for (int index = 0; index < current.Profile.MedicalHistory.Count; index++)
                        {
                            PreviousExam previous = current.Profile.MedicalHistory[index];
                            float y = 313 + index * 51;
                            Fill(new RectangleF(235, y, 844, 43), Paper);
                            Label(previous.Name, 248, y + 9, 18, Ink);
                            Label("Há " + previous.MonthsAgo + " meses", 917, y + 9, 16, Muted);
                        }
                        break;
                    }

                    Box(TimelineArea, Card);
                    Label("Timeline de resultados", 235, 272, 23, Ink, "Georgia");
                    if (current.Timeline.Length > VisibleTimelineRows)
                    {
                        if (current.TimelineOffset > 0)
                        {
                            IconButton(TimelineUp, null, "↑");
                        }

                        if (current.TimelineOffset + VisibleTimelineRows < current.Timeline.Length)
                        {
                            IconButton(TimelineDown, null, "↓");
                        }
                    }

                    if (current.Timeline.Length == 0)
                    {
                        Label("Nenhum exame realizado.", 235, 313, 17, Muted);
                    }

                    int visible = Math.Min(VisibleTimelineRows, current.Timeline.Length - current.TimelineOffset);
                    for (int index = 0; index < visible; index++)
                    {
                        ExamLine item = current.Timeline[current.TimelineOffset + index];
                        RectangleF row = TimelineRow(index);
                        float y = row.Y;
                        if (row.Contains(current.Cursor))
                        {
                            Fill(row, Highlight);
                        }
                        Fill(new RectangleF(235, y + 5, 3, 46), Gold);
                        Label(item.Name, 248, y + 3, 18, Ink);
                        Label(TimeSpan.FromMilliseconds(item.CompletedAtMilliseconds).ToString(@"hh\:mm\:ss"),
                            1004, y + 4, 15, Muted);
                        Paragraph(item.Report.Findings, 248, y + 25, 92, 17);
                    }
                    break;
            }

            Fill(new RectangleF(218, 578, 878, 1), Border);
            Label(current.NotesCaret >= 0 ? "Enter conclui a edição · Esc fecha o computador" : "Esc para sair",
                218, 593, 15, Muted);

            Texture pointer = cursorTexture;
            if (new RectangleF(0, 0, Width, Height).Contains(current.Cursor))
            {
                float x = current.Cursor.X;
                float y = current.Cursor.Y;
                if (pointer != null)
                {
                    float size = 24f * current.Scale;
                    graphics.DrawTexture(pointer, new RectangleF(
                        current.Left + x * current.Scale - size / 12f,
                        current.Top + y * current.Scale - size / 12f, size, size));
                }
                else
                {
                    Fill(new RectangleF(x - 4, y - 4, 8, 8), Ink);
                    Fill(new RectangleF(x - 2, y - 2, 4, 4), Color.White);
                }
            }
        }

        private static Texture CreateCursorTexture()
        {
            using (var bitmap = new Bitmap(96, 96))
            using (var painter = System.Drawing.Graphics.FromImage(bitmap))
            using (var outline = new Pen(Ink, 5f) { LineJoin = LineJoin.Round })
            using (var encoded = new MemoryStream())
            {
                painter.SmoothingMode = SmoothingMode.AntiAlias;
                var arrow = new[]
                {
                    new Point(8, 8), new Point(70, 53), new Point(44, 56),
                    new Point(56, 80), new Point(43, 86), new Point(31, 62), new Point(13, 80)
                };
                painter.FillPolygon(Brushes.White, arrow);
                painter.DrawPolygon(outline, arrow);
                bitmap.Save(encoded, ImageFormat.Png);
                return LoadCachedTexture(encoded.ToArray());
            }
        }

        private static Texture LoadIcon(string name, Color? tint = null)
        {
            try
            {
                using (Stream resource = typeof(BedManagementPanel).Assembly
                    .GetManifestResourceStream("lumen_sante.Assets." + name + ".png"))
                {
                    if (resource == null)
                    {
                        return null;
                    }

                    using (var encoded = new MemoryStream())
                    {
                        if (tint.HasValue)
                        {
                            using (var image = Image.FromStream(resource))
                            using (var bitmap = new Bitmap(image.Width, image.Height))
                            using (var painter = System.Drawing.Graphics.FromImage(bitmap))
                            using (var attributes = new ImageAttributes())
                            {
                                Color color = tint.Value;
                                var matrix = new ColorMatrix
                                {
                                    Matrix00 = 0f, Matrix11 = 0f, Matrix22 = 0f,
                                    Matrix40 = color.R / 255f, Matrix41 = color.G / 255f, Matrix42 = color.B / 255f
                                };
                                attributes.SetColorMatrix(matrix);
                                var bounds = new Rectangle(0, 0, image.Width, image.Height);
                                painter.DrawImage(image, bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                                bitmap.Save(encoded, ImageFormat.Png);
                            }
                        }
                        else
                        {
                            resource.CopyTo(encoded);
                        }

                        return LoadCachedTexture(encoded.ToArray());
                    }
                }
            }
            catch (Exception error)
            {
                Game.LogTrivial("[Central do Hospital] Falha ao carregar o ícone " + name + ": " + error.Message);
                return null;
            }
        }

        private static Texture LoadCachedTexture(byte[] png)
        {
            string directory = Path.Combine(Path.GetTempPath(), "LumenSante", "HospitalUiTextures");
            Directory.CreateDirectory(directory);
            string hash;
            using (var sha = SHA256.Create())
            {
                hash = BitConverter.ToString(sha.ComputeHash(png)).Replace("-", string.Empty);
            }

            string path = Path.Combine(directory, hash + ".png");
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, png);
            }

            // O RAGE pode manter o arquivo aberto depois de criar a textura.
            return Game.CreateTextureFromFile(path);
        }

        private sealed class FocusWatch : IDisposable
        {
            private readonly System.Threading.Timer timer;
            internal volatile bool LostFocus;

            internal FocusWatch()
            {
                timer = new System.Threading.Timer(CheckFocus, null, 0, 100);
            }

            private void CheckFocus(object state)
            {
                if (!LostFocus && !TerminalKeyboard.IsGameFocused(ProcessId))
                {
                    LostFocus = true;
                }
            }

            public void Dispose() => timer.Dispose();
        }

        private sealed class Frame
        {
            internal readonly Size Resolution;
            internal readonly float Scale;
            internal readonly float Left;
            internal readonly float Top;
            internal readonly PointF Cursor;
            internal readonly Page Page;
            internal readonly RecordTab RecordTab;
            internal readonly PatientProfile Profile;
            internal readonly int SelectedExam;
            internal readonly string Notes;
            internal readonly int NotesCaret;
            internal readonly string PatientName;
            internal readonly string Room;
            internal readonly string DiagnosticRoom;
            internal readonly bool DiagnosticRoomOccupied;
            internal readonly string DiagnosticPatientName;
            internal readonly PatientEntry[] Patients;
            internal readonly bool IsInpatient;
            internal readonly bool Deceased;
            internal readonly ExamLine[] Exams;
            internal readonly ExamLine[] Timeline;
            internal readonly int TimelineOffset;
            internal readonly bool PortraitVisible;
            internal readonly bool ReturnToRecord;

            internal Frame(Size resolution, float scale, float left, float top, PointF cursor,
                Page page, int selectedExam, RecordTab recordTab, string patientName, PatientProfile profile,
                string room, string diagnosticRoom, bool diagnosticRoomOccupied, string diagnosticPatientName,
                PatientEntry[] patients,
                bool isInpatient, bool deceased, string notes, int notesCaret,
                ExamLine[] exams, ExamLine[] timeline, int timelineOffset, bool portraitVisible, bool returnToRecord)
            {
                Resolution = resolution;
                Scale = scale;
                Left = left;
                Top = top;
                Cursor = cursor;
                Page = page;
                RecordTab = recordTab;
                Profile = profile;
                SelectedExam = selectedExam;
                Notes = notes;
                NotesCaret = notesCaret;
                PatientName = patientName;
                Room = room;
                DiagnosticRoom = diagnosticRoom;
                DiagnosticRoomOccupied = diagnosticRoomOccupied;
                DiagnosticPatientName = diagnosticPatientName;
                Patients = patients;
                IsInpatient = isInpatient;
                Deceased = deceased;
                Exams = exams;
                Timeline = timeline;
                TimelineOffset = timelineOffset;
                PortraitVisible = portraitVisible;
                ReturnToRecord = returnToRecord;
            }
        }

        internal readonly struct PatientEntry
        {
            internal readonly ExamOrders Orders;
            internal readonly Ped Ped;
            internal readonly bool Deceased;

            internal PatientEntry(ExamOrders orders, Ped ped, bool deceased)
            {
                Orders = orders;
                Ped = ped;
                Deceased = deceased;
            }
        }

        private readonly struct ExamLine
        {
            internal readonly string Name;
            internal readonly ExamStatus Status;
            internal readonly ExamReport Report;
            internal readonly long CompletedAtMilliseconds;

            internal ExamLine(string name, ExamStatus status, ExamReport report, long completedAtMilliseconds)
            {
                Name = name;
                Status = status;
                Report = report;
                CompletedAtMilliseconds = completedAtMilliseconds;
            }
        }
    }
}
