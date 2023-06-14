using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static System.Math;

namespace A25;

class MyWindow : Window {
   public MyWindow () {
      Width = 800; Height = 600;
      Left = 50; Top = 50;
      WindowStyle = WindowStyle.ToolWindow;
      Image image = new Image () {
         Stretch = Stretch.None,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
      RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);

      mBmp = new WriteableBitmap ((int)Width, (int)Height,
         96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      image.Source = mBmp;
      Content = image;
      image.MouseDown += OnMouseDown;
   }

   void OnMouseDown (object sender, MouseButtonEventArgs e) {
      var pos = e.GetPosition (this);
      mStart = mEnd == Zero ? pos : mEnd;
      mEnd = pos;
      DrawLine ((int)mStart.X, (int)mStart.Y, (int)mEnd.X, (int)mEnd.Y);
   }
   Point mStart = new (), mEnd = Zero;
   static Point Zero = new (0, 0);

   void DrawLine (int x1, int y1, int x2, int y2) {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         int dx = Abs (x2 - x1), dy = Abs (y2 - y1);
         int stepX = x1 < x2 ? 1 : -1, stepY = y1 < y2 ? 1 : -1;
         var rect = new Int32Rect (stepX > 0 ? x1 : x2, stepY > 0 ? y1 : y2, dx + 1, dy + 1);
         dy = -dy;
         int err = dx + dy;
         while (true) {
            SetPixel (x1, y1, 255);
            if (x1 == x2 && y1 == y2) break;
            int delta = 2 * err;
            if (delta >= dy) {
               if (x1 == x2) break;
               err += dy;
               x1 += stepX;
            }
            if (delta <= dx) {
               if (y1 == y2) break;
               err += dx;
               y1 += stepY;
            }
         }
         mBmp.AddDirtyRect (rect);
      } finally {
         mBmp.Unlock ();
      }
   }

   void DrawMandelbrot (double xc, double yc, double zoom) {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         int dx = mBmp.PixelWidth, dy = mBmp.PixelHeight;
         double step = 2.0 / dy / zoom;
         double x1 = xc - step * dx / 2, y1 = yc + step * dy / 2;
         for (int x = 0; x < dx; x++) {
            for (int y = 0; y < dy; y++) {
               Complex c = new Complex (x1 + x * step, y1 - y * step);
               SetPixel (x, y, Escape (c));
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, dx, dy));
      } finally {
         mBmp.Unlock ();
      }
   }

   byte Escape (Complex c) {
      Complex z = Complex.Zero;
      for (int i = 1; i < 32; i++) {
         if (z.NormSq > 4) return (byte)(i * 8);
         z = z * z + c;
      }
      return 0;
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         try {
            mBmp.Lock ();
            mBase = mBmp.BackBuffer;
            var pt = e.GetPosition (this);
            int x = (int)pt.X, y = (int)pt.Y;
            SetPixel (x, y, 255);
            mBmp.AddDirtyRect (new Int32Rect (x, y, 1, 1));
         } finally {
            mBmp.Unlock ();
         }
      }
   }

   void DrawGraySquare () {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         for (int x = 0; x <= 255; x++) {
            for (int y = 0; y <= 255; y++) {
               SetPixel (x, y, (byte)x);
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, 256, 256));
      } finally {
         mBmp.Unlock ();
      }
   }

   void SetPixel (int x, int y, byte gray) {
      unsafe {
         var ptr = (byte*)(mBase + y * mStride + x);
         *ptr = gray;
      }
   }

   WriteableBitmap mBmp;
   int mStride;
   nint mBase;
}

internal class Program {
   [STAThread]
   static void Main (string[] args) {
      Window w = new MyWindow ();
      w.Show ();
      Application app = new Application ();
      app.Run ();
   }
}
