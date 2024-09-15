using ScrollerMapper.BitplaneRenderers;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;

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

        public ImageInfo ConvertAll(string name, ImageDefinition definition, Destination destination = Destination.Executable)
        {
            var fileName = definition.ImageFile;
            var image = fileName.FromInputFolder().LoadIndexedBitmap();
            _bitplaneRenderer.Render(name, image, definition.PlaneCount, destination);
            var palette = new PaletteTransformer(name, image.Palette, definition.PlaneCount.PowerOfTwo());
            _paletteRenderer.Render(palette, false);
            return new ImageInfo { 
                Height = image.Height,
            };
        }
    }

    internal class ImageInfo
    {
        public int Height;
    }
}
