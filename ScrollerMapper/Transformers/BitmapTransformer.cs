﻿using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScrollerMapper.Transformers
{
    internal interface IBitmapTransformer
    {
        void SetBitmap(Bitmap bitmap);
        int GetByteWidth();
        int GetHeight();
        byte[] GetBitplanes(int bitplaneCount);
    }

    internal class BitmapTransformer : IBitmapTransformer
    {
        private Bitmap _bitmap;

        public void SetBitmap(Bitmap bitmap)
        {
            _bitmap = bitmap;
        }

        public int GetByteWidth()
        {
            return RoundUp(_bitmap.Width / 8);
        }

        private int RoundUp(int bytes)
        {
            var requiredWords = ((bytes / 2) + 1);
            return requiredWords * 2;
        }

        public int GetHeight()
        {
            return _bitmap.Height;
        }

        public byte[] GetBitplanes(int bitplaneCount)
        {
            ValidateBitmap(_bitmap);
            var byteWidth = GetByteWidth();
            var width = _bitmap.Width;
            var height = GetHeight();

            var converted = new byte[byteWidth * height * bitplaneCount];


            byte bplTest = (byte) (1 << (bitplaneCount - 1));
            
            var source = _bitmap.GetImageBytes();

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
                throw new ConversionException("Only 8bpp format bitmaps are supported.");
            }
        }
    }
}