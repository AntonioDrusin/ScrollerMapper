using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;

namespace ScrollerMapper.ImageRenderers
{
    internal class BitmapTransformer
    {
        public int GetByteWidth(Bitmap bitmap)
        {
            return RoundUp(bitmap.Width / 8);
        }

        private int RoundUp(int bytes)
        {
            var requiredWords = ((bytes / 2) + 1);
            return requiredWords * 2;
        }

        public int GetHeight(Bitmap bitmap)
        {
            return bitmap.Height;
        }

        public byte[] GetBitplanes(Bitmap bitmap, int bitplaneCount)
        {
            ValidateBitmap(bitmap);
            var byteWidth = GetByteWidth(bitmap);
            var width = bitmap.Width;
            var height = GetHeight(bitmap);

            var converted = new byte[byteWidth * height * bitplaneCount];


            byte bplTest = (byte) (1 << (bitplaneCount - 1));
            
            var source = bitmap.GetImageBytes();

            var convertIndex = 0;
            for (int bitplane = bitplaneCount-1; bitplane >= 0; bitplane--)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < byteWidth; x++)
                    {

                        byte shift = 7;
                        byte result = 0;

                        for (int b = 0; b < Math.Min(8, width - (x * 8)); b++)
                        {
                            if ((x*8 + b) < width)
                            {
                                result = (byte) (result | (source[(x*8) + b + y * width] & bplTest) >> bitplane << shift--);
                            }
                        }

                        converted[convertIndex++] = result;
                    }
                }

                bplTest = (byte) (bplTest >> 1);
            }

            return converted;
        }

        private static void ValidateBitmap(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("Only 8bpp format is supported.");
            }
        }
    }
}