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

        public LevelConverter(
            Options options,
            TiledConverter tiledConverter,
            ImageConverter imageConverter,
            BobConverter bobConverter, IPaletteRenderer paletteRenderer)
        {
            _options = options;
            _tiledConverter = tiledConverter;
            _imageConverter = imageConverter;
            _bobConverter = bobConverter;
            _paletteRenderer = paletteRenderer;
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

            var bobPalette = definition.BobPaletteFile.LoadBitmap();
            ConvertBobPalette(bobPalette.Palette, definition);

            foreach (var bob in definition.Bobs)
            {
                _bobConverter.ConvertAll(bob.Key, bob.Value, definition.BobPlaneCount, bobPalette.Palette);
            }
        }

        private void ConvertBobPalette(ColorPalette palette, LevelDefinition definition)
        {
            _paletteRenderer.Render("bob", palette, definition.BobPlaneCount.PowerOfTwo());
        }
    }
}