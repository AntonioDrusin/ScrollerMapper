using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using NUnit.Framework;
using ScrollerMapper.BitplaneRenderers;
using ScrollerMapperTests.Services;

namespace ScrollerMapperTests
{
    [TestFixture]
    public class BinaryBobRendererTest
    {
        private BinaryBobRenderer _renderer;
        private MockWriter _writer;
        private MockTransformer _transformer;

        [SetUp]
        public void SetUp()
        {
            _writer = new MockWriter();
            _transformer = new MockTransformer();
            _renderer = new BinaryBobRenderer(_writer, _transformer);
        }

        [Test]
        public void MakesCookies()
        {
            // 2 bobs 8 bits wide, 4 rows tall, 3 bpl
            var source = new byte[]
            {
                0xf0, 0x0f,
                0xaa, 0x01,
                0x03, 0x30,
                0x00, 0x11,

                0x10, 0x20,
                0x20, 0x40,
                0x10, 0x20,
                0x10, 0x20,

                0x01, 0x01,
                0x01, 0x01,
                0x01, 0x01,
                0x02, 0x03,
            };

            _transformer.SetArray(2, 4, source);

            _renderer.Render("test", new Bitmap(1,1), 8, 3);
            var result = _writer.Data;

            var expected = new byte[]
            {
                // First bob, interleaved.
                0xf0, 0x00, // Padded to word size
                0x10, 0x00,
                0x01, 0x00,

                0xaa, 0x00,
                0x20, 0x00,
                0x01, 0x00,

                0x03, 0x00,
                0x10, 0x00,
                0x01, 0x00,

                0x00, 0x00,
                0x10, 0x00,
                0x02, 0x00,

                // First bob cookie
                0xf1, 0x00,
                0xf1, 0x00,
                0xf1, 0x00,
                0xab, 0x00,
                0xab, 0x00,
                0xab, 0x00,
                0x13, 0x00,
                0x13, 0x00,
                0x13, 0x00,
                0x12, 0x00,
                0x12, 0x00,
                0x12, 0x00,

                // Second bob interleaved
                0x0f,  0x00,
                0x20,  0x00,
                0x01,  0x00,

                0x01, 0x00,
                0x40, 0x00,
                0x01, 0x00,

                0x30, 0x00,
                0x20, 0x00,
                0x01, 0x00,

                0x11, 0x00,
                0x20, 0x00,
                0x03, 0x00,

                // Second bob cookie
                0x2f, 0x00,
                0x2f, 0x00,
                0x2f, 0x00,
                0x41, 0x00,
                0x41, 0x00,
                0x41, 0x00,
                0x31, 0x00,
                0x31, 0x00,
                0x31, 0x00,
                0x33, 0x00,
                0x33, 0x00,
                0x33, 0x00

            };
            
            CollectionAssert.AreEqual(expected, result);
        }
    }
}