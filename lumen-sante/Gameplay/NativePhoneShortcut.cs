using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Rage;
using Rage.Native;

namespace lumen_sante.Gameplay
{
    // GTA V Legacy b3889 cellphone_controller globals; never use these indices on another build.
    internal sealed class NativePhoneShortcut : IDisposable
    {
        private const int Table = 9509, GridMap = 10050, GridUsed = 10087, GridPage = 10124;
        private const int PhoneState = 21650, SelectedApp = 21652, PhoneMovie = 21630, Highlight = 21629;
        private const string Marker = "LSM_HOSP";
        private static readonly int[] Candidates = { 19, 18, 22, 12, 8, 6 };
        private int slot = -1, position = -1, lastState = -1;
        private long[] original;
        private bool disabled;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private long nextScan;
        private long nextConflictCheck;

        internal NativePhoneShortcut()
        {
            try
            {
                var version = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName);
                disabled = version.FileMajorPart != 1 || version.FileMinorPart != 0 ||
                    version.FileBuildPart != 3889 || version.FilePrivatePart != 0;
            }
            catch (Exception error)
            {
                disabled = true;
                Game.LogTrivial("[Lumen Santé] Não foi possível verificar a versão do GTA: " + error.Message);
            }
            if (disabled) Game.LogTrivial("[Lumen Santé] Atalho do celular desativado: versão do GTA não reconhecida.");
        }

        private static int Field(int row, int offset) { return Table + 1 + row * 15 + offset; }
        private static IntPtr Address(int index)
        {
            IntPtr address = Game.GetScriptGlobalVariableAddress(index);
            if (address == IntPtr.Zero) throw new InvalidOperationException("Global do celular indisponível.");
            return address;
        }
        private static int Read(int index) { return Marshal.ReadInt32(Address(index)); }
        private static void Write(int index, int value) { Marshal.WriteInt32(Address(index), value); }
        private static void WriteLabel(int index, string value)
        {
            var data = new byte[16];
            byte[] text = Encoding.ASCII.GetBytes(value);
            Array.Copy(text, data, Math.Min(15, text.Length));
            for (int i = 0; i < 4; i++) Write(index + i, BitConverter.ToInt32(data, i * 4));
        }
        private bool OwnsRow()
        {
            return slot >= 0 && Read(Field(slot, 0)) == BitConverter.ToInt32(Encoding.ASCII.GetBytes(Marker), 0) &&
                Read(Field(slot, 1)) == BitConverter.ToInt32(Encoding.ASCII.GetBytes(Marker), 4);
        }

        // Returns true only when this exact shortcut was selected on the native phone.
        internal bool Update()
        {
            if (disabled) return false;
            try
            {
                int state = Read(PhoneState);
                bool pressed = slot >= 0 && state == 7 && lastState != 7 && Read(SelectedApp) == slot && OwnsRow();
                bool enteredHome = state == 6 && lastState != 6;
                lastState = state;
                if (state != 6 || Read(GridPage) != 1) return pressed;
                if (enteredHome) nextScan = 0;

                if (slot >= 0 && !OwnsRow())
                {
                    // Another script or a controller rebuild took the row; do not overwrite it.
                    slot = -1; position = -1; original = null;
                }
                if (slot >= 0 && clock.ElapsedMilliseconds >= nextConflictCheck)
                {
                    nextConflictCheck = clock.ElapsedMilliseconds + 1000;
                    if (!IsCellFree(position)) { Dispose(); return pressed; }
                }
                if (slot < 0)
                {
                    if (clock.ElapsedMilliseconds < nextScan) return pressed;
                    nextScan = clock.ElapsedMilliseconds + 1000;
                    int free = Enumerable.Range(0, 9).FirstOrDefault(index => IsCellFree(index));
                    if (!IsCellFree(free)) return pressed;
                    foreach (int candidate in Candidates)
                    {
                        if (Read(Field(candidate, 4)) != -99 || Read(Field(candidate, 9)) != 0) continue;
                        slot = candidate; position = free;
                        original = Enumerable.Range(0, 15).Select(i => Marshal.ReadInt64(Address(Field(slot, i)))).ToArray();
                        WriteLabel(Field(slot, 0), Marker);
                        WriteLabel(Field(slot, 5), "appinternet");
                        Write(Field(slot, 9), unchecked((int)NativeFunction.CallByHash<uint>(0xD24D37CC275948CCUL, "appinternet")));
                        Write(Field(slot, 10), Read(Field(2, 10)));
                        Write(Field(slot, 11), 1);
                        Write(Field(slot, 4), position);
                        enteredHome = true;
                        break;
                    }
                }
                if (slot >= 0 && (Read(GridMap + 1 + position) != slot || enteredHome))
                {
                    if (!IsCellFree(position)) { Dispose(); return pressed; }
                    int movie = Read(PhoneMovie);
                    if (movie == 0 || !NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(movie, "SET_DATA_SLOT")) return pressed;
                    NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(1);
                    NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(position);
                    NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(Read(Field(slot, 10)));
                    NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(0);
                    NativeFunction.Natives.BEGIN_TEXT_COMMAND_SCALEFORM_STRING("STRING");
                    NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME("Hospital");
                    NativeFunction.Natives.END_TEXT_COMMAND_SCALEFORM_STRING();
                    NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(255);
                    NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                    Write(GridMap + 1 + position, slot);
                    Write(GridUsed + 1 + position, 1);
                    if (NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(movie, "DISPLAY_VIEW"))
                    {
                        NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(1);
                        NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(Read(Highlight));
                        NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                    }
                }
                return pressed;
            }
            catch (Exception error)
            {
                Game.LogTrivial("[Lumen Santé] Atalho do celular indisponível: " + error.Message);
                try { Dispose(); } catch { }
                disabled = true;
                return false;
            }
        }

        private bool IsCellFree(int cell)
        {
            if (cell < 0 || cell >= 9) return false;
            for (int row = 0; row < 35; row++)
                if (row != slot && Read(Field(row, 11)) == 1 && Read(Field(row, 4)) == cell) return false;
            return Read(GridUsed + 1 + cell) == 0 || (slot >= 0 && Read(GridMap + 1 + cell) == slot);
        }

        public void Dispose()
        {
            if (slot < 0) return;
            if (OwnsRow())
            {
                if (position >= 0 && Read(GridMap + 1 + position) == slot)
                {
                    int movie = Read(PhoneMovie);
                    if (Read(PhoneState) == 6 && movie != 0 && NativeFunction.Natives.BEGIN_SCALEFORM_MOVIE_METHOD<bool>(movie, "SET_DATA_SLOT"))
                    {
                        NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(1);
                        NativeFunction.Natives.SCALEFORM_MOVIE_METHOD_ADD_PARAM_INT(position);
                        NativeFunction.Natives.END_SCALEFORM_MOVIE_METHOD();
                    }
                    Write(GridUsed + 1 + position, 0);
                    Write(GridMap + 1 + position, -1);
                }
                for (int i = 0; i < original.Length; i++) Marshal.WriteInt64(Address(Field(slot, i)), original[i]);
            }
            slot = -1; position = -1; original = null;
        }
    }
}
