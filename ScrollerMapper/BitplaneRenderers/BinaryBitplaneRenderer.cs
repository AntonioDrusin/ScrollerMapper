using System;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.Processors;
using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal class BinaryBitplaneRenderer : IBitplaneRenderer
    {
        private readonly IWriter _writer;
        private readonly IBitmapTransformer _transformer;
        private readonly ICodeWriter _codeWriter;

        public BinaryBitplaneRenderer(IWriter writer, IBitmapTransformer transformer, ICodeWriter codeWriter)
        {
            _writer = writer;
            _transformer = transformer;
            _codeWriter = codeWriter;
        }

        public void Render(string name, Bitmap bitmap, int planeCount, Destination destination)
        {
            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("Only 8bpp format is supported.");
            }

            _transformer.SetBitmap(bitmap);

            _writer.StartObject(destination == Destination.Disk ? ObjectType.Chip: ObjectType.Bitmap, name);


            var byteWidth = _transformer.GetByteWidth();
            var height = _transformer.GetHeight();

            _codeWriter.WriteNumericConstant($"{name}_BPL", planeCount);
            _codeWriter.WriteNumericConstant($"{name}_BWIDTH", byteWidth);
            _codeWriter.WriteNumericConstant($"{name}_HEIGHT", height);

            var interleaved = _transformer.GetInterleaved(planeCount);

            RunLengthEncoder.ProjectCompression(interleaved);

            _writer.WriteBlob(interleaved);
            _writer.EndObject();
        }
    }
}