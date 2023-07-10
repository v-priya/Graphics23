using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Media.TextFormatting;

namespace GrayBMP {
   class PolyFill : Window {
      public PolyFill () {
         Width = 900; Height = 600;
         Left = 200; Top = 50;
         WindowStyle = WindowStyle.None;
         mBmp = new GrayBMP (Width, Height);

         Image image = new () {
            Stretch = Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Source = mBmp.Bitmap
         };
         RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
         RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);
         Content = image;
         Load ();
         using (new BlockTimer ("Fill Polygon"))
            Fill (255);
      }
      readonly GrayBMP mBmp;

      void Load () {
         using (var sr = new StreamReader (Assembly.GetExecutingAssembly ().GetManifestResourceStream ("GrayBMP.Res.leaf-fill.txt"))) {
            while (sr.ReadLine () is string line) {
               var pts = line.Split (' ').Select (int.Parse).ToList ();
               AddLine (pts[0], pts[1], pts[2], pts[3]);
            }
         }
      }

      public void AddLine (int x1, int y1, int x2, int y2) {
         if (y1 == y2) return;   // Don't add horizontal lines
         if (y1 > y2) (x1, y1, x2, y2) = (x2, y2, x1, y1);  // Keep Y1 smaller always
         mLines.Add ((x1, y1, x2, y2, GetSlope (x1, y1, x2, y2)));
      }

      /// <summary>Find the inverse slope</summary>
      double GetSlope (int x1, int y1, int x2, int y2) {
         var dy = y2 - y1; var dx = x2 - x1;
         return dy == 0 ? 1 : dx == 0 ? 0 : dx / dy;
      }

      void Fill (int color) {
         int min = mLines.Min (a => a.Y1), max = mLines.Max (a => a.Y1);
         for (var yScan = min + 0.5; yScan < max; yScan++) {
            mIntersections.Clear ();
            for (int i = 0; i < mLines.Count; i++) {
               var (X1, Y1, X2, Y2, Slope) = mLines[i];
               // Check to see if the line lies within the scan line range
               if ((Y1 <= yScan && Y2 > yScan) || (Y1 > yScan && Y2 <= yScan))
                  mIntersections.Add ((int)(X1 + Slope * (yScan - Y1)));
            }
            mIntersections = mIntersections.Order ().ToList ();
            for (int n = 0; n < mIntersections.Count; n += 2)
               mBmp.DrawHorizontalLine (mIntersections[n], mIntersections[n + 1], (int)yScan, color);
         }
      }

      List<(int X1, int Y1, int X2, int Y2, double Slope)> mLines = new (); 
      List<int> mIntersections = new ();
   }
}
