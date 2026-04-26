using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindbgDoom
{
    internal sealed unsafe class DbgEngOutput : IDisposable
    {
        private static readonly Guid IID_IDebugControl = new Guid(
            0x5182e668, 0x105e, 0x416e, 0xad, 0x92, 0x24, 0xef, 0x80, 0x04, 0x24, 0xba);

        private const uint DEBUG_OUTPUT_NORMAL = 0x1;

        private const uint DEBUG_OUTCTL_ALL_CLIENTS = 0x00000001;
        private const uint DEBUG_OUTCTL_IGNORE = 0x00000003;
        private const uint DEBUG_OUTCTL_DML = 0x00000020;

        private const uint DEBUG_EXECUTE_NOT_LOGGED = 0x00000002;

        private const int VtblGetInterrupt = 3;
        private const int VtblOutput = 14;
        private const int VtblControlledOutput = 16;
        private const int VtblExecute = 66;

        private IntPtr _control;

        public bool IsAvailable => _control != IntPtr.Zero;

        public DbgEngOutput(IntPtr client)
        {
            if (client == IntPtr.Zero)
            {
                return;
            }

            Guid iid = IID_IDebugControl;
            IntPtr control;
            int hr = QueryInterface(client, &iid, &control);
            if (hr >= 0 && control != IntPtr.Zero)
            {
                _control = control;
            }
        }

        public void WriteLine(string text) => Write((text ?? string.Empty) + "\n");

        public void Write(string text)
        {
            if (_control == IntPtr.Zero || string.IsNullOrEmpty(text)) return;

            byte[] fmt = Encoding.ASCII.GetBytes("%s\0");
            byte[] msg = Encoding.UTF8.GetBytes(text + "\0");

            IntPtr* vtable = *(IntPtr**)_control;
            var output = (delegate* unmanaged[Stdcall]<IntPtr, uint, byte*, byte*, int>)vtable[VtblOutput];

            fixed (byte* fmtPtr = fmt)
            fixed (byte* msgPtr = msg)
            {
                _ = output(_control, DEBUG_OUTPUT_NORMAL, fmtPtr, msgPtr);
            }
        }

        public void WriteDml(string dml)
        {
            if (_control == IntPtr.Zero || string.IsNullOrEmpty(dml)) return;

            byte[] fmt = Encoding.ASCII.GetBytes("%s\0");
            byte[] msg = Encoding.UTF8.GetBytes(dml + "\0");

            IntPtr* vtable = *(IntPtr**)_control;
            var output = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, byte*, byte*, int>)vtable[VtblControlledOutput];

            uint outputControl = DEBUG_OUTCTL_ALL_CLIENTS | DEBUG_OUTCTL_DML;
            fixed (byte* fmtPtr = fmt)
            fixed (byte* msgPtr = msg)
            {
                _ = output(_control, outputControl, DEBUG_OUTPUT_NORMAL, fmtPtr, msgPtr);
            }
        }

        public void Execute(string command)
        {
            if (_control == IntPtr.Zero || string.IsNullOrEmpty(command)) return;

            byte[] cmdBytes = Encoding.ASCII.GetBytes(command + "\0");

            IntPtr* vtable = *(IntPtr**)_control;
            var execute = (delegate* unmanaged[Stdcall]<IntPtr, uint, byte*, uint, int>)vtable[VtblExecute];

            fixed (byte* p = cmdBytes)
            {
                _ = execute(_control, DEBUG_OUTCTL_IGNORE, p, DEBUG_EXECUTE_NOT_LOGGED);
            }
        }

        public bool InterruptRequested()
        {
            if (_control == IntPtr.Zero) return false;

            IntPtr* vtable = *(IntPtr**)_control;
            var getInterrupt = (delegate* unmanaged[Stdcall]<IntPtr, int>)vtable[VtblGetInterrupt];
            int hr = getInterrupt(_control);
            return hr == 0;
        }

        public void Dispose()
        {
            if (_control != IntPtr.Zero)
            {
                IntPtr* vtable = *(IntPtr**)_control;
                var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)vtable[2];
                release(_control);
                _control = IntPtr.Zero;
            }
        }

        private static int QueryInterface(IntPtr instance, Guid* iid, IntPtr* result)
        {
            IntPtr* vtable = *(IntPtr**)instance;
            var qi = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)vtable[0];
            return qi(instance, iid, result);
        }
    }
}
