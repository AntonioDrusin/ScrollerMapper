using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ScrollerMapper.Transformers
{
    public class PaletteTransformer
    {
        public IEnumerable<ushort> Colors => _colors;
        public string Name { get; private set; }
        public int Length => _colors.Length;

        private readonly ushort [] _colors;

        public PaletteTransformer(string name, ColorPalette palette, int maxValues)
        {
            Name = name;
            palette.ValidatePalette(name, maxValues);

            if (maxValues != palette.Entries.Length)
            {
                Console.WriteLine($"Warning palette {name} has {palette.Entries.Length} entries instead of {maxValues}");
            }

            _colors = new ushort [maxValues];

            for (var i = 0; i < Math.Min(maxValues, palette.Entries.Length); i++)
            {
                var entry = palette.Entries.Length >= i ? palette.Entries[i] : Color.Black;
                _colors[i]= (ushort)(((entry.R & 0xf0) << 4) | ((entry.G & 0xf0)) | ((entry.B & 0xf0) >> 4));
            }
        }

        public void Flip(int color1, int color2)
        {
            var temp = _colors[color1];
            _colors[color1] = _colors[color2];
            _colors[color2] = temp;
        }
    }
}
