using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Martini
{
    public static class Utils
    {
        public delegate float HeightFormula(int r, int g, int b);

        public static float[] MapboxTerrainToGrid(Bitmap png, HeightFormula formula)
        {
            int tileSize = png.Width;
            int gridSize = tileSize + 1;

            float[] terrain = new float[gridSize * gridSize];

            // Lock the bitmap to access the pixel data efficiently
            var rect = new Rectangle(0, 0, tileSize, tileSize);
            BitmapData bmpData = png.LockBits(rect, ImageLockMode.ReadOnly, png.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytesPerPixel = Image.GetPixelFormatSize(png.PixelFormat) / 8;
            int stride = bmpData.Stride;

            unsafe
            {
                for (int y = 0; y < tileSize; y++)
                {
                    byte* pixelBase = (byte*)ptr + y * stride;
                    for (int x = 0; x < tileSize; x++)
                    {
                        int r = pixelBase[x * bytesPerPixel + 2];
                        int g = pixelBase[x * bytesPerPixel + 1];
                        int b = pixelBase[x * bytesPerPixel];    
                        terrain[y * gridSize + x] = formula(r, g, b);
                    }

                    // Backfill right border
                    terrain[y * gridSize + gridSize - 1] = terrain[y * gridSize + gridSize - 2];
                }

                // Backfill bottom border (duplicate the last row)
                for (int x = 0; x < gridSize; x++)
                {
                    terrain[(gridSize - 1) * gridSize + x] = terrain[(gridSize - 2) * gridSize + x];
                }
            }

            png.UnlockBits(bmpData);

            return terrain;
        }
    }
}
