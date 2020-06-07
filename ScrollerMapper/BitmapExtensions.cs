using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using ScrollerMapper.Transformers;

namespace ScrollerMapper
{
    internal static class BitmapExtensions
    {
        public static byte[] GetImageBytes(this Bitmap sourceImage)
        {
            if (sourceImage == null)
                throw new ArgumentNullException("sourceImage", "Source image is null!");
            var width = sourceImage.Width;
            var height = sourceImage.Height;

            var sourceData = sourceImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                sourceImage.PixelFormat);

            var stride = sourceData.Stride;


            var actualDataWidth = (Image.GetPixelFormatSize(sourceImage.PixelFormat) * width + 7) / 8;

            var sourcePos = sourceData.Scan0.ToInt64();
            var destPos = 0;

            var data = new byte[actualDataWidth * height];
            for (var y = 0; y < height; ++y)
            {
                Marshal.Copy(new IntPtr(sourcePos), data, destPos, actualDataWidth);
                sourcePos += stride;
                destPos += actualDataWidth;
            }

            sourceImage.UnlockBits(sourceData);
            return data;
        }

        public static void SetImageBytes(this Bitmap destinationImage, byte[] bytes)
        {
            var width = destinationImage.Width;
            var height = destinationImage.Height;
            var data = destinationImage.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                destinationImage.PixelFormat);
            var stride = data.Stride;
            var actualDataWidth = (Image.GetPixelFormatSize(destinationImage.PixelFormat) * width + 7) / 8;

            var sourcePos = 0;
            var destPos = data.Scan0;

            for (var y = 0; y < destinationImage.Height; y++)
            {
                Marshal.Copy(bytes, sourcePos, destPos, actualDataWidth);
                sourcePos += actualDataWidth;
                destPos = IntPtr.Add(destPos, stride);
            }

            destinationImage.UnlockBits(data);
        }

        public static void ValidatePalette(this ColorPalette palette, string name, int maxValues)
        {
            var colors = palette.Entries.Take(maxValues).Select((_, i) => new
            {
                Color = _,
                ConvertedColor = Color.FromArgb(_.A, _.R & 0xf0, _.G & 0xf0, _.B & 0xf0),
                Index = i
            });
            var hash = new HashSet<Color>();

            Console.WriteLine("Validating palette " + name);
            foreach (var color in colors)
            {
                var duplicate = !hash.Add(color.ConvertedColor) ? "duplicate": "";
                Console.WriteLine($"{color.Index}: ${color.Color.R,2:X2}{color.Color.G,2:X2}{color.Color.B,2:X2} => ${color.ConvertedColor.R>>4:X}{color.ConvertedColor.G>>4:X}{color.ConvertedColor.B>>4:X} {duplicate}");
            }

            Console.WriteLine();
        }
    }
}