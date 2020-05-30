using System;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.ImageRenderers
{
    internal class BinaryBitplaneRenderer : IBitplaneRenderer
    {
        private readonly IWriter _writer;
        private readonly IBitmapTransformer _transformer;

        public BinaryBitplaneRenderer(IWriter writer, IBitmapTransformer transformer)
        {
            _writer = writer;
            _transformer = transformer;
        }

        public void Render(string name, Bitmap bitmap, int planeCount)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("Only 8bpp format is supported.");
            }

            _transformer.SetBitmap(bitmap);


            _writer.StartObject(ObjectType.Bitmap, name);


            var byteWidth = _transformer.GetByteWidth();
            var height = _transformer.GetHeight();

            _writer.WriteCode(Code.Normal, $"{name}_BPL\t\tequ\t{planeCount}");
            _writer.WriteCode(Code.Normal, $"{name}_BWIDTH\t\tequ\t{byteWidth}");
            _writer.WriteCode(Code.Normal, $"{name}_MODULO\t\tequ\t{name}_BWIDTH*{name}_BPL");


            var planes = _transformer.GetBitplanes(planeCount);
            var interleaved = new byte[planes.Length];

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

            _writer.WriteBlob(interleaved);
            _writer.EndObject();
        }
    }
}