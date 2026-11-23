using System;
using System.Collections.Generic;

namespace lumen_sante.Gameplay
{
    // Represents only successfully transmitted Scaleform slots, never the desired state.
    internal sealed class WebsiteSlotCache
    {
        private readonly Dictionary<int, object[]> slots = new Dictionary<int, object[]>();

        internal bool IsCurrent(int slot, object[] values)
        {
            object[] previous;
            if (!slots.TryGetValue(slot, out previous) || previous.Length != values.Length) return false;
            for (int index = 0; index < values.Length; index++)
                if (!Equals(previous[index], values[index])) return false;
            return true;
        }

        internal void Commit(int slot, object[] values) { slots[slot] = (object[])values.Clone(); }
        internal void Clear() { slots.Clear(); }
    }
}
