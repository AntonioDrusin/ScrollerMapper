using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal class SpriteRenderer
    {
        private const int PlaneCount = 2;
        private readonly IWriter _writer;
        private readonly IBitmapTransformer _transformer;
        private static bool _once;
        private List<byte[]> _converted0, _converted1;
        private SpriteDefinition _definition;
        private bool _attached;

        public SpriteRenderer(IWriter writer, IBitmapTransformer transformer)
        {
            _writer = writer;
            _transformer = transformer;
        }

        public void Render(string name, SpriteDefinition definition)
        {
            _definition = definition;
            _converted0 = new List<byte[]>();
            _converted1 = new List<byte[]>();

            _attached =
                string.Compare(definition.SpriteNumber, "attached", StringComparison.CurrentCultureIgnoreCase) == 0;
            ConvertSprite(name);
        }

        private void ConvertSprite(string name)
        {
            var palette = _definition.Palette.FromInputFolder().LoadIndexedBitmap();
            var bitmap = _definition.File.FromInputFolder().LoadBitmap();

            var spriteX = _definition.StartX;
            var spriteY = _definition.StartY;
            var numTiles = _definition.Count;
            var width = 16;
            var height = _definition.Height;
            var maxX = bitmap.Width / width;
            var maxY = bitmap.Height / height;

            int i = 0;
            while (i < numTiles)
            {
                var bobBitmap = bitmap.Clone(new Rectangle(spriteX * width, spriteY * height, width, height),
                    bitmap.PixelFormat);
                bobBitmap = MapColors(palette, bobBitmap);

                _transformer.SetBitmap(bobBitmap);

                var sprite = TrimSprite(_transformer.GetInterleaved(_attached ? PlaneCount * 2 : PlaneCount));
                if (sprite.Any(v => v != 0))
                {
                    if (_attached)
                    {
                        // Given the content (in words) w0,w1,w2,w3,w4,w5,w6,w7
                        // sprite0 will take w0,w1,w4,w5
                        // sprite1 will take w2,w3,w6,w7
                        var sprite0 = new List<byte>();
                        var sprite1 = new List<byte>();
                        var t = 0;
                        while (t < sprite.Length)
                        {
                            for (var n = 0; n < 4; n++) sprite0.Add(sprite[t++]);
                            for (var n = 0; n < 4; n++) sprite1.Add(sprite[t++]);
                        }

                        AddSprite(_converted0, sprite0.ToArray());
                        AddSprite(_converted1, sprite1.ToArray());
                    }
                    else
                    {
                        AddSprite(_converted0, sprite);
                    }

                    i++;
                }

                spriteX++;
                if (spriteX >= maxX)
                {
                    spriteY++;
                    if (spriteY >= maxY)
                        throw new ConversionException(
                            $"Converting {_definition.File} reached the end of the image trying to get {numTiles} tiles.");
                    spriteX = 0;
                }
            }

            WriteSpriteData(name, _converted0);
            if (_attached) WriteSpriteData(name + "A", _converted1);
        }

        private void WriteSpriteData(string name, List<byte[]> list)
        {
            WriteSpriteHeader(name);
            var offset = 0;
            foreach (var sprite in list)
            {
                _writer.WriteCode(Code.Data, $"\tdc.l\t{name}CelsSprite+{offset}");
                offset += sprite.Length;
            }

            var spriteType = _definition.Mode == SpriteMode.Fast ? ObjectType.SpriteFast : ObjectType.Sprite;

            _writer.StartObject(spriteType, $"{name}Cels");
            foreach (var sprite in list)
            {
                _writer.WriteBlob(sprite);
            }

            _writer.EndObject();
        }

        private void AddSprite(ICollection<byte[]> list, byte[] sprite)
        {
            if (_definition.Mode == SpriteMode.ChipWithControlWords)
            {
                var controlWords = new byte[] {0, 0, 0, 0};
                sprite = controlWords.Concat(sprite).Concat(controlWords).ToArray();
            }

            list.Add(sprite);
        }

        private void WriteSpriteHeader(string name)
        {
            WriteSpriteCommentsOnce();

            _writer.WriteCode(Code.Data, $"\tsection data");
            _writer.WriteCode(Code.Data, $"{name}Sprite:");
            _writer.WriteCode(Code.Data,
                $"\tdc.w\t{_definition.Height - _definition.TopTrim - _definition.BottomTrim}");
            _writer.WriteCode(Code.Data, $"\tdc.w\t{_definition.Count}");
        }

        private void WriteSpriteCommentsOnce()
        {
            if (_once) return;
            _once = true;

            _writer.WriteCode(Code.Normal, @"
;---- SPRITES STRUCTURES ----
    structure   SpriteStructure, 0
    word        SpriteHeight_w
    word        SpriteCellCount_w
    label       SPRITE_STRUCT_SIZE
; This is followed by SpriteCellCount_w CelStructure

;---- CEL STRUCTURE ----
    structure   SpriteCelStructure, 0
    word        SCelPtr_l         ; Pointer to sprite data (excludes control words)
    label       SCEL_STRUCT_SIZE ; This will be 8 so you can shift
");
        }

        private Bitmap MapColors(Bitmap palette, Bitmap celBitmap)
        {
            var resultBitmap = new Bitmap(celBitmap.Width, celBitmap.Height, PixelFormat.Format8bppIndexed);
            var resultPalette = resultBitmap.Palette;
            for (int i = 0; i < resultPalette.Entries.Length; i++)
            {
                resultPalette.Entries[i] = Color.FromArgb(0, 0, 0, 0);
            }

            if (_attached)
            {
                for (int i = 1; i < 16; i++) // 16 colors are allowed for attached sprites on the Amiga
                {
                    resultPalette.Entries[i] = palette.Palette.Entries[i];
                }
            }
            else
            {
                var spriteColorIndexOffset = (int.Parse(_definition.SpriteNumber) / 2) * 4;
                for (int i = 1; i < 4; i++)
                {
                    resultPalette.Entries[i] = palette.Palette.Entries[i + spriteColorIndexOffset];
                }
            }

            var transformer = new IndexedTransformer(_definition.File, celBitmap, resultPalette);
            return transformer.ConvertToIndexed();
        }

        public byte[] TrimSprite(byte[] sprite)
        {
            var rowSize = _attached ? 8 : 4;
            var topTrim = _definition.TopTrim * rowSize;
            var take = (_definition.Height - (_definition.BottomTrim + _definition.TopTrim)) * rowSize;
            return sprite.Skip(topTrim).Take(take).ToArray();
        }
    }
}