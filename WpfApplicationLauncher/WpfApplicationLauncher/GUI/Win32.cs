using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApplicationLauncher.GUI
{
    public static class Win32
    {
        public class Win32Window : System.Windows.Forms.IWin32Window
        {
            IntPtr _handle;

            public Win32Window(IntPtr handle)
            {
                _handle = handle;
            }

            #region IWin32Window Members

            IntPtr System.Windows.Forms.IWin32Window.Handle
            {
                get
                {
                    return _handle;
                }
            }

            #endregion
        }

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr window, int index, int value);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr window, int index);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn
        (
        int nLeftRect, // x-coordinate of upper-left corner
        int nTopRect, // y-coordinate of upper-left corner
        int nRightRect, // x-coordinate of lower-right corner
        int nBottomRect, // y-coordinate of lower-right corner
        int nWidthEllipse, // height of ellipse
        int nHeightEllipse // width of ellipse
        );

        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;    // 'Large icon
        public const uint SHGFI_SMALLICON = 0x1;    // 'Small icon

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                                    uint dwFileAttributes,
                                                    ref SHFILEINFO psfi,
                                                    uint cbSizeFileInfo,
                                                    uint uFlags);



        public static Icon GetIcon(string filePath)
        {
            Icon retVal = null;

            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImgSmall = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON);

            if (hImgSmall != IntPtr.Zero)
            {
                System.Drawing.Icon thisIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
                if (!thisIcon.Size.IsEmpty)
                {
                    retVal = thisIcon.Clone() as Icon;
                    DestroyIcon(shinfo.hIcon);
                }
            }

            return retVal;
        }

        public static ImageSource AsImageSource(this Icon icon)
        {
            ImageSource retVal = null;

            if (icon != null)
            {
                retVal = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            return retVal;
        }

        public static ImageSource GetImageSource(string filePath)
        {
            return GetIcon(filePath).AsImageSource();
        }

        // alt tab code from http://bytes.com/forum/thread442047.html
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int SW_RESTORE = 9;

        public static void HideFromAltTab(Window w)
        {
            WindowInteropHelper wint = new WindowInteropHelper(w);
            HideFromAltTab(wint.Handle);
        }

        public static void HideFromAltTab(IntPtr Handle)
        {
            int currentStyles = GetWindowLong(Handle, GWL_EXSTYLE);
            int newStyles = currentStyles | WS_EX_TOOLWINDOW;
            int ret = SetWindowLong(Handle, GWL_EXSTYLE, newStyles);
        }

        #region Window styles

        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        #endregion
    }
}
