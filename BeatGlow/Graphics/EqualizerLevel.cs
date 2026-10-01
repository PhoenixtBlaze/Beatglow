using System;

namespace BeatGlow.Graphics
{
    /// <summary>
    /// Fast attack and slower release, so a hit climbs faster than it falls.
    /// </summary>
    internal static class EqualizerLevel
    {
        internal static float Advance(float current, float target, float attack, float release, float dt)
        {
            if (dt < 0f)
                dt = 0f;
            if (dt > 0.1f)
                dt = 0.1f;
            if (target < 0f)
                target = 0f;
            else if (target > 1f)
                target = 1f;

            float rate = target > current ? attack : release;
            if (rate < 0f)
                rate = 0f;
            float blend = 1f - (float)Math.Exp(-rate * dt);
            return current + ((target - current) * blend);
        }
    }
}
