using System.Drawing;
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
                0x00, 0xf0,
                0xaa, 0x01,
                0x03, 0x30,
                0x00, 0x00,

                0x00, 0x20,
                0x20, 0x40,
                0x10, 0x20,
                0x00, 0x00,

                0x00, 0x01,
                0x01, 0x01,
                0x01, 0x01,
                0x00, 0x00,
            };

            _transformer.SetArray(2, 4, source);

            _renderer.Render("test", new Bitmap(1,1), 8, 3);
            var result = _writer.Data;

            var expected = new byte[]
            {
                0x00, 0x01, // WORD WIDTH of the bobs               //0
                0x00, 0x02, // WORD Number of bobs                  //2
                
                // First bob metadata
                0x00, 20,                                         // 4  Bob offset
                0x00, 32, // Mask offset
                0x00, 02, // Line count
                0x0, 0x1, // Y adjustment

                // Second bob metadata
                0x00, 44,                                         //12
                0x00, 62,
                0x00, 03,
                0x00, 0x00,

                // First bob, interleaved.
                // Padded to word size // Zero lines removed

                0xaa, 0x00,                                         //20
                0x20, 0x00,
                0x01, 0x00,

                0x03, 0x00,                                         //26
                0x10, 0x00,
                0x01, 0x00,

                // First bob cookie
                0xab, 0x00,                                         //32
                0xab, 0x00,
                0xab, 0x00,

                0x13, 0x00,                                         //38
                0x13, 0x00,
                0x13, 0x00,

                // Second bob interleaved
                0xf0,  0x00,                                         //44
                0x20,  0x00,
                0x01,  0x00,

                0x01, 0x00,                                         //50
                0x40, 0x00,
                0x01, 0x00,

                0x30, 0x00,                                         //56
                0x20, 0x00,
                0x01, 0x00,

                // Second bob cookie
                0xf1, 0x00,                                         //62
                0xf1, 0x00,
                0xf1, 0x00,

                0x41, 0x00,                                         //68
                0x41, 0x00,
                0x41, 0x00,

                0x31, 0x00,                                         //74
                0x31, 0x00,
                0x31, 0x00

            };
            
            CollectionAssert.AreEqual(expected, result);
        }
    }
}