using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using NUnit.Framework;
using ScrollerMapper.Transformers;

namespace ScrollerMapperTests
{
    [TestFixture]
    public class BitplaneTransformerTest
    {
        private BitmapTransformer _transformer;

        [SetUp]
        public void Setup()
        {
            _transformer = new BitmapTransformer();
        }

        [Test]
        public void RoundsWidthToWord()
        {
            var bitmap = CreateBitmap(23);
            _transformer.SetBitmap(bitmap);

            var result = _transformer.GetByteWidth();

            Assert.AreEqual(2, result);
        }


        [Test]
        public void ReturnsHeight()
        {
            var bitmap = CreateBitmap(23);
            _transformer.SetBitmap(bitmap);

            var result = _transformer.GetHeight();

            Assert.AreEqual(10, result);
        }

        [Test]
        public void ConvertsFourBitplanesCorrectly()
        {
            var source = GetSource();
            var bitmap = CreateBitmap(10, 4, source);
            _transformer.SetBitmap(bitmap);

            var result = _transformer.GetBitplanes(4);

            var expected = new byte[]
            {

                0b11110001, 0b00000000, // 1st bit
                0b00000000, 0b00000000,
                0b01011011, 0b11000000,
                0b00100101, 0b11000000,


                0b11100010, 0b00000000, // 2nd bit
                0b00000000, 0b00000000,
                0b00011001, 0b11000000,
                0b01100111, 0b11000000,

                0b11000100, 0b00000000, // 3rd bit
                0b00000000, 0b00000000,
                0b00101010, 0b00000000,
                0b10010100, 0b00000000,

                0b10001000, 0b00000000, // HIGH bit
                0b11111111, 0b11000000,
                0b10101000, 0b00000000,
                0b00010110, 0b00000000,
            };

            VerifyResults(expected, result, 4);
        }

        [Test]
        public void ConvertsOneBitplaneCorrectly()
        {
            var source = GetSource();
            var bitmap = CreateBitmap(10, 4, source);
            _transformer.SetBitmap(bitmap);

            var result = _transformer.GetBitplanes(1);

            var expected = new byte[]
            {
                0b11110001, 0b00000000,
                0b00000000, 0b00000000,
                0b01011011, 0b11000000,
                0b00100101, 0b11000000,
            };

            VerifyResults(expected, result, 1);
        }


        private static void VerifyResults(byte[] expected, byte[] result, int planes)
        {
            for (int plane = 0; plane < planes; plane++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        var expectedString = Convert.ToString(expected[plane * 8 + y * 2 + x], 2).PadLeft(8, '0');
                        var resultString = Convert.ToString(result[plane * 8 + y * 2 + x], 2).PadLeft(8, '0');
                        Assert.AreEqual(expectedString, resultString, $"Must match plane {plane}, y {y}, x {x}");
                    }
                }
            }
        }

        private static byte[] GetSource()
        {
            var source = new byte[]
            {
                0b00001111, 0b00000111, 0b00000011, 0b00000001, 0b00001000, 0b00000100, 0b00000010, 0b00000001,
                0b01100000, 0b10010000,

                0b00011000, 0b00011000, 0b00011000, 0b00011000, 0b00011000, 0b00011000, 0b00011000, 0b00011000,
                0b00011000, 0b00111000,

                0b00001000, 0b00000001, 0b00001100, 0b00000011, 0b00001111, 0b00000000, 0b00000101, 0b00000011,
                0b00000011, 0b00000011,

                0b00000100, 0b00000010, 0b00000011, 0b00001100, 0b00000000, 0b00001111, 0b00001010, 0b00000011,
                0b00000011, 0b00000011,
            };
            return source;
        }

        private static Bitmap CreateBitmap(int width, int height = 10, byte[] source = null)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly,
                PixelFormat.Format8bppIndexed);
            byte[] bytes = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);


            if (source == null)
            {
                for (int x = 0; x < data.Stride; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        bytes[y * data.Stride + x] = 0;
                    }
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        bytes[y * data.Stride + x] = source[y * width + x];
                    }
                }
            }

            Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }
    }
}