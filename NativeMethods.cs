using System;
using System.Runtime.InteropServices;

namespace ImgSoh
{
    public static class NativeMethods
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool NativeDeleteObject([In] IntPtr hObject);

        public static bool DeleteObject(IntPtr hObject)
        {
            return NativeDeleteObject(hObject);
        }
    }
}
