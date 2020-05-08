using System.Drawing;
using Moq;
using NUnit.Framework;
using ScrollerMapper;
using ScrollerMapper.TileRenderers;
using ScrollerMapper.Transformers;
using ScrollerMapperTests.Services;

namespace ScrollerMapperTests
{
    [TestFixture]
    public class BinaryTileRendererTest
    {
        private Mock<IBitmapTransformer> _transformerMock;
        private BinaryTileRenderer _renderer;
        private MockWriter _writer;

        [SetUp]
        public void SetUp()
        {
            _transformerMock = new Mock<IBitmapTransformer>();
            _writer = new MockWriter();
            var options = new Options {PlaneCount = 2};
            _renderer = new BinaryTileRenderer(options, _transformerMock.Object, _writer);
        }

        [Test]
        public void ReorganizesTilesSequentially()
        {
            var planesData = new byte[]
            {
                // BPL1 
                11, 12, 21, 22,
                13, 14, 23, 24,

                31, 32, 41, 42,
                33, 34, 43, 44,

                // BPL2 
                51, 52, 61, 62,
                53, 54, 63, 64,

                71, 72, 81, 82,
                73, 74, 83, 84,
            };

            _transformerMock.Setup(f => f.GetHeight()).Returns(4);
            _transformerMock.Setup(f => f.GetByteWidth()).Returns(4);
            _transformerMock.Setup(f => f.GetBitplanes(It.Is<int>(n => n == 2))).Returns(planesData);

            _renderer.Render("data", new Bitmap(16 * 2, 4), 16, 2);

            var expected = new byte[]
            {
                // Adds one blank tile so 0 maps here.
                00, 00,
                00, 00,
                00, 00,
                00, 00,

                11, 12, // Tile 1 BPL 1
                51, 52, // Tile 1 BPL 2
                13, 14, // Tile 1 BPL 1 row 2
                53, 54, // Tile 1 BPL 2 row 2


                21, 22, // Tile 2 BPL 1
                61, 62, // Tile 2 BPL 2
                23, 24,
                63, 64,

                31, 32, // ...
                71, 72, // ...
                33, 34,
                73, 74,

                41, 42, // ...
                81, 82, // ...
                43, 44,
                83, 84
            };

            Assert.AreEqual(expected, _writer.Data);
        }
    }
}