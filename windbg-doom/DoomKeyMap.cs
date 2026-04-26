using ManagedDoom;

namespace WindbgDoom
{
    internal static class DoomKeyMap
    {
        public const int VkBack = 0x08;
        public const int VkTab = 0x09;
        public const int VkReturn = 0x0D;
        public const int VkShift = 0x10;
        public const int VkControl = 0x11;
        public const int VkMenu = 0x12;
        public const int VkPause = 0x13;
        public const int VkEscape = 0x1B;
        public const int VkSpace = 0x20;
        public const int VkPrior = 0x21;
        public const int VkNext = 0x22;
        public const int VkEnd = 0x23;
        public const int VkHome = 0x24;
        public const int VkLeft = 0x25;
        public const int VkUp = 0x26;
        public const int VkRight = 0x27;
        public const int VkDown = 0x28;
        public const int VkInsert = 0x2D;
        public const int VkDelete = 0x2E;
        public const int VkLShift = 0xA0;
        public const int VkRShift = 0xA1;
        public const int VkLControl = 0xA2;
        public const int VkRControl = 0xA3;
        public const int VkLMenu = 0xA4;
        public const int VkRMenu = 0xA5;

        public static DoomKey FromVk(int vk, int scanCode, bool extended)
        {
            if (vk == VkShift)
            {
                int resolved = MapVirtualKey((uint)scanCode, MAPVK_VSC_TO_VK_EX);
                if (resolved == VkRShift) return DoomKey.RShift;
                return DoomKey.LShift;
            }
            if (vk == VkControl) return extended ? DoomKey.RControl : DoomKey.LControl;
            if (vk == VkMenu) return extended ? DoomKey.RAlt : DoomKey.LAlt;

            switch (vk)
            {
                case VkBack: return DoomKey.Backspace;
                case VkTab: return DoomKey.Tab;
                case VkReturn: return DoomKey.Enter;
                case VkPause: return DoomKey.Pause;
                case VkEscape: return DoomKey.Escape;
                case VkSpace: return DoomKey.Space;
                case VkPrior: return DoomKey.PageUp;
                case VkNext: return DoomKey.PageDown;
                case VkEnd: return DoomKey.End;
                case VkHome: return DoomKey.Home;
                case VkLeft: return DoomKey.Left;
                case VkUp: return DoomKey.Up;
                case VkRight: return DoomKey.Right;
                case VkDown: return DoomKey.Down;
                case VkInsert: return DoomKey.Insert;
                case VkDelete: return DoomKey.Delete;
                case VkLShift: return DoomKey.LShift;
                case VkRShift: return DoomKey.RShift;
                case VkLControl: return DoomKey.LControl;
                case VkRControl: return DoomKey.RControl;
                case VkLMenu: return DoomKey.LAlt;
                case VkRMenu: return DoomKey.RAlt;
            }

            if (vk >= 'A' && vk <= 'Z')
            {
                return (DoomKey)((int)DoomKey.A + (vk - 'A'));
            }
            if (vk >= '0' && vk <= '9')
            {
                return (DoomKey)((int)DoomKey.Num0 + (vk - '0'));
            }
            if (vk >= 0x60 && vk <= 0x69) return (DoomKey)((int)DoomKey.Numpad0 + (vk - 0x60));
            if (vk >= 0x70 && vk <= 0x7E) return (DoomKey)((int)DoomKey.F1 + (vk - 0x70));

            switch (vk)
            {
                case 0x6A: return DoomKey.Multiply;   
                case 0x6B: return DoomKey.Add;        
                case 0x6D: return DoomKey.Subtract;   
                case 0x6F: return DoomKey.Divide;     
                case 0xBA: return DoomKey.Semicolon;  
                case 0xBB: return DoomKey.Equal;      
                case 0xBC: return DoomKey.Comma;      
                case 0xBD: return DoomKey.Hyphen;     
                case 0xBE: return DoomKey.Period;     
                case 0xBF: return DoomKey.Slash;      
                case 0xC0: return DoomKey.Tilde;      
                case 0xDB: return DoomKey.LBracket;   
                case 0xDC: return DoomKey.Backslash;  
                case 0xDD: return DoomKey.RBracket;   
                case 0xDE: return DoomKey.Quote;      
            }

            return DoomKey.Unknown;
        }

        private const uint MAPVK_VSC_TO_VK_EX = 3;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int MapVirtualKey(uint code, uint mapType);
    }
}
