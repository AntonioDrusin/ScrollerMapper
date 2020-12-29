using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ScrollerMapper.Converters;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.Processors
{
    internal class BobProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly BobConverter _bobConverter;
        private readonly ItemManager _items;
        private readonly IPaletteRenderer _paletteRenderer;
        private LevelDefinition _definition;

        public BobProcessor(IWriter writer, BobConverter bobConverter, ItemManager items,
            IPaletteRenderer paletteRenderer)
        {
            _writer = writer;
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
            _writer.WriteCode(Code.Normal, "");
            _writer.WriteCode(Code.Normal, "** Structure of BOBS ");
            _writer.WriteCode(Code.Normal, "** WordWidth  WORD ");
            _writer.WriteCode(Code.Normal, "** BobCount WORD ");
            _writer.WriteCode(Code.Normal, "**  ");
            _writer.WriteCode(Code.Normal, "** BobCount frames follow: ");
            _writer.WriteCode(Code.Normal, "** FrameByteOffset WORD ; from the beginning of the file ");
            _writer.WriteCode(Code.Normal, "** MaskByteOffset WORD ; from the beginning of the file ");
            _writer.WriteCode(Code.Normal, "** Lines WORD ; how many lines is this frame made out of ");
            _writer.WriteCode(Code.Normal, "** YAdjustment WORD ; word to add to Y when drawing ");
            _writer.WriteCode(Code.Normal, "**  ");
            _writer.WriteCode(Code.Normal, "**  Binary Blob data follows ");
            _writer.WriteCode(Code.Normal, "**  @ FrameByteOffset interleaved planes with the data");
            _writer.WriteCode(Code.Normal,
                "**  @ MaskByteOffset interleaved planes with the data (same mask is repeated for each plane)");
            _writer.WriteCode(Code.Normal, @"
    structure   BobsStructure, 0
    word        BobsWordWidth_w
    word        BobsCount_w 
    label       BOBS_STRUCT_SIZE

    structure   BCelStructure, 0 
    long        BCelPlaneOffset_l        ; Long, so upon load you can turn it into a pointer
    long        BCelMaskOffset_l         ; Long, so upon load you can turn this into a pointer
    word        BCelHeight_w
    word        BCelYAdjust_w
    word        BCelDModulo_w            ; Destination modulo for bob (set to 0, you need to initialize this)
    word        BCelBlitSize_w           ; This is pre-calculated    
    label       BCEL_STRUCT_SIZE

");
            _writer.WriteCode(Code.Normal, "");
        }
    }
}