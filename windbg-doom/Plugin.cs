using System;
using System.Runtime.InteropServices;

namespace WindbgDoom
{
    public static class Plugin
    {
        [UnmanagedCallersOnly(EntryPoint = "DebugExtensionInitialize")]
        public static unsafe int DebugExtensionInitialize(uint* version, uint* flags)
        {
            *version = (1u & 0xffff) << 16;
            *flags = 0;
            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "DebugExtensionUninitialize")]
        public static void DebugExtensionUninitialize()
        {
        }

        [UnmanagedCallersOnly(EntryPoint = "doom")]
        public static int Doom(IntPtr client, IntPtr argsPtr)
        {
            try
            {
                using var output = new DbgEngOutput(client);
                string args = Marshal.PtrToStringAnsi(argsPtr) ?? string.Empty;
                args = args.Trim();

                if (args.Length == 0 || args == "?" || args == "/?" || args == "-?" || args == "help")
                {
                    PrintHelp(output);
                    return 0;
                }

                DoomHost.Run(args, output);
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "help")]
        public static int Help(IntPtr client, IntPtr argsPtr)
        {
            using var output = new DbgEngOutput(client);
            PrintHelp(output);
            return 0;
        }

        private static void PrintHelp(DbgEngOutput output)
        {
            output.WriteLine("windbg-doom: play DOOM in windbg's output pane.");
            output.WriteLine("");
            output.WriteLine("  !doom <IWAD-path> [options]   Run a game. Ctrl+Break stops it.");
            output.WriteLine("                                Keep windbg focused for keyboard input.");
            output.WriteLine("");
            output.WriteLine("  -res WxH      Cell grid (default 160x50, max 640x200).");
            output.WriteLine("  -use KEY      Use/interact key (default Space).");
            output.WriteLine("  -fire KEY     Fire key (default Ctrl). Comma-separate: -use \"e,enter\".");
            output.WriteLine("");
            output.WriteLine("  Engine flags forwarded to managed-doom:");
            output.WriteLine("    -warp E M, -skill N, -nomonsters, -fast, -respawn, -file <pwad>");
            output.WriteLine("");
            output.WriteLine("Defaults: arrows/WASD move, Ctrl fire, Space use, Shift run, Alt strafe,");
            output.WriteLine("          1-7 weapons, Esc menu, Tab automap.");
        }
    }
}
