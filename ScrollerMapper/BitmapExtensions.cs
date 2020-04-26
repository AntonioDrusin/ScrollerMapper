using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScrollerMapper
{
    internal static class BitmapExtensions
    {
        public static Byte[] GetImageBytes(this Bitmap sourceImage)
        {
            if (sourceImage == null)
                throw new ArgumentNullException("sourceImage", "Source image is null!");
            Int32 width = sourceImage.Width;
            Int32 height = sourceImage.Height;

            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                sourceImage.PixelFormat);
            var stride = sourceData.Stride;
            Int32 actualDataWidth = ((Image.GetPixelFormatSize(sourceImage.PixelFormat) * width) + 7) / 8;
            Int64 sourcePos = sourceData.Scan0.ToInt64();
            Int32 destPos = 0;

            var data = new Byte[actualDataWidth * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Marshal.Copy(new IntPtr(sourcePos), data, destPos, actualDataWidth);
                sourcePos += stride;
                destPos += actualDataWidth;
            }

            stride = actualDataWidth;
            sourceImage.UnlockBits(sourceData);
            return data;
        }
    }
}