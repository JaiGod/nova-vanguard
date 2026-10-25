using UnityEngine;

namespace NovaVanguard
{
    /// <summary>Tiny software synthesizer that generates every sound effect and music loop at startup.</summary>
    public static class Synth
    {
        static System.Random rng = new System.Random(1234);
        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        static float Saw(float ph) => 2f * (ph - Mathf.Floor(ph + 0.5f));
        static float Square(float ph) => (ph - Mathf.Floor(ph)) < 0.5f ? 1f : -1f;
        static float Tri(float ph) => 1f - 4f * Mathf.Abs((ph - Mathf.Floor(ph)) - 0.5f);
        static float Sin(float ph) => Mathf.Sin(ph * 2f * Mathf.PI);
        static float Midi(float n) => 440f * Mathf.Pow(2f, (n - 69f) / 12f);

        static AudioClip Clip(string name, float[] data, int rate)
        {
            // soft clip + normalise
            float peak = 0.0001f;
            for (int i = 0; i < data.Length; i++) { data[i] = (float)System.Math.Tanh(data[i]); peak = Mathf.Max(peak, Mathf.Abs(data[i])); }
            float g = 0.92f / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= g;
            var c = AudioClip.Create(name, data.Length, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        public static AudioClip MakeSfx(Sfx s, int rate)
        {
            float dur;
            switch (s)
            {
                case Sfx.Explosion: dur = 0.7f; break;
                case Sfx.BigExplosion: dur = 1.8f; break;
                case Sfx.Emp: case Sfx.Ultimate: dur = 1.2f; break;
                case Sfx.Alarm: dur = 0.9f; break;
                case Sfx.Warp: case Sfx.PowerDown: dur = 0.8f; break;
                case Sfx.Missile: case Sfx.Boost: dur = 0.5f; break;
                case Sfx.Purchase: dur = 0.45f; break;
                default: dur = 0.18f; break;
            }
            int n = Mathf.CeilToInt(dur * rate);
            var d = new float[n];
            float ph = 0f, ph2 = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate, k = t / dur;
                float env = Mathf.Exp(-k * 5f);
                float v = 0f, f;
                switch (s)
                {
                    case Sfx.Laser:
                        f = Mathf.Lerp(1800f, 300f, Mathf.Pow(k, 0.5f)); ph += f / rate;
                        v = Square(ph) * 0.35f * env; break;
                    case Sfx.EnemyShot:
                        f = Mathf.Lerp(900f, 200f, k); ph += f / rate;
                        v = Saw(ph) * 0.3f * env; break;
                    case Sfx.Plasma:
                        f = Mathf.Lerp(500f, 90f, k); ph += f / rate; ph2 += f * 1.01f / rate;
                        v = (Saw(ph) + Saw(ph2)) * 0.3f * env + Noise() * 0.1f * env; break;
                    case Sfx.MachineGun:
                        lp += (Noise() - lp) * 0.35f; f = 160f; ph += f / rate;
                        v = (lp * 0.8f + Square(ph) * 0.3f) * Mathf.Exp(-k * 12f); break;
                    case Sfx.Pulse:
                        f = Mathf.Lerp(700f, 1400f, k); ph += f / rate;
                        v = Tri(ph) * 0.5f * env * (0.6f + 0.4f * Square(t * 60f)); break;
                    case Sfx.Missile:
                        lp += (Noise() - lp) * 0.08f; f = Mathf.Lerp(120f, 300f, k); ph += f / rate;
                        v = (lp * 1.4f + Saw(ph) * 0.2f) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 3f)) * (1f - k); break;
                    case Sfx.Explosion:
                    case Sfx.BigExplosion:
                        lp += (Noise() - lp) * Mathf.Lerp(0.25f, 0.02f, k);
                        f = Mathf.Lerp(90f, 30f, k); ph += f / rate;
                        v = (lp * 2f + Sin(ph) * 0.6f * Mathf.Exp(-k * 6f)) * Mathf.Exp(-k * (s == Sfx.BigExplosion ? 2.5f : 4f)); break;
                    case Sfx.Hit:
                        lp += (Noise() - lp) * 0.5f; v = lp * Mathf.Exp(-k * 14f); break;
                    case Sfx.ShieldHit:
                        f = 1200f + 400f * Sin(t * 40f); ph += f / rate; v = Sin(ph) * 0.4f * Mathf.Exp(-k * 9f); break;
                    case Sfx.Emp:
                        f = Mathf.Lerp(60f, 1600f, Mathf.Pow(1f - k, 3f)); ph += f / rate;
                        v = (Square(ph) * 0.3f + Noise() * 0.2f) * env; break;
                    case Sfx.Ultimate:
                        f = Mathf.Lerp(80f, 900f, k); ph += f / rate; ph2 += f * 1.5f / rate;
                        v = (Saw(ph) + Saw(ph2) * 0.6f) * 0.3f * Mathf.Sin(Mathf.PI * k); break;
                    case Sfx.Pickup:
                        f = k < 0.5f ? 880f : 1320f; ph += f / rate; v = Square(ph) * 0.25f * env; break;
                    case Sfx.UiClick:
                        f = 1000f; ph += f / rate; v = Sin(ph) * 0.5f * Mathf.Exp(-k * 18f); break;
                    case Sfx.UiBack:
                        f = 600f; ph += f / rate; v = Sin(ph) * 0.5f * Mathf.Exp(-k * 18f); break;
                    case Sfx.Purchase:
                        f = Midi(k < 0.33f ? 72 : k < 0.66f ? 76 : 79); ph += f / rate; v = Square(ph) * 0.25f * Mathf.Exp(-((k * 3f) % 1f) * 3f); break;
                    case Sfx.Alarm:
                        f = ((int)(k * 4f)) % 2 == 0 ? 880f : 660f; ph += f / rate; v = Saw(ph) * 0.3f * (k < 0.95f ? 1f : (1f - k) * 20f); break;
                    case Sfx.Boost:
                        lp += (Noise() - lp) * Mathf.Lerp(0.05f, 0.3f, k); v = lp * 1.5f * Mathf.Sin(Mathf.PI * k); break;
                    case Sfx.PowerDown:
                        f = Mathf.Lerp(600f, 40f, k); ph += f / rate; v = Square(ph) * 0.3f * (1f - k); break;
                    case Sfx.Warp:
                        f = Mathf.Lerp(100f, 2000f, k * k); ph += f / rate; v = Sin(ph) * 0.4f * Mathf.Sin(Mathf.PI * k) + Noise() * 0.05f; break;
                }
                d[i] = v;
            }
            return Clip("sfx_" + s, d, rate);
        }

        public static AudioClip MakeMusic(Music m, int rate)
        {
            // Chord roots (MIDI) per bar and tempo for each track.
            int[] prog; float bpm; int bars; bool minorArp = true; float drive = 1f;
            switch (m)
            {
                case Music.Menu: prog = new[] { 45, 41, 48, 43 }; bpm = 92; bars = 8; drive = 0.6f; break;
                case Music.Boss: prog = new[] { 40, 40, 43, 38, 40, 40, 46, 45 }; bpm = 148; bars = 8; drive = 1.5f; break;
                case Music.Victory: prog = new[] { 48, 53, 55, 60 }; bpm = 120; bars = 4; minorArp = false; break;
                default: prog = new[] { 45, 41, 43, 40 }; bpm = 128; bars = 8; break;
            }
            float beat = 60f / bpm;
            float dur = bars * 4 * beat;
            int n = Mathf.CeilToInt(dur * rate);
            var d = new float[n];
            int[] arp = minorArp ? new[] { 0, 3, 7, 12, 7, 3, 10, 7 } : new[] { 0, 4, 7, 12, 16, 12, 7, 4 };
            float bassPh = 0f, arpPh = 0f, padPh1 = 0f, padPh2 = 0f, padPh3 = 0f, kickPh = 0f, lp = 0f, lpArp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float beatPos = t / beat;
                int bar = Mathf.FloorToInt(beatPos / 4f) % bars;
                int root = prog[bar % prog.Length];
                float inBeat = beatPos - Mathf.Floor(beatPos);
                float six = beatPos * 4f; int step = Mathf.FloorToInt(six); float inSix = six - step;

                // Bass: eighth-note pulse
                float eighth = beatPos * 2f - Mathf.Floor(beatPos * 2f);
                bassPh += Midi(root - 12) / rate;
                float bass = Saw(bassPh) * Mathf.Exp(-eighth * 4f) * 0.5f;
                lp += (bass - lp) * 0.12f;

                // Arpeggio: sixteenth notes
                int note = root + 12 + arp[step % arp.Length];
                arpPh += Midi(note) / rate;
                float a = Square(arpPh) * 0.5f + Saw(arpPh * 1.005f) * 0.5f;
                lpArp += (a - lpArp) * (m == Music.Menu ? 0.06f : 0.18f);
                float arpV = lpArp * Mathf.Exp(-inSix * 6f) * (m == Music.Menu ? 0.18f : 0.24f);

                // Pad: sustained triad
                int third = minorArp ? 3 : 4;
                padPh1 += Midi(root + 12) / rate; padPh2 += Midi(root + 12 + third) / rate * 1.002f; padPh3 += Midi(root + 19) / rate * 0.998f;
                float pad = (Tri(padPh1) + Tri(padPh2) + Tri(padPh3)) * 0.07f * (m == Music.Menu ? 1.6f : 1f);

                // Drums
                float drums = 0f;
                if (m != Music.Menu || bar >= 4)
                {
                    float kf = 50f + 120f * Mathf.Exp(-inBeat * 30f); kickPh += kf / rate;
                    drums += Sin(kickPh) * Mathf.Exp(-inBeat * 9f) * 0.75f * drive;
                    int beatIdx = Mathf.FloorToInt(beatPos) % 4;
                    if (beatIdx == 1 || beatIdx == 3) drums += Noise() * Mathf.Exp(-inBeat * 14f) * 0.28f * drive; // snare
                    drums += Noise() * Mathf.Exp(-inSix * 40f) * 0.06f; // hats
                }
                else kickPh = 0f;

                float v = lp + arpV + pad + drums;
                // smooth loop seam
                float edge = Mathf.Min(t, dur - t);
                if (edge < 0.01f) v *= edge / 0.01f;
                d[i] = v * 0.9f;
            }
            return Clip("music_" + m, d, rate);
        }
    }
}
