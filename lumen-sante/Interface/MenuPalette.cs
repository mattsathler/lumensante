using System.Drawing;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace lumen_sante.Interface
{
    internal enum ActionTone { Neutral, Accent, Destructive }

    internal static class MenuPalette
    {
        internal static readonly Color Ink = Color.FromArgb(12, 47, 40);
        internal static readonly Color Highlight = Color.FromArgb(224, 237, 227);
        internal static readonly Color Accent = Color.FromArgb(85, 170, 155);
        private static readonly Color AccentBackground = Color.FromArgb(207, 233, 222);
        private static readonly Color AccentHighlighted = Color.FromArgb(155, 208, 190);
        private static readonly Color DestructiveBackground = Color.FromArgb(250, 223, 220);
        private static readonly Color DestructiveHighlighted = Color.FromArgb(241, 175, 171);
        private static readonly Color DestructiveInk = Color.FromArgb(111, 37, 35);

        internal static T Apply<T>(T item, ActionTone tone = ActionTone.Neutral) where T : UIMenuItem
        {
            if (!item.Enabled) tone = ActionTone.Neutral;
            item.BackColor = tone == ActionTone.Neutral ? UIMenuItem.DefaultBackColor : ActionBackground(tone, false);
            item.ForeColor = tone == ActionTone.Neutral ? UIMenuItem.DefaultForeColor : ActionText(tone);
            item.HighlightedBackColor = ActionBackground(tone, true);
            item.HighlightedForeColor = ActionText(tone);
            item.DisabledForeColor = UIMenuItem.DefaultDisabledForeColor;
            return item;
        }

        internal static void SetEnabled(UIMenuItem item, bool enabled, ActionTone tone)
        {
            if (item.Enabled == enabled) return;
            item.Enabled = enabled;
            Apply(item, tone);
        }

        internal static UIMenuItem Item(string label, ActionTone tone = ActionTone.Neutral, bool enabled = true) =>
            Apply(new UIMenuItem(label) { Enabled = enabled }, tone);

        internal static UIMenu SubMenu(MenuPool pool, UIMenu parent, string label, ActionTone tone = ActionTone.Neutral)
        {
            UIMenu submenu = pool.AddSubMenu(parent, label);
            Apply(submenu.ParentItem, tone);
            return submenu;
        }

        internal static Color ActionBackground(ActionTone tone, bool highlighted)
        {
            switch (tone)
            {
                case ActionTone.Accent: return highlighted ? AccentHighlighted : AccentBackground;
                case ActionTone.Destructive: return highlighted ? DestructiveHighlighted : DestructiveBackground;
                default: return Highlight;
            }
        }

        internal static Color ActionText(ActionTone tone) => tone == ActionTone.Destructive ? DestructiveInk : Ink;
    }
}
