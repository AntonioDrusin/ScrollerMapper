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
        private int _offset;
        private List<byte[]> _converted;
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
            _attached =
                string.Compare(definition.SpriteNumber, "attached", StringComparison.CurrentCultureIgnoreCase) == 0;
            ConvertSprite(name);
        }

        private void ConvertSprite(string name)
        {
            _offset = 0;
            var palette = _definition.Palette.FromInputFolder().LoadIndexedBitmap();
            var bitmap = _definition.File.FromInputFolder().LoadBitmap();

            StartCelList(name);

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
                bobBitmap = MapColors(_definition, palette, bobBitmap);

                _transformer.SetBitmap(bobBitmap);
                var sprite = TrimSprite(_transformer.GetInterleaved(PlaneCount));

                if (sprite.Any(v => v != 0))
                {
                    AddSprite(sprite, name);
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

            CompleteSpriteList(name);
        }

        private void CompleteSpriteList(string name)
        {
            var spriteType = _definition.Mode == SpriteMode.Fast ? ObjectType.SpriteFast : ObjectType.Sprite;

            _writer.StartObject(spriteType, $"{name}Cels");
            foreach (var sprite in _converted)
            {
                WriteControlWords();
                _writer.WriteBlob(sprite);
                WriteControlWords();
            }

            _writer.EndObject();
        }

        private void WriteControlWords()
        {
            if (_definition.Mode == SpriteMode.ChipWithControlWords)
            {
                _writer.WriteWord(0);
                _writer.WriteWord(0);
            }
        }

        private void AddSprite(byte[] sprite, string name)
        {
            _converted.Add(sprite);
            _writer.WriteCode(Code.Data, $"\tdc.l\t{name}CelsSprite+{_offset}");
            _offset += sprite.Length;

            if (_definition.Mode == SpriteMode.ChipWithControlWords)
            {
                _offset += 8; // Add Control words and termination words as well, 
            }
        }

        private void StartCelList(string name)
        {
            WriteSpriteCommentsOnce();

            _converted = new List<byte[]>();

            _writer.WriteCode(Code.Data, $"\tsection data");
            _writer.WriteCode(Code.Data, $"{name}Sprite:");
            _writer.WriteCode(Code.Data, $"\tdc.w\t{_definition.Height-_definition.TopTrim-_definition.BottomTrim}");
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

        private static Bitmap MapColors(SpriteDefinition definition, Bitmap palette, Bitmap celBitmap)
        {
            var resultBitmap = new Bitmap(celBitmap.Width, celBitmap.Height, PixelFormat.Format8bppIndexed);
            var resultPalette = resultBitmap.Palette;
            for (int i = 0; i < resultPalette.Entries.Length; i++)
            {
                resultPalette.Entries[i] = Color.FromArgb(0, 0, 0, 0);
            }

            var spriteColorIndexOffset = (int.Parse(definition.SpriteNumber) / 2) * 4;
            for (int i = 1; i < 4; i++)
            {
                resultPalette.Entries[i] = palette.Palette.Entries[i + spriteColorIndexOffset];
            }

            var transformer = new IndexedTransformer(definition.File, celBitmap, resultPalette);
            return transformer.ConvertToIndexed();
        }

        public byte[] TrimSprite(byte[] sprite)
        {
            var topTrim = _definition.TopTrim * 4;
            var take = (_definition.Height - (_definition.BottomTrim + _definition.TopTrim)) * 4;
            return sprite.Skip(topTrim).Take(take).ToArray();
        }
    }
}