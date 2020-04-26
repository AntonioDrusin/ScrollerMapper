using System;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper.ImageRenderers
{
    internal class BinaryBitplaneRenderer : IBitplaneRenderer
    {
        private readonly Options _options;
        private readonly IFileNameGenerator _fileNameGenerator;

        public BinaryBitplaneRenderer(Options options, IFileNameGenerator fileNameGenerator)
        {
            _options = options;
            _fileNameGenerator = fileNameGenerator;
        }

        public void Render(string name, Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("Only 8bpp format is supported.");
            }

            var transformer = new BitmapTransformer();
            var result = transformer.GetBitplanes(bitmap, _options.PlaneCount);

            using (var output = _fileNameGenerator.GetBitmapFileName(name).GetBinaryWriter())
            {
                output.WriteWord((ushort)transformer.GetByteWidth(bitmap));
                output.WriteWord((ushort)transformer.GetHeight(bitmap));
                output.Write(transformer.GetBitplanes(bitmap, _options.PlaneCount));
            }

        }
    }
}