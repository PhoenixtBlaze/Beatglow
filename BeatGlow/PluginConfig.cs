using System;

namespace BeatGlow
{
    public class PluginConfig
    {
        public static PluginConfig Instance { get; set; }

        public virtual bool Enabled { get; set; } = true;

        /// <summary>Folder name under UserData. Not an absolute path.</summary>
        public virtual string ImageFolder { get; set; } = "BeatGlow";

        /// <summary>File name only, chosen from the menu.</summary>
        public virtual string ActiveImage { get; set; } = "";

        /// <summary>Typed name shown beside the runway. Used only when DisplayMode is Gamertag.</summary>
        public virtual string Gamertag { get; set; } = "";

        /// <summary>Logo or Gamertag. Gameplay shows one of them.</summary>
        public virtual string DisplayMode { get; set; } = "Logo";

        /// <summary>When true, the name or logo stands upright. When false, it lies flat on the runway.</summary>
        public virtual bool StandUp { get; set; } = false;

        internal bool UseGamertag()
        {
            return string.Equals(DisplayMode, "Gamertag", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Gap from the note centerline to the inner edge of each copy, in meters.</summary>
        public virtual float LateralOffset { get; set; } = 2.6f;
        /// <summary>World height of the bottom of the name or logo. Zero is the note origin; the runway sits slightly below that.</summary>
        public virtual float Height { get; set; } = -0.35f;
        public virtual float DistanceAlongRunway { get; set; } = 2f;
        public virtual float Scale { get; set; } = 1.2f;
        public virtual float WhiteLumaMin { get; set; } = 0.72f;
        public virtual float WhiteChromaMax { get; set; } = 0.18f;
        public virtual float Attack { get; set; } = 48f;
        public virtual float Release { get; set; } = 14f;
        public virtual float BrightnessCap { get; set; } = 1f;
        public virtual float DimAmount { get; set; } = 0.12f;
        public virtual float LowColorR { get; set; } = 0.15f;
        public virtual float LowColorG { get; set; } = 0.75f;
        public virtual float LowColorB { get; set; } = 1f;
        public virtual float HighColorR { get; set; } = 1f;
        public virtual float HighColorG { get; set; } = 0.20f;
        public virtual float HighColorB { get; set; } = 0.35f;

        internal float SanitizedLateral()
        {
            float value = LateralOffset;
            if (value < 0f)
                value = -value;
            if (value < 2.4f)
                value = 2.4f;
            if (value > 8f)
                value = 8f;
            return value;
        }

        internal float SanitizedHeight()
        {
            return Clamp(Height, -1.5f, 3f, -0.35f);
        }

        internal float SanitizedDistance()
        {
            return Clamp(DistanceAlongRunway, 1f, 40f, 2f);
        }

        internal float SanitizedScale()
        {
            return Clamp(Scale, 0.2f, 6f, 1.2f);
        }

        internal float SanitizedLumaMin()
        {
            return Clamp(WhiteLumaMin, 0.4f, 0.98f, 0.72f);
        }

        internal float SanitizedChromaMax()
        {
            return Clamp(WhiteChromaMax, 0.02f, 0.6f, 0.18f);
        }

        internal float SanitizedAttack()
        {
            return Clamp(Attack, 0.1f, 80f, 48f);
        }

        internal float SanitizedRelease()
        {
            return Clamp(Release, 0.1f, 40f, 14f);
        }

        internal float SanitizedBrightness()
        {
            return Clamp(BrightnessCap, 0f, 1f, 1f);
        }

        internal float SanitizedDim()
        {
            return Clamp(DimAmount, 0f, 1f, 0.12f);
        }

        private static float Clamp(float value, float min, float max, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return fallback;
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
