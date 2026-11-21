using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace lumen_sante.Interface
{
    internal sealed class TerminalKeyboard
    {
        private readonly byte[] state = new byte[256];
        private readonly bool[] held = new bool[256];
        private readonly StringBuilder characters = new StringBuilder(8);

        internal int Caret { get; private set; }

        internal void Begin(string text)
        {
            Caret = text.Length;
            for (int key = 8; key < state.Length; key++)
            {
                held[key] = GetAsyncKeyState(key) < 0;
            }
        }

        internal string Update(string text)
        {
            for (int key = 8; key < state.Length; key++)
            {
                state[key] = GetAsyncKeyState(key) < 0 ? (byte)128 : (byte)0;
            }

            state[(int)Keys.CapsLock] |= (byte)(GetKeyState((int)Keys.CapsLock) & 1);
            var layout = GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), out _));
            for (int key = 8; key < state.Length; key++)
            {
                bool down = (state[key] & 128) != 0;
                bool pressed = down && !held[key];
                held[key] = down;
                if (!pressed)
                {
                    continue;
                }

                switch ((Keys)key)
                {
                    case Keys.Back when Caret > 0:
                        text = text.Remove(--Caret, 1);
                        break;
                    case Keys.Delete when Caret < text.Length:
                        text = text.Remove(Caret, 1);
                        break;
                    case Keys.Left:
                        Caret = Math.Max(0, Caret - 1);
                        break;
                    case Keys.Right:
                        Caret = Math.Min(text.Length, Caret + 1);
                        break;
                    case Keys.Home:
                        Caret = 0;
                        break;
                    case Keys.End:
                        Caret = text.Length;
                        break;
                    default:
                        characters.Clear();
                        int count = ToUnicodeEx((uint)key, MapVirtualKeyEx((uint)key, 0, layout), state,
                            characters, characters.Capacity, 0, layout);
                        if (count > 0 && text.Length + characters.Length <= 240)
                        {
                            string typed = characters.ToString();
                            if (!char.IsControl(typed[0]))
                            {
                                text = text.Insert(Caret, typed);
                                Caret += typed.Length;
                            }
                        }
                        break;
                }
            }

            return text;
        }

        internal static bool IsGameFocused(int processId)
        {
            IntPtr window = GetForegroundWindow();
            return window != IntPtr.Zero && !IsIconic(window) &&
                GetWindowThreadProcessId(window, out uint owner) != 0 && owner == (uint)processId;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr window);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int key);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint thread);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKeyEx(uint code, uint mapType, IntPtr layout);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicodeEx(uint key, uint scanCode, byte[] state, StringBuilder buffer,
            int bufferLength, uint flags, IntPtr layout);
    }
}
