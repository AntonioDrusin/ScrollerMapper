using System;
using System.Drawing;
using System.Drawing.Imaging;

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
        public void Render(string name, ColorPalette palette, int maxValues)
        {
            palette.ValidatePalette(name, maxValues);
            _writer.StartObject(ObjectType.Palette, name);

            if (maxValues != palette.Entries.Length)
            {
                Console.WriteLine($"Warning palette {name} has {palette.Entries.Length} entries instead of {maxValues}");
            }

            for (var i = 0; i < Math.Min(maxValues, palette.Entries.Length); i++)
            {
                var entry = palette.Entries.Length >= i ? palette.Entries[i] : Color.Black;
                var colors = (ushort) (((entry.R & 0xf0) << 4) | ((entry.G & 0xf0)) | ((entry.B & 0xf0) >> 4));
                _writer.WriteWord(colors);
            }

            _writer.EndObject();
        }
    }
}