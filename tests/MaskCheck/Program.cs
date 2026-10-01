using System;
using BeatGlow;
using BeatGlow.Graphics;

namespace BeatGlow.MaskCheck
{
    internal static class Program
    {
        private static int Main()
        {
            int failures = 0;
            failures += Expect("pure white", WhiteMaskMath.Mask(255, 255, 255, 0.72f, 0.18f) > 0.95f);
            failures += Expect("black", WhiteMaskMath.Mask(0, 0, 0, 0.72f, 0.18f) == 0f);
            failures += Expect("red", WhiteMaskMath.Mask(255, 0, 0, 0.72f, 0.18f) == 0f);
            failures += Expect("yellow", WhiteMaskMath.Mask(255, 255, 0, 0.72f, 0.18f) == 0f);
            float gray = WhiteMaskMath.Mask(230, 230, 230, 0.72f, 0.18f);
            failures += Expect("gray edge", gray > 0.4f && gray < 0.9f);
            float tinted = WhiteMaskMath.Mask(255, 255, 200, 0.72f, 0.18f);
            failures += Expect("tinted near-white rejected", tinted == 0f);

            float up = EqualizerLevel.Advance(0f, 1f, 16f, 3.5f, 0.05f);
            float down = 1f - EqualizerLevel.Advance(1f, 0f, 16f, 3.5f, 0.05f);
            failures += Expect("attack rises faster than release", up > down);
            failures += Expect("attack stays in range", up > 0f && up < 1f);
            failures += Expect("gamertag trim", GamertagText.Sanitize("  Phoenix  ") == "Phoenix");
            failures += Expect("gamertag strips markup", GamertagText.Sanitize("A<size=40>B") == "Asize=40B");
            failures += Expect("gamertag empty", GamertagText.Sanitize("   ") == "");

            float br, bg, bb, energy;
            BandColor.Mix(1f, 0f, 0f, 0.12f, out br, out bg, out bb, out energy);
            failures += Expect("bass is blue", bb > br && bb > bg && energy > 0.9f);
            BandColor.Mix(0f, 1f, 0f, 0.12f, out br, out bg, out bb, out energy);
            failures += Expect("mids are green", bg > br && bg > bb);
            BandColor.Mix(0f, 0f, 1f, 0.12f, out br, out bg, out bb, out energy);
            failures += Expect("highs are red", br > bg && br > bb);
            float quietR, quietG, quietB, quietEnergy;
            BandColor.Mix(0f, 0f, 0f, 0.12f, out quietR, out quietG, out quietB, out quietEnergy);
            failures += Expect("silence stays dim", quietEnergy == 0f && quietR < 0.05f && quietG < 0.05f && quietB < 0.05f);

            float sr, sg, sb;
            SpectrumFill.Rainbow(0f, 0f, out sr, out sg, out sb);
            failures += Expect("spectrum bottom is red", sr > 0.9f && sg < 0.1f && sb < 0.1f);
            SpectrumFill.Rainbow(0.33f, 0f, out sr, out sg, out sb);
            failures += Expect("spectrum middle is green", sg > sr && sg > sb);
            SpectrumFill.Rainbow(0.66f, 0f, out sr, out sg, out sb);
            failures += Expect("spectrum upper is blue", sb > sr && sb > sg);
            float lowR, lowG, lowB, highR, highG, highB;
            SpectrumFill.ColorAt(0.15f, 0.45f, 0f, 0.1f, out lowR, out lowG, out lowB);
            SpectrumFill.ColorAt(0.9f, 0.45f, 0f, 0.1f, out highR, out highG, out highB);
            failures += Expect("equalizer lights below the level", (lowR + lowG + lowB) > (highR + highG + highB) * 2f);

            if (failures == 0)
            {
                Console.WriteLine("Mask and equalizer checks passed.");
                return 0;
            }

            Console.Error.WriteLine(failures + " check(s) failed.");
            return 1;
        }

        private static int Expect(string name, bool ok)
        {
            if (ok)
                return 0;
            Console.Error.WriteLine("FAIL " + name);
            return 1;
        }
    }
}
