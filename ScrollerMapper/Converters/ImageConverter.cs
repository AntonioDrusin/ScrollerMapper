using System.IO;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.ImageRenderers;
using ScrollerMapper.PaletteRenderers;

namespace ScrollerMapper.Converters
{
    internal class ImageConverter
    {
        private readonly IPaletteRenderer _paletteRenderer;
        private readonly IBitplaneRenderer _bitplaneRenderer;

        public ImageConverter(IPaletteRenderer paletteRenderer, IBitplaneRenderer bitplaneRenderer)
        {
            _paletteRenderer = paletteRenderer;
            _bitplaneRenderer = bitplaneRenderer;
        }

        public void ConvertAll(string name, ImageDefinition definition)
        {
            var fileName = definition.ImageFile;
            var image = fileName.LoadBitmap();
            _bitplaneRenderer.Render(Path.GetFileNameWithoutExtension(fileName), image, definition.PlaneCount);
            _paletteRenderer.Render(Path.GetFileNameWithoutExtension(fileName), image.Palette, definition.PlaneCount.PowerOfTwo());
        }
    }
}
