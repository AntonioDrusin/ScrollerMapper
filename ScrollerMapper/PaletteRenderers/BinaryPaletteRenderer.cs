using System;
using System.Drawing.Imaging;

namespace ScrollerMapper.PaletteRenderers
{
    internal class BinaryPaletteRenderer : IPaletteRenderer
    {
        private readonly TileOptions _options;
        private readonly IWriter _writer;

        public BinaryPaletteRenderer(TileOptions options, IWriter writer)
        {
            _options = options;
            _writer = writer;
        }

        // Renders the palette in one file with each color as a word formatted in bigendian.
        // 0x00RRGGBB
        public void Render(string name, ColorPalette palette, int maxValues)
        {
            _writer.StartObject(ObjectType.Palette, name);

            var maxEntries = Math.Min(palette.Entries.Length, _options.PlaneCount.PowerOfTwo());
            for (int i = 0; i < maxEntries; i++)
            {
                var entry = palette.Entries[i];
                var colors = (ushort) (((entry.R & 0xf0) << 4) | ((entry.G & 0xf0)) | ((entry.B & 0xf0) >> 4));
                _writer.WriteWord(colors);
            }

            _writer.CompleteObject();
        }
    }
}