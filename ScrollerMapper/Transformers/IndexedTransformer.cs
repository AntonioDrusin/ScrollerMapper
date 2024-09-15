using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ScrollerMapper.Transformers
{
    internal class ColorIndex
    {
        public Color Color;
        public int Index;
    }

    internal class ColorIndexComparer : IEqualityComparer<ColorIndex>
    {
        public bool Equals(ColorIndex x, ColorIndex y)
        {
            if (x == null || y == null) return false;
            return x.Color.Equals(y.Color);
        }

        public int GetHashCode(ColorIndex obj)
        {
            return obj.Color.GetHashCode();
        }
    }


    internal class IndexedTransformer
    {
        private readonly string _fileName;
        private readonly Bitmap _bitmap;
        private readonly ColorPalette _palette;
        private bool _missedColor;
        private readonly Dictionary<Tuple<byte, byte, byte>, byte> _lut;

        public IndexedTransformer(string fileName, Bitmap bitmap, ColorPalette palette, ConvertMode mode)
        {
            _fileName = fileName;
            _bitmap = bitmap;
            _palette = palette;
            // We reserve color 0 for transparent
            var skip = mode == ConvertMode.TransparentIsZero ? 1 : 0;
            _lut = palette.Entries.Skip( skip ).Select((c, i) => new ColorIndex {Color = c, Index = i})
                .Where(p => p.Color.A == 255) // Only get colors with no alpha
                .Distinct(new ColorIndexComparer())
                .ToDictionary(p => Tuple.Create(p.Color.R, p.Color.G, p.Color.B), color => (byte) (color.Index + skip));
        }

        public Bitmap ConvertToIndexed()
        {
            var width = _bitmap.Width;
            var height = _bitmap.Height;

            var converted = new Bitmap(width, height, PixelFormat.Format8bppIndexed)
            {
                Palette = _palette
            };
            var result = new byte[width * height];


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var color = _bitmap.GetPixel(x, y);
                    
                    result[width * y + x] = color.A < 255 ? (byte) 0 : GetClosestColorIndex(color);
                }
            }

            converted.SetImageBytes(result);

            if (_missedColor)
            {
                Console.WriteLine($"WARNING: Some colors could not be accurately mapped for {_fileName}");
            }

            return converted;
        }

        private byte GetClosestColorIndex(Color color)
        {
            if (_lut.TryGetValue(Tuple.Create(color.R, color.G, color.B), out var index))
            {
                return index;
            }

            var distance = double.MaxValue;
            foreach (var tuple in _lut)
            {
                var curDistance = GetDistance(color, tuple.Key);
                if (curDistance < distance)
                {
                    distance = curDistance;
                    index = tuple.Value;
                }
            }

            _missedColor = true;
            return index;
        }


        private static double GetDistance(Color first, Tuple<byte, byte, byte> second)
        {
            // Purposefully ignore A as there should be none
            var (r, g, b) = second;
            return Math.Sqrt(Math.Pow(first.R - r, 2) + Math.Pow(first.G - g, 2) +
                             Math.Pow(first.B - b, 2));
        }
    }
}