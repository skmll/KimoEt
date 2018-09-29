using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace KimoEt.ProcessWindow
{

    class ProcessWindowManager
    {
        private static ProcessWindowManager instance = null;
        private static readonly object padlock = new object();

        public static ProcessWindowManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new ProcessWindowManager();
                    }
                    return instance;
                }
            }
        }

        private string procName;
        private WinEventDelegate winDelegate;
        private Process proc;
        private IntPtr procWindowHandle;
        private MainWindow windowToBind;
        private bool isWindowStateNormal;

        private ProcessWindowManager()
        {
        }

        public void Init(string processName)
        {
            proc = Process.GetProcessesByName(processName)[0];
            procName = processName;
            procWindowHandle = proc.MainWindowHandle;
            winDelegate = TargetMoved;

            User32.SetWinEventHook(User32.EVENT_OBJECT_LOCATIONCHANGE, User32.EVENT_OBJECT_LOCATIONCHANGE, IntPtr.Zero, winDelegate, (uint)proc.Id,
                User32.GetWindowThreadProcessId(procWindowHandle, IntPtr.Zero), User32.WINEVENT_OUTOFCONTEXT | User32.WINEVENT_SKIPOWNPROCESS | User32.WINEVENT_SKIPOWNTHREAD);
        }

        public void BindLocationToThisWindow(MainWindow mainWindow)
        {
            this.windowToBind = mainWindow;
        }

        private void TargetMoved(IntPtr hWinEventHook, uint eventType, IntPtr lParam, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (windowToBind != null && Application.Current != null)
            {
                IntPtr? windowHandle = null;
                try
                {
                    windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                } catch (Exception) { /* do nothing*/}

                IntPtr foregroundWindowHandle = User32.GetForegroundWindow();

                if ((foregroundWindowHandle != procWindowHandle && foregroundWindowHandle != windowHandle) || User32.IsIconic(procWindowHandle))
                {
                    isWindowStateNormal = false;
                    //if (MainWindow.toDebug != null)
                    //    MainWindow.toDebug.Text = "isWindowStateNormal = false";
                    windowToBind.Visibility = Visibility.Hidden;
                }
                else
                {
                    isWindowStateNormal = true;
                    //if (MainWindow.toDebug != null)
                    //    MainWindow.toDebug.Text = "isWindowStateNormal = true";
                    try
                    {
                        windowToBind.Visibility = Visibility.Visible;
                    } catch { /* do nothing */ }

                    var newLocation = GetWindowRect();
                    windowToBind.Left = newLocation.Left / MainWindow.ScaleFactorX;
                    windowToBind.Top = newLocation.Top / MainWindow.ScaleFactorY;
                    windowToBind.Width = newLocation.Right / MainWindow.ScaleFactorX - newLocation.Left / MainWindow.ScaleFactorX;
                    windowToBind.Height = newLocation.Bottom / MainWindow.ScaleFactorY - newLocation.Top / MainWindow.ScaleFactorY;

                    wToBindLeft = newLocation.Left;
                    wToBindTop = newLocation.Top;
                }
            }
        }

        public void ForceFocus(float opacity = 0.3f)
        {
            // By setting a non-transparent background color, we can keep focus on our window
            if (windowToBind == null) return;
            windowToBind.Background = new SolidColorBrush(Colors.Black) { Opacity = opacity };
        }

        public void ReleaseFocus()
        {
            if (windowToBind == null) return;
            windowToBind.Background = System.Windows.Media.Brushes.Transparent;
        }

        public bool IsWindowStateNormal()
        {
            return isWindowStateNormal;
        }

        public Bitmap GetWindowBitmap(bool shouldBringWindowForward)
        {
            if (shouldBringWindowForward)
            {
                BringWindowForward();
            }

            var rect = GetWindowRect();

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);

            return bmp;
        }

        int wToBindLeft;
        int wToBindTop;
        public Bitmap GetWindowAreaBitmap(RECT area, bool shouldBringWindowForward)
        {
            if (shouldBringWindowForward)
            {
                BringWindowForward();
            }

            var bmp = new Bitmap(area.Width, area.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.InterpolationMode = InterpolationMode.Bicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CopyFromScreen(wToBindLeft + area.Left, wToBindTop + area.Top, 0, 0, area.Size, CopyPixelOperation.SourceCopy);

            return bmp;
        }

        public RECT GetWindowRect()
        {
            var rect = new RECT();
            User32.GetWindowRect(procWindowHandle, ref rect);

            var clientRect = new RECT();
            User32.GetClientRect(procWindowHandle, ref clientRect);

            var processWindowTitleHeight = (rect.Bottom - rect.Top) - (clientRect.Bottom - clientRect.Top);
            rect.Top += processWindowTitleHeight - (int)((MainWindow.ScaleFactorY - 1) * 6);
            rect.Left += (int)((MainWindow.ScaleFactorX - 1) * 6);

            return rect;
        }

        public void BringWindowForward()
        {
            if (User32.GetForegroundWindow() != procWindowHandle)
            {
                Console.WriteLine("Bringing window forward");
                User32.SetForegroundWindow(procWindowHandle);
                User32.ShowWindow(procWindowHandle, User32.SW_RESTORE);
                Thread.Sleep(200);
            }
            else
            {
                Console.WriteLine("Window was already forward");
            }
        }

        public void BringOurWindowForward()
        {
            IntPtr? windowHandle = null;
            try
            {
                windowHandle = new WindowInteropHelper(windowToBind).Handle;
            }
            catch (Exception) { /* do nothing*/}

            if (windowHandle == null)
                return;

            User32.SetForegroundWindow((IntPtr)windowHandle);
            User32.ShowWindow(procWindowHandle, User32.SW_RESTORE);
            Thread.Sleep(200);
        }

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
                        IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        //DLL's and stuff
        private class User32
        {
            internal const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
            internal const int HT_CAPTION = 0x2;
            internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;
            internal const uint WINEVENT_SKIPOWNPROCESS = 0x0002;
            internal const uint WINEVENT_SKIPOWNTHREAD = 0x0001;
            internal const int WM_NCLBUTTONDOWN = 0xA1;

            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetClientRect(IntPtr hWnd, ref RECT rect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsIconic(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern bool IsZoomed(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

            [DllImport("user32.dll")]
            internal static extern int SetForegroundWindow(IntPtr hWnd);

            internal const int SW_RESTORE = 9;

            [DllImport("user32.dll")]
            internal static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

            [DllImport("user32.dll")]
            internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
        }
    }
}
