using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ScrollerMapper.Transformers
{
    internal class IndexedTransformer
    {
        private readonly string _fileName;
        private readonly Bitmap _bitmap;
        private readonly ColorPalette _palette;
        private bool _missedColor;
        private readonly Dictionary<Tuple<byte, byte, byte>, byte> _lut;

        public IndexedTransformer(string fileName, Bitmap bitmap, ColorPalette palette)
        {
            _fileName = fileName;
            _bitmap = bitmap;
            _palette = palette;
            // We reserve color 0 for transparent
            _lut = palette.Entries.Skip(1).Select((c, i) => new {Color = c, Index = i})
                .ToDictionary(_ => Tuple.Create(_.Color.R, _.Color.G, _.Color.B), _ => (byte) (_.Index + 1));
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
                    byte colorIndex;
                    var color = _bitmap.GetPixel(x, y);

                    if (color.A < 255)
                    {
                        colorIndex = 0;
                    }
                    else
                    {
                        colorIndex = GetClosestColorIndex(color);
                        if (colorIndex != 6 && colorIndex != 5 && colorIndex != 0 && colorIndex != 1)
                        {
                            colorIndex = colorIndex;
                        }
                    }

                    result[width * y + x] = colorIndex;
                }
            }

            converted.SetImageBytes(result);
            var pick = converted.GetPixel(3, 13);
            if (_missedColor)
            {
                Console.WriteLine($"Some colors could no be accurately mapped for {_fileName}");
            }

            return converted;
        }

        private byte GetClosestColorIndex(Color color)
        {
            byte index;
            if (_lut.TryGetValue(Tuple.Create(color.R, color.G, color.B), out index))
            {
                return (byte) index;
            }

            var distance = double.MaxValue;
            for (byte i = 0; i < _palette.Entries.Length; i++)
            {
                var curDistance = GetDistance(color, _palette.Entries[i]);
                if (!(curDistance < distance)) continue;

                distance = curDistance;
                index = i;
            }

            _missedColor = true;
            return index;
        }


        private static double GetDistance(Color first, Color second)
        {
            // Purposefully ignore A as there should be none
            return Math.Sqrt(Math.Pow(first.R - second.R, 2) + Math.Pow(first.G - second.G, 2) +
                             Math.Pow(first.B - second.B, 2));
        }
    }
}