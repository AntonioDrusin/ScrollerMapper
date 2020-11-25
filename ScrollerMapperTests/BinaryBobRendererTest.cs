using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using NUnit.Framework;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;
using ScrollerMapperTests.Services;

namespace ScrollerMapperTests
{
    [TestFixture]
    public class BinaryBobRendererTest
    {
        private BinaryBobRenderer _renderer;
        private MockWriter _writer;
        private BitmapTransformer _transformer;

        [SetUp]
        public void SetUp()
        {
            _writer = new MockWriter();
            _transformer = new BitmapTransformer();
            _renderer = new BinaryBobRenderer(_writer, _transformer);
        }

        [Test]
        public void MakesCookies()
        {
            // 2 bobs 8 bits wide, 4 rows tall, 3 bpl
            var source = new byte[] // Interleaved bpl format, just for understanding
            {
                0x00, 0xf0,
                0x00, 0x20,
                0x00, 0x01,

                0xaa, 0x01,
                0x20, 0x40,
                0x01, 0x01,

                0x03, 0x30,
                0x10, 0x20,
                0x01, 0x01,

                0x00, 0x00,
                0x00, 0x00,
                0x00, 0x00,
            };

            var bitmapSource = new byte[] // 8 bit indexed
            {
                0,0,0,0,0,0,0,0,  1,1,3,1,0,0,0,4,
                1,0,3,0,1,0,1,4,  0,2,0,0,0,0,0,5,
                0,0,0,2,0,0,1,5,  0,0,3,1,0,0,0,4,
                0,0,0,0,0,0,0,0,  0,0,0,0,0,0,0,0, 

            };

            IntPtr unmanageBytes = Marshal.AllocHGlobal(16*4);
            Marshal.Copy(bitmapSource, 0, unmanageBytes, bitmapSource.Length);
            var bitmap = new Bitmap(16, 4, 16, PixelFormat.Format8bppIndexed, unmanageBytes);

            _transformer.SetBitmap(bitmap);
            var interleaved = _transformer.GetInterleaved(3);
            CollectionAssert.AreEqual(source, interleaved);


            _renderer.Render("test", bitmap, new BobDefinition {Width = 8} , 3, false);
            var result = _writer.Data;

            var expected = new byte[]
            {
                0x00, 0x01, // WORD WIDTH of the bobs               //0
                0x00, 0x02, // WORD Number of bobs                  //2

                // First bob metadata
                0x00,0x00,0x00, 36,                                         // 4  Bob offset
                0x00,0x00,0x00, 48, // 8 Mask offset
                0x00, 02, // 12 Line count
                0x0, 0x1, // 14 Y adjustment
                0x00,0x00,   // 16 modulo
                0x01,0x82,  // 18 bltsize

                // Second bob metadata
                0x00,0x00,0x00, 60,                                         // 20
                0x00,0x00,0x00, 78,
                0x00, 03,
                0x00, 0x00,
                0x00, 0x00,  // modulo
                0x02, 0x42,  // blt size

                // First bob, interleaved.
                // Padded to word size // Zero lines removed

                0xaa, 0x00,                                         //36
                0x20, 0x00,
                0x01, 0x00,

                0x03, 0x00,                                         //42
                0x10, 0x00,
                0x01, 0x00,

                // First bob cookie
                0xab, 0x00,                                         //48
                0xab, 0x00,
                0xab, 0x00,

                0x13, 0x00,                                         //54
                0x13, 0x00,
                0x13, 0x00,

                // Second bob interleaved
                0xf0,  0x00,                                         //60
                0x20,  0x00,
                0x01,  0x00,

                0x01, 0x00,                                         //66
                0x40, 0x00,
                0x01, 0x00,

                0x30, 0x00,                                         //72
                0x20, 0x00,
                0x01, 0x00,

                // Second bob cookie
                0xf1, 0x00,                                         //78
                0xf1, 0x00,
                0xf1, 0x00,

                0x41, 0x00,                                         //84
                0x41, 0x00,
                0x41, 0x00,

                0x31, 0x00,                                         //90
                0x31, 0x00,
                0x31, 0x00

            };
            
            CollectionAssert.AreEqual(expected, result);
            bitmap.Dispose();
            Marshal.FreeHGlobal(unmanageBytes);

        }
    }
}