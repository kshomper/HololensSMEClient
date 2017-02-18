﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace ARTAPclient
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class VideoStreamWindow : Window
    {
        #region Private Fields

        /// <summary>
        /// Aspect ratio of screen
        /// </summary>
        private double _aspectRatio;

        /// <summary>
        /// Is the height being adjusted
        /// </summary>
        private bool? _adjustingHeight = null;

        #endregion

        #region Constuctor

        public VideoStreamWindow(string ip, string user, string password)
        {
            InitializeComponent();
            this.SourceInitialized += Window_SourceInitialized;

            string connectionURL = String.Format("http://{0}:{1}@{2}/api/holographic/stream/live_low.mp4?holo=true&pv=true&mic=false&loopback=false",
                user, password, ip);
            MediaElement.Source = new Uri(connectionURL);
        }

        #endregion

        #region Public Methods

        public BitmapImage CaptureScreen()
        {
            WriteableBitmap bmp = MediaElement.GetCurrentFrame();
            return ConvertWriteableBitmap(bmp);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Converts a WriteableBitmap image to a BitmapImage
        /// Code used from:
        /// http://stackoverflow.com/questions/14161665/how-do-i-convert-a-writeablebitmap-object-to-a-bitmapimage-object-in-wpf
        /// </summary>
        /// <param name="wbm">WriteableBitmap</param>
        /// <returns>BitmapImage</returns>
        private BitmapImage ConvertWriteableBitmap(WriteableBitmap wbm)
        {
            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                stream.Seek(0, SeekOrigin.Begin);
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
        }

        #endregion

        #region Event Handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MediaElement.Play();
        }

        #endregion

        #region Fixed Aspect Ratio Components

        ///
        /// Code used for fixed aspect ratio borrowed from here:
        /// http://stackoverflow.com/questions/2471867/resize-a-wpf-window-but-maintain-proportions
        ///

        internal enum SWP
        {
            NOMOVE = 0x0002
        }
        internal enum WM
        {
            WINDOWPOSCHANGING = 0x0046,
            EXITSIZEMOVE = 0x0232,
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static System.Windows.Point GetMousePosition() // mouse position relative to screen
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new System.Windows.Point(w32Mouse.X, w32Mouse.Y);
        }

        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            HwndSource hwndSource = (HwndSource)HwndSource.FromVisual((Window)sender);
            hwndSource.AddHook(DragHook);

            _aspectRatio = this.Width / this.Height;
        }

        private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.WINDOWPOSCHANGING:
                    {
                        WINDOWPOS pos = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                        if ((pos.flags & (int)SWP.NOMOVE) != 0)
                            return IntPtr.Zero;

                        Window wnd = (Window)HwndSource.FromHwnd(hwnd).RootVisual;
                        if (wnd == null)
                            return IntPtr.Zero;

                        // determine what dimension is changed by detecting the mouse position relative to the 
                        // window bounds. if gripped in the corner, either will work.
                        if (!_adjustingHeight.HasValue)
                        {
                            System.Windows.Point p = GetMousePosition();

                            double diffWidth = Math.Min(Math.Abs(p.X - pos.x), Math.Abs(p.X - pos.x - pos.cx));
                            double diffHeight = Math.Min(Math.Abs(p.Y - pos.y), Math.Abs(p.Y - pos.y - pos.cy));

                            _adjustingHeight = diffHeight > diffWidth;
                        }

                        if (_adjustingHeight.Value)
                            pos.cy = (int)(pos.cx / _aspectRatio); // adjusting height to width change
                        else
                            pos.cx = (int)(pos.cy * _aspectRatio); // adjusting width to heigth change

                        Marshal.StructureToPtr(pos, lParam, true);
                        handled = true;
                    }
                    break;
                case WM.EXITSIZEMOVE:
                    _adjustingHeight = null; // reset adjustment dimension and detect again next time window is resized
                    break;
            }

            return IntPtr.Zero;
        }

        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            var windows = Application.Current.Windows;
            foreach (var item in windows)
            {
                (item as Window).Close();
            }
            Application.Current.Shutdown();
        }
    }
}
