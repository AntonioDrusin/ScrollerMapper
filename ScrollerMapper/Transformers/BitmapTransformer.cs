using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScrollerMapper.Transformers
{
    internal interface IBitmapTransformer
    {
        void SetBitmap(Bitmap bitmap);
        int GetByteWidth();
        int GetHeight();
        byte[] GetBitplanes(int planeCount);
        byte[] GetInterleaved(int planeCount);
        void FlipColors(byte color1, byte color2);
    }

    internal class BitmapTransformer : IBitmapTransformer
    {
        private Bitmap _bitmap;
        private byte[] _source;

        public void SetBitmap(Bitmap bitmap)
        {
            _bitmap = bitmap;
            _source = _bitmap.GetImageBytes();
        }

        public int GetByteWidth()
        {
            return RoundUp(_bitmap.Width / 8);
        }

        private int RoundUp(int bytes)
        {
            var requiredWords = (bytes+1) / 2;
            return requiredWords * 2;
        }

        public int GetHeight()
        {
            return _bitmap.Height;
        }


        public void FlipColors(byte color1, byte color2)
        {
            for (int t = 0; t < _source.Length; t++)
            {
                var color = _source[t];
                if (color == color1) _source[t] = color2;
                else if (color == color2) _source[t] = color1;
            }
        }

        public byte[] GetBitplanes(int planeCount)
        {
            ValidateBitmap(_bitmap);
            var byteWidth = GetByteWidth();
            var width = _bitmap.Width;
            var height = GetHeight();

            var converted = new byte[byteWidth * height * planeCount];


            byte bplTest = 1;
            
            var convertIndex = 0;
            for (int bitplane = 0; bitplane < planeCount; bitplane++)
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
                                result = (byte) (result | (_source[(x*8) + b + y * width] & bplTest) >> bitplane << shift--);
                            }
                        }

                        converted[convertIndex++] = result;
                    }
                }

                bplTest = (byte) (bplTest << 1);
            }

            return converted;
        }

        public byte[] GetInterleaved(int planeCount)
        {
            var planes = GetBitplanes(planeCount);
            var interleaved = new byte[planes.Length];
            var height = GetHeight();
            var byteWidth = GetByteWidth();

            for (var x = 0; x < height; x++)
            {
                for (var p = 0; p < planeCount; p++)
                    Array.Copy(
                        planes,
                        x * byteWidth + p * height * byteWidth,
                        interleaved,
                        (x * byteWidth * planeCount) + (p * byteWidth),
                        byteWidth
                    );
            }
            return interleaved;
        }

        private static void ValidateBitmap(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new ConversionException("Only 8bpp format bitmaps are supported.");
            }
        }
    }
}