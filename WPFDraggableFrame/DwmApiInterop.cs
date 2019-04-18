using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Touchbar
{
    public struct DwmBlurbehind
    {
        public int dwFlags;
        public bool fEnable;
        public string hRgnBlur;
        public bool fTransitionOnMaximized;
    }
    public static class DwmApiInterop
    {
        public const int DWM_BB_TRANSITIONMAXIMIZED = 0x00000004;
        public const int DWM_BB_ENABLE = 0x00000001;
        public const int DWM_BB_BLURREGION = 0x00000002;

        public static bool IsCompositionEnabled()
        {
            bool isEnabled = false;
            NativeMethods.DwmIsCompositionEnabled(ref isEnabled);
            return isEnabled;
        }

        public static int ExtendFrameIntoClientArea(IntPtr hWnd, ref DwmBlurbehind blurBehind)
        {
            return NativeMethods.DwmEnableBlurBehindWindow(hWnd, ref blurBehind);
        }

    }

    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        [DllImport("dwmapi.dll")]
        internal static extern void DwmIsCompositionEnabled(ref bool isEnabled);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmEnableBlurBehindWindow(IntPtr hWnd, ref DwmBlurbehind blurBehind);

    }
}