using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;

namespace ScrollerMapper.GameProcessors
{
    internal class FontProcessor : IGameProcessor
    {
        private readonly IWriter _writer;
        private readonly IBitmapTransformer _bitmapTransformer;
        private readonly ICodeWriter _codeWriter;

        public FontProcessor(IWriter writer, IBitmapTransformer bitmapTransformer, ICodeWriter codeWriter)
        {
            _writer = writer;
            _bitmapTransformer = bitmapTransformer;
            _codeWriter = codeWriter;
        }

        public void Process(GameDefinition definition)
        {
            _codeWriter.WriteStructureDeclaration<FontStructure>();
            if (definition.Fonts != null)
            {
                ProcessFonts(definition.Fonts);
            }
        }

        private void ProcessFonts(Dictionary<string, FontDefinition> definitionFonts)
        {
            foreach (var tuple in definitionFonts)
            {
                var fontName = tuple.Key;
                var font = tuple.Value;

                ProcessFont(fontName, font);
            }
        }

        private void ProcessFont(string fontName, FontDefinition font)
        {
            var dst = ExtractCharacters(font);

            var paletteBitmap = font.PaletteFile.FromInputFolder().LoadIndexedBitmap();
            var transformer =
                new IndexedTransformer(font.ImageFile, dst, paletteBitmap.Palette, ConvertMode.StrictPalette);
            var indexedBitmap = transformer.ConvertToIndexed();
            _bitmapTransformer.SetBitmap(indexedBitmap);
            var bitplanes = _bitmapTransformer.GetInterleaved(font.PlaneCount);
            _writer.StartObject(ObjectType.Bitmap, fontName + "Font");
            _writer.WriteBlob(bitplanes);
            _writer.EndObject();

            // _writer.WriteCode(Code.Data, $"\tsection\tdata");
            // _writer.WriteCode(Code.Data, $"{fontName}Font:");
            // _writer.WriteCode(Code.Data, $"\tdc.l\t{fontName}FontBitmap");
            // _writer.WriteCode(Code.Data, $"\tdc.l\t{fontName}FontBitmap+{_bitmapTransformer.GetByteWidth()*(font.Height-1)*font.PlaneCount}");
            // var fontWordWidth = ((font.Width + 15) / 16);
            // var modulo = (_bitmapTransformer.GetByteWidth()  * font.PlaneCount) - ((fontWordWidth + 1) * 2);
            // _writer.WriteCode(Code.Data, $"\tdc.w\t{modulo}\t; modulo");
            // _writer.WriteCode(Code.Data, $"\tdc.w\t{font.Width}\t; font width");
            // _writer.WriteCode(Code.Data, $"\tdc.w\t{font.Height}\t; font height");
            // _writer.WriteCode(Code.Data, $"\tdc.w\t{(1 + (font.Width / 16)) * 2}\t; font byte width for dst modulo calc");
            // var blitSize = ((font.Height * font.PlaneCount) << 6) + (fontWordWidth+1);
            // _writer.WriteCode(Code.Data, $"\tdc.w\t${blitSize:X4}\t; BLTSIZE");
            // // first word mask is all ones for things larger than 15
            // ushort FWM = 0xffff;
            // ushort LWM;
            // if (font.Width < 16)
            // {
            //     FWM = (ushort) (FWM << (16 - font.Width));
            //     LWM = 0;
            // }
            // else
            // {
            //     LWM = (ushort)(0xffff << (16 - (font.Width % 16)));
            // }
            // _writer.WriteCode(Code.Data, $"\tdc.w\t${FWM:X4}\t; FWM");
            //
            // _writer.WriteCode(Code.Data, $"\tdc.w\t${LWM:X4}\t; LWM");
            // _writer.WriteCode(Code.Data, $"\tdc.w\t{font.PlaneCount}\t; PlaneCount");
        }

        private static Bitmap ExtractCharacters(FontDefinition font)
        {
            var numChar = 128 - 32;
            var lines = font.Characters.Split('\n');
            var x = font.X;
            var y = font.Y;

            var paletteImage = font.PaletteFile.FromInputFolder().LoadIndexedBitmap();
            var image = font.ImageFile.FromInputFolder().LoadBitmap();

            var dst = new Bitmap(font.Width * numChar, font.Height, PixelFormat.Format24bppRgb);
            dst.Palette = paletteImage.Palette;
            var graphics = Graphics.FromImage(dst);
            var src = new Bitmap(image);

            foreach (var line in lines)
            {
                var byteLine = Encoding.ASCII.GetBytes(line);
                foreach (var character in byteLine)
                {
                    if (character - 32 < 128 && character > 32)
                    {
                        var destinationX = (character - 32) * font.Width;

                        graphics.DrawImage(
                            src,
                            new Rectangle(destinationX, 0, font.Width, font.Height),
                            new Rectangle(x, y, font.Width, font.Height),
                            GraphicsUnit.Pixel
                        );
                    }

                    x += font.Width;
                }

                x = font.X;
                y += font.VerticalDistance;
            }

            return dst;
        }

        public IEnumerable<string> RequiredTypes()
        {
            return null;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class FontStructure
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public int FontBitmapPtr;
        public int FontBitmapEndPtr;
        public short FontModulo;
        public short FontWidth;
        public short FontHeight;
        public short FontByteWidth;
        public short FontBltSize;
        public short FontFWM;
        public short FontLWM;
        public short FontPlaneCount;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }
}