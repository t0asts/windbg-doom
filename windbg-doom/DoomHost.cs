using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using ManagedDoom;
using ManagedDoom.Audio;

namespace WindbgDoom
{
    internal static class DoomHost
    {
        private const int DefaultCharsW = 160;
        private const int DefaultCharsH = 50;

        private const int MinCharsW = 40;
        private const int MaxCharsW = 640;
        private const int MinCharsH = 15;
        private const int MaxCharsH = 200;

        public static void Run(string commandLine, DbgEngOutput output)
        {
            ParsedArgs parsed;
            try
            {
                parsed = ParseArgs(commandLine, output);
                if (parsed == null) return;
            }
            catch (Exception ex)
            {
                output.WriteLine("windbg-doom: " + ex.Message);
                return;
            }

            try
            {
                RunGame(parsed, output);
            }
            catch (Exception ex)
            {
                output.WriteLine("windbg-doom: aborted " + ex.Message);
                LogException(ex);
            }
        }

        private static void RunGame(ParsedArgs parsed, DbgEngOutput output)
        {
            var cmdArgs = new CommandLineArgs(parsed.EngineArgs);

            var config = new Config();
            config.video_highresolution = true;
            config.video_screenwidth = 640;
            config.video_screenheight = 400;
            config.video_fpsscale = 1;

            ApplyKeyOverride(config, parsed.UseKey, "use", b => config.key_use = b, output);
            ApplyKeyOverride(config, parsed.FireKey, "fire", b => config.key_fire = b, output);

            using var content = new GameContent(cmdArgs);

            var input = new WindbgUserInput(config);
            var video = new WindbgVideo(config, content, output, parsed.CharsW, parsed.CharsH);
            var sound = NullSound.GetInstance();
            var music = NullMusic.GetInstance();

            var doom = new Doom(cmdArgs, config, content, video, sound, music, input);

            output.WriteLine("windbg-doom: running at " + parsed.CharsW + "x" + parsed.CharsH +
                " cells. Press Ctrl+Break to stop. Keep windbg focused for input.");

            const int targetFps = 6;
            long tickFreq = Stopwatch.Frequency;
            long ticksPerFrame = tickFreq / targetFps;
            long nextTick = Stopwatch.GetTimestamp();

            while (true)
            {
                if (output.InterruptRequested())
                {
                    break;
                }

                foreach (var ev in input.Poll())
                {
                    doom.PostEvent(ev);
                }

                var update = doom.Update();
                if (update == UpdateResult.Completed) break;

                video.Render(doom, Fixed.One);

                nextTick += ticksPerFrame;
                long now = Stopwatch.GetTimestamp();
                long sleep = nextTick - now;
                if (sleep > 0)
                {
                    int ms = (int)((sleep * 1000) / tickFreq);
                    if (ms > 0) Thread.Sleep(ms);
                }
                else if (-sleep > ticksPerFrame * 4)
                {
                    nextTick = Stopwatch.GetTimestamp();
                }
            }

            output.WriteLine("windbg-doom: stopped.");
        }

        private static void ApplyKeyOverride(Config config, string spec, string label, Action<KeyBinding> setter, DbgEngOutput output)
        {
            if (string.IsNullOrEmpty(spec)) return;

            var binding = KeyBinding.Parse(spec.ToLowerInvariant());
            if (binding.Keys.Count == 0)
            {
                output.WriteLine("windbg-doom: warning -" + label + " '" + spec +
                    "' not recognized as a key. Keeping default binding.");
                return;
            }
            setter(binding);
        }

        private sealed class ParsedArgs
        {
            public string[] EngineArgs;
            public int CharsW;
            public int CharsH;
            public string UseKey;
            public string FireKey;
        }

        private static ParsedArgs ParseArgs(string commandLine, DbgEngOutput output)
        {
            var list = new List<string>();
            int i = 0;
            string s = commandLine ?? string.Empty;

            while (i < s.Length)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
                if (i >= s.Length) break;

                var sb = new StringBuilder();
                if (s[i] == '"')
                {
                    i++;
                    while (i < s.Length && s[i] != '"') sb.Append(s[i++]);
                    if (i < s.Length) i++;
                }
                else
                {
                    while (i < s.Length && !char.IsWhiteSpace(s[i])) sb.Append(s[i++]);
                }
                list.Add(sb.ToString());
            }

            if (list.Count == 0)
            {
                output.WriteLine("Usage: !doom <path-to-IWAD> [-res WxH] [-use KEY] [-fire KEY] [-warp E M] [-skill N] [-nomonsters] [-fast] [-respawn]");
                return null;
            }

            if (list[0].Length > 0 && list[0][0] != '-')
            {
                list.Insert(0, "-iwad");
            }

            int charsW = DefaultCharsW;
            int charsH = DefaultCharsH;

            string resValue = ConsumeStringFlag(list, "-res");
            if (resValue != null && !TryParseRes(resValue, out charsW, out charsH))
            {
                throw new ArgumentException(
                    "Bad -res value '" + resValue + "'. Expected WxH, e.g. 200x60. Range: " +
                    MinCharsW + "x" + MinCharsH + " to " + MaxCharsW + "x" + MaxCharsH + ".");
            }

            string useKey = ConsumeStringFlag(list, "-use");
            string fireKey = ConsumeStringFlag(list, "-fire");

            for (int k = 0; k < list.Count - 1; k++)
            {
                if (string.Equals(list[k], "-iwad", StringComparison.OrdinalIgnoreCase))
                {
                    string p = list[k + 1];
                    if (!File.Exists(p))
                    {
                        throw new FileNotFoundException("IWAD not found: " + p);
                    }
                    break;
                }
            }

            return new ParsedArgs
            {
                EngineArgs = list.ToArray(),
                CharsW = charsW,
                CharsH = charsH,
                UseKey = useKey,
                FireKey = fireKey,
            };
        }

        private static string ConsumeStringFlag(List<string> list, string flag)
        {
            for (int k = list.Count - 2; k >= 0; k--)
            {
                if (string.Equals(list[k], flag, StringComparison.OrdinalIgnoreCase))
                {
                    string value = list[k + 1];
                    list.RemoveAt(k + 1);
                    list.RemoveAt(k);
                    return value;
                }
            }
            return null;
        }

        private static bool TryParseRes(string spec, out int w, out int h)
        {
            w = 0; h = 0;
            if (string.IsNullOrEmpty(spec)) return false;

            int x = spec.IndexOfAny(new[] { 'x', 'X' });
            if (x <= 0 || x >= spec.Length - 1) return false;

            if (!int.TryParse(spec.AsSpan(0, x), out int parsedW)) return false;
            if (!int.TryParse(spec.AsSpan(x + 1), out int parsedH)) return false;

            if (parsedW < MinCharsW || parsedW > MaxCharsW) return false;
            if (parsedH < MinCharsH || parsedH > MaxCharsH) return false;

            w = parsedW;
            h = parsedH;
            return true;
        }

        private static void LogException(Exception ex)
        {
            try
            {
                File.AppendAllText(
                    Path.Combine(Path.GetTempPath(), "windbg-doom.log"),
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " +
                    ex.ToString() + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
