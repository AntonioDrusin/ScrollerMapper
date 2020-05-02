using System;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.ImageRenderers
{
    internal class BinaryBitplaneRenderer : IBitplaneRenderer
    {
        private readonly Options _options;
        private readonly IWriter _writer;
        private readonly IBitmapTransformer _transformer;

        public BinaryBitplaneRenderer(Options options, IWriter writer, IBitmapTransformer transformer)
        {
            _options = options;
            _writer = writer;
            _transformer = transformer;
        }

        public void Render(string name, Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("Only 8bpp format is supported.");
            }

            _transformer.SetBitmap(bitmap);


            _writer.StartObject(ObjectType.Bitmap, name);
            _writer.WriteWord((ushort) _transformer.GetByteWidth());
            _writer.WriteWord((ushort) _transformer.GetHeight());
            _writer.WriteBlob(_transformer.GetBitplanes(_options.PlaneCount));
            _writer.CompleteObject();
        }
    }
}