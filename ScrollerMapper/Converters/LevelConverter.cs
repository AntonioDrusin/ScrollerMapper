using System.Drawing.Imaging;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;

namespace ScrollerMapper.Converters
{
    internal class LevelConverter
    {
        private readonly Options _options;
        private readonly TiledConverter _tiledConverter;
        private readonly ImageConverter _imageConverter;
        private readonly BobConverter _bobConverter;
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly IWriter _writer;

        public LevelConverter(
            Options options,
            TiledConverter tiledConverter,
            ImageConverter imageConverter,
            BobConverter bobConverter, IPaletteRenderer paletteRenderer, IWriter writer)
        {
            _options = options;
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
            _paletteRenderer = paletteRenderer;
            _writer = writer;
        }

        public void ConvertAll()
        {

            var definition = _options.InputFile.ReadJsonFile<LevelDefinition>();
            foreach (var tiledDefinition in definition.Tiles)
            {
                _tiledConverter.ConvertAll(tiledDefinition.Key, tiledDefinition.Value);
            }

            foreach (var imageDefinition in definition.Images)
            {
                _imageConverter.ConvertAll(imageDefinition.Key, imageDefinition.Value);
            }

            WriteBobComments();


            var bobPalette = definition.BobPaletteFile.LoadBitmap();
            ConvertBobPalette(bobPalette.Palette, definition);

            foreach (var bob in definition.Bobs)
            {
                _bobConverter.ConvertAll(bob.Key, bob.Value, definition.BobPlaneCount, bobPalette.Palette);
            }
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
            _writer.WriteCode(Code.Normal, "**  @ MaskByteOffset interleaved planes with the data (same mask is repeated for each plane)");
            _writer.WriteCode(Code.Normal,"");
            _writer.WriteCode(Code.Normal, "BOBS_WIDTH\t\tequ\t0");
            _writer.WriteCode(Code.Normal, "BOBS_COUNT\t\tequ\t2");
            _writer.WriteCode(Code.Normal, "BOBS_STRUCT_SIZE\t\tequ\t4");
            _writer.WriteCode(Code.Normal, "BOB_PLANEOFFSET\t\tequ\t0");
            _writer.WriteCode(Code.Normal, "BOB_MASKOFFSET\t\tequ\t2");
            _writer.WriteCode(Code.Normal, "BOB_HEIGHT\t\tequ\t4");
            _writer.WriteCode(Code.Normal, "BOB_YADJUST\t\tequ\t6");
            _writer.WriteCode(Code.Normal, "BOB_STRUCT_SIZE\t\tequ\t8");

            _writer.WriteCode(Code.Normal, "");
        }

        private void ConvertBobPalette(ColorPalette palette, LevelDefinition definition)
        {
            _paletteRenderer.Render("bob", palette, definition.BobPlaneCount.PowerOfTwo());
        }
    }
}