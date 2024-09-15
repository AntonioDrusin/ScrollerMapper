using ScrollerMapper.Transformers;
using ScrollerMapper.Writers;

namespace ScrollerMapper.PaletteRenderers
{
    internal class BinaryPaletteRenderer : IPaletteRenderer
    {
        private readonly IWriter _writer;

        public BinaryPaletteRenderer(IWriter writer)
        {
            _writer = writer;
        }

        // Renders the palette in one file with each color as a word formatted in bigendian.
        // 0x0RGB
        public void Render(PaletteTransformer palette, bool disk)
        {
            if (disk)
            {
                _writer.StartObject(ObjectType.Fast, palette.Name);
            }
            else
            {
                _writer.StartObject(ObjectType.Palette, palette.Name);
            }


            foreach (var color in palette.Colors)
            {
                _writer.WriteWord(color);
            }
            _writer.EndObject();
        }
    }
}