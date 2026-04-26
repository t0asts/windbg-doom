using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using ManagedDoom;
using ManagedDoom.UserInput;

namespace WindbgDoom
{
    internal sealed class WindbgUserInput : IUserInput
    {
        private readonly Config _config;
        private readonly bool[] _curr = new bool[(int)DoomKey.Count];
        private readonly bool[] _prev = new bool[(int)DoomKey.Count];
        private readonly bool[] _weaponKeys = new bool[7];
        private readonly uint _ourPid;
        private int _turnHeld;

        public WindbgUserInput(Config config)
        {
            _config = config;
            _ourPid = GetCurrentProcessId();
        }

        public List<DoomEvent> Poll()
        {
            Array.Copy(_curr, _prev, _curr.Length);
            Array.Clear(_curr, 0, _curr.Length);

            if (ShouldAcceptInput())
            {
                for (int vk = 1; vk < 256; vk++)
                {
                    if (vk == 0x10 || vk == 0x11 || vk == 0x12) continue;
                    short s = GetAsyncKeyState(vk);
                    if ((s & 0x8000) == 0) continue;

                    DoomKey k = DoomKeyMap.FromVk(vk, 0, false);
                    if (k != DoomKey.Unknown)
                    {
                        _curr[(int)k] = true;
                    }
                }
            }

            var edges = new List<DoomEvent>();
            for (int i = 0; i < _curr.Length; i++)
            {
                if (_curr[i] != _prev[i])
                {
                    edges.Add(new DoomEvent(
                        _curr[i] ? EventType.KeyDown : EventType.KeyUp, (DoomKey)i));
                }
            }
            return edges;
        }

        public bool IsKeyDown(DoomKey k)
        {
            int idx = (int)k;
            if ((uint)idx >= (uint)_curr.Length) return false;
            return _curr[idx];
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            bool keyForward = IsPressed(_config.key_forward);
            bool keyBackward = IsPressed(_config.key_backward);
            bool keyStrafeLeft = IsPressed(_config.key_strafeleft);
            bool keyStrafeRight = IsPressed(_config.key_straferight);
            bool keyTurnLeft = IsPressed(_config.key_turnleft);
            bool keyTurnRight = IsPressed(_config.key_turnright);
            bool keyFire = IsPressed(_config.key_fire);
            bool keyUse = IsPressed(_config.key_use);
            bool keyRun = IsPressed(_config.key_run);
            bool keyStrafe = IsPressed(_config.key_strafe);

            _weaponKeys[0] = IsKeyDown(DoomKey.Num1);
            _weaponKeys[1] = IsKeyDown(DoomKey.Num2);
            _weaponKeys[2] = IsKeyDown(DoomKey.Num3);
            _weaponKeys[3] = IsKeyDown(DoomKey.Num4);
            _weaponKeys[4] = IsKeyDown(DoomKey.Num5);
            _weaponKeys[5] = IsKeyDown(DoomKey.Num6);
            _weaponKeys[6] = IsKeyDown(DoomKey.Num7);

            cmd.Clear();

            bool strafe = keyStrafe;
            int speed = keyRun ? 1 : 0;
            int forward = 0;
            int side = 0;

            if (_config.game_alwaysrun) speed = 1 - speed;

            if (keyTurnLeft || keyTurnRight) _turnHeld++;
            else _turnHeld = 0;

            int turnSpeed = _turnHeld < PlayerBehavior.SlowTurnTics ? 2 : speed;

            if (strafe)
            {
                if (keyTurnRight) side += PlayerBehavior.SideMove[speed];
                if (keyTurnLeft) side -= PlayerBehavior.SideMove[speed];
            }
            else
            {
                if (keyTurnRight) cmd.AngleTurn -= (short)PlayerBehavior.AngleTurn[turnSpeed];
                if (keyTurnLeft) cmd.AngleTurn += (short)PlayerBehavior.AngleTurn[turnSpeed];
            }

            if (keyForward) forward += PlayerBehavior.ForwardMove[speed];
            if (keyBackward) forward -= PlayerBehavior.ForwardMove[speed];
            if (keyStrafeLeft) side -= PlayerBehavior.SideMove[speed];
            if (keyStrafeRight) side += PlayerBehavior.SideMove[speed];

            if (keyFire) cmd.Buttons |= TicCmdButtons.Attack;
            if (keyUse) cmd.Buttons |= TicCmdButtons.Use;

            for (int i = 0; i < _weaponKeys.Length; i++)
            {
                if (_weaponKeys[i])
                {
                    cmd.Buttons |= TicCmdButtons.Change;
                    cmd.Buttons |= (byte)(i << TicCmdButtons.WeaponShift);
                    break;
                }
            }

            if (forward > PlayerBehavior.MaxMove) forward = PlayerBehavior.MaxMove;
            else if (forward < -PlayerBehavior.MaxMove) forward = -PlayerBehavior.MaxMove;
            if (side > PlayerBehavior.MaxMove) side = PlayerBehavior.MaxMove;
            else if (side < -PlayerBehavior.MaxMove) side = -PlayerBehavior.MaxMove;

            cmd.ForwardMove += (sbyte)forward;
            cmd.SideMove += (sbyte)side;
        }

        private bool IsPressed(KeyBinding kb)
        {
            foreach (DoomKey k in kb.Keys)
            {
                if (IsKeyDown(k)) return true;
            }
            return false;
        }

        public void Reset() { }
        public void GrabMouse() { }
        public void ReleaseMouse() { }
        public int MaxMouseSensitivity => 9;
        public int MouseSensitivity { get => 0; set { } }

        private bool ShouldAcceptInput()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            uint pid;
            GetWindowThreadProcessId(fg, out pid);
            if (pid == _ourPid) return true;
            return IsDebuggerProcess(pid);
        }

        private static bool IsDebuggerProcess(uint pid)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return false;
            try
            {
                var sb = new StringBuilder(MAX_PATH);
                uint size = (uint)sb.Capacity;
                if (!QueryFullProcessImageNameW(h, 0, sb, ref size)) return false;

                string path = sb.ToString();
                int slash = path.LastIndexOfAny(s_pathSeparators);
                string name = slash >= 0 ? path.Substring(slash + 1) : path;

                int dot = name.LastIndexOf('.');
                if (dot > 0) name = name.Substring(0, dot);
                name = name.ToLowerInvariant();

                return name.StartsWith("windbg")
                    || name.StartsWith("dbgx")
                    || name.StartsWith("enghost")
                    || name.StartsWith("ntsd")
                    || name.StartsWith("cdb")
                    || name.StartsWith("kd");
            }
            finally
            {
                CloseHandle(h);
            }
        }

        private static readonly char[] s_pathSeparators = new[] { '\\', '/' };
        private const int MAX_PATH = 260;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageNameW(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);
    }
}
