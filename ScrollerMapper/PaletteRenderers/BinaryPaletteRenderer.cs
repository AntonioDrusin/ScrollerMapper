using System;
using System.Drawing.Imaging;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper.PaletteRenderers
{
    internal class BinaryPaletteRenderer : IPaletteRenderer
    {
        private readonly Options _options;
        private readonly IFileNameGenerator _fileNameGenerator;

        public BinaryPaletteRenderer(Options options, IFileNameGenerator fileNameGenerator)
        {
            _options = options;
            _fileNameGenerator = fileNameGenerator;
        }

        // Renders the palette in one file with each color as a word formatted in bigendian.
        // 0x00RRGGBB
        public void Render(string name, ColorPalette palette, int maxValues)
        {
            using (var writer = _fileNameGenerator.GetPaletteFileName(name).GetBinaryWriter())
            {
                var colors = new byte[4];
                colors[3] = 0;

                var maxEntries = Math.Min(palette.Entries.Length, _options.PlaneCount.PowerOfTwo());
                for (int i = 0; i < maxEntries; i++)
                {
                    var entry = palette.Entries[i];
                    colors[2] = entry.R;
                    colors[1] = entry.G;
                    colors[0] = entry.B;
                    writer.Write(colors);
                }
            }
        }
    }
}