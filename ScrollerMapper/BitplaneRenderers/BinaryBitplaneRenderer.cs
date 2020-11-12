using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ScrollerMapper.Processors;
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

        public void Render(string name, Bitmap bitmap, int planeCount, bool disk)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("Only 8bpp format is supported.");
            }

            _transformer.SetBitmap(bitmap);

            _writer.StartObject(disk ? ObjectType.Chip: ObjectType.Bitmap, name);


            var byteWidth = _transformer.GetByteWidth();
            var height = _transformer.GetHeight();

            _writer.WriteCode(Code.Normal, $"{name}_BPL\t\tequ\t{planeCount}");
            _writer.WriteCode(Code.Normal, $"{name}_BWIDTH\t\tequ\t{byteWidth}");
            _writer.WriteCode(Code.Normal, $"{name}_HEIGHT\t\tequ\t{height}");

            var interleaved = _transformer.GetInterleaved(planeCount);

            RunLengthEncoder.ProjectCompression(interleaved);

            _writer.WriteBlob(interleaved);
            _writer.EndObject();
        }

     

    
    }
}