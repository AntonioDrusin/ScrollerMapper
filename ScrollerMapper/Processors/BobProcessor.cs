using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.Converters;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal class BobProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ICodeWriter _codeWriter;
        private readonly BobConverter _bobConverter;
        private readonly ItemManager _items;
        private readonly IPaletteRenderer _paletteRenderer;
        private LevelDefinition _definition;

        public BobProcessor(IWriter writer,
            ICodeWriter codeWriter,
            BobConverter bobConverter, ItemManager items,
            IPaletteRenderer paletteRenderer)
        {
            _writer = writer;
            _codeWriter = codeWriter;
            _bobConverter = bobConverter;
            _items = items;
            _paletteRenderer = paletteRenderer;
        }

        public void Process(LevelDefinition definition)
        {
            _definition = definition;
            var bobPalette = _definition.BobPaletteFile.FromInputFolder().LoadIndexedBitmap();

            WriteBobComments();

            RenderBobPalette(bobPalette.Palette);

            foreach (var bob in _definition.Bobs)
            {
                ConvertBobToDisk(bob.Key, bob.Value, _definition, bobPalette);
            }
        }

        public IEnumerable<string> RequiredTypes()
        {
            return null;
        }

        private void ConvertBobToDisk(string name, BobDefinition bob, LevelDefinition definition, Bitmap bobPalette)
        {
            var offset = _writer.GetCurrentOffset(ObjectType.Chip);

            _bobConverter.ConvertBob(name, bob, definition.BobPlaneCount, bobPalette.Palette,
                _definition.BobPaletteFlip0AndLast ? BobMode.ColorFlip : BobMode.NoColorFlip, Destination.Disk);

            _items.Add(ItemTypes.Bob, name, offset);
        }

        private void RenderBobPalette(ColorPalette bitmapPalette)
        {
            var palette = new PaletteTransformer("BobPalette", bitmapPalette, _definition.BobPlaneCount.PowerOfTwo());
            if (_definition.BobPaletteFlip0AndLast)
            {
                palette.Flip(0, palette.Length - 1);
            }

            _paletteRenderer.Render(palette, true);
        }

        private void WriteBobComments()
        {
            _codeWriter.WriteIncludeComments(
                "** Structure of BOBS ",
                "** WordWidth  WORD ",
                "** BobCount WORD ",
                "**  ",
                "** BobCount frames follow: ",
                "** FrameByteOffset WORD ; from the beginning of the file ",
                "** MaskByteOffset WORD ; from the beginning of the file ",
                "** Lines WORD ; how many lines is this frame made out of ",
                "** YAdjustment WORD ; word to add to Y when drawing ",
                "**  ",
                "**  Binary Blob data follows ",
                "**  @ FrameByteOffset interleaved planes with the data",
                "**  @ MaskByteOffset interleaved planes with the data (same mask is repeated for each plane)");

            _codeWriter.WriteStructureDeclaration<BobsStructure>();
            _codeWriter.WriteStructureDeclaration<BcelStructure>();
        }
    }

    internal class BobsStructure
    {
        public short BobsWordWidth;
        public short BobsCount;
    }

    internal class BcelStructure
    {
        [Comments("Long, so upon load you can turn it into a pointer")]
        public int BCelPlaneOffset;
        [Comments("Long, so upon load you can turn it into a pointer")]
        public int BCelMaskOffset;
        public short BCelHeight;
        public short BCelYAdjust;
        [Comments("Destination modulo for bob (set to 0, you need to initialize this)")]
        public short BCelDModulo;
        [Comments("This is pre-calculated")]
        public short BCelBlitSize;
    }
}