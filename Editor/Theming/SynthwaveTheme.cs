using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Editor.Theming
{
    public static class SynthwaveTheme
    {
        public static readonly Color BackgroundDark = Color.FromArgb(18, 12, 32);
        public static readonly Color SurfaceDark = Color.FromArgb(32, 22, 53);
        public static readonly Color SurfaceLight = Color.FromArgb(45, 32, 75);

        public static readonly Color NeonPink = Color.FromArgb(255, 42, 133);
        public static readonly Color NeonCyan = Color.FromArgb(0, 243, 255);

        public static readonly Color TextPrimary = Color.FromArgb(224, 230, 242);
        public static readonly Color TextMuted = Color.FromArgb(140, 150, 180);

        public static readonly Color HighLightColor = Color.FromArgb(255, 42, 133);
        public static readonly Color HighlightText = Color.FromArgb(224, 230, 242);

        // Component Card Specifics
        public static readonly Color CardHeaderDefault = Color.FromArgb(28, 18, 48);
        public static readonly Color CardBodyDefault = Color.FromArgb(22, 15, 38);
        public static readonly Color CardHeaderSelected = NeonPink;
        public static readonly Color CardBodySelected = Color.FromArgb(48, 20, 58);
    }
}
