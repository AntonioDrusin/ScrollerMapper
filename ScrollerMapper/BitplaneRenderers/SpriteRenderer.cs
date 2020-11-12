using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal class SpriteRenderer
    {
        private const int PlaneCount = 2;
        private readonly IWriter _writer;
        private readonly Options _options;
        private readonly IBitmapTransformer _transformer;
        private static bool _once = false;
        private int _offset;
        private List<byte[]> _converted;
        private SpriteDefinition _definition;

        public SpriteRenderer(IWriter writer, Options options, IBitmapTransformer transformer)
        {
            _writer = writer;
            _options = options;
            _transformer = transformer;
        }

        public void Render(string name, SpriteDefinition definition, bool disk = false)
        {
            _definition = definition;

            if (string.Compare(Path.GetExtension(definition.File), ".aseprite", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                ConvertAseprite(name, disk);
            }
            else
            {
                ConvertSprite(name, disk);
            }

        }

        private void ConvertAseprite(string name, bool disk)
        {
            var result = Aseprite.ConvertAnimation(_definition.File, _options.OutputFolder);
            var aseprite = result.JsonFile.ReadJsonFile<AsepriteDefinition>();
            var palette = _definition.Palette.FromInputFolder().LoadIndexedBitmap();
            var bitmap = result.BitmapFile.LoadBitmap();
            
            StartSpriteList(name, aseprite.Frames.Count, disk);

            foreach (var frame in aseprite.Frames)
            {
                if (frame.Frame.W != 16)
                {
                    throw new ConversionException($"Sprite {name} in {_definition.File} frames must be 16xx");
                }

                var celBitmap = bitmap.Clone(new Rectangle(frame.Frame.X, frame.Frame.Y, frame.Frame.W, frame.Frame.H),
                    bitmap.PixelFormat);

                if (string.Compare(_definition.SpriteNumber, "attached", StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    celBitmap = MapColors(_definition, palette, celBitmap);
                }

                _transformer.SetBitmap(celBitmap);
                var sprite = TrimmedSprite.TrimSprite(_transformer.GetInterleaved(PlaneCount));

                AddSprite(sprite, frame.Duration);
            }

            CompleteSpriteList();
        }

        private void ConvertSprite(string name, bool disk)
        {

            var palette = _definition.Palette.FromInputFolder().LoadIndexedBitmap();
            var bitmap = _definition.File.FromInputFolder().LoadBitmap();

            StartSpriteList(name, _definition.Count, disk);
            
            var spriteX = _definition.StartX;
            var spriteY = _definition.StartY;
            var numTiles = _definition.Count;
            var width = 16;
            var height = _definition.Height;
            var maxX = bitmap.Width / width;
            var maxY = bitmap.Height / height;

            int i = 0;
            while (i<numTiles)
            {
                var bobBitmap = bitmap.Clone(new Rectangle(spriteX * width, spriteY * height, width, height), bitmap.PixelFormat);
                bobBitmap = MapColors(_definition, palette, bobBitmap);

                _transformer.SetBitmap(bobBitmap);
                var sprite = TrimmedSprite.TrimSprite(_transformer.GetInterleaved(PlaneCount));

                if (sprite.Height > 0)
                {
                    AddSprite(sprite, _definition.Duration);
                    i++;
                }
                
                spriteX++;
                if (spriteX >= maxX)
                {
                    spriteY++;
                    if (spriteY >= maxY) throw new ConversionException($"Converting {_definition.File} reached the end of the image trying to get {numTiles} tiles.");
                    spriteX = 0;
                }
            }
            CompleteSpriteList();
        }

        private void CompleteSpriteList()
        {
            foreach (var sprite in _converted)
            {
                _writer.WriteWord(0);
                _writer.WriteWord(0);
                _writer.WriteBlob(sprite);
                _writer.WriteWord(0); // Termination word (assuming no sprite reuse)
                _writer.WriteWord(0);
            }

            _writer.EndObject();
        }

        private void AddSprite(TrimmedSprite sprite, int duration)
        {
            _converted.Add(sprite.Sprite);
            _writer.WriteWord((ushort) _offset);
            _writer.WriteWord((ushort) (duration / 20)); // 20ms frames = 1/50th of a second.
            _writer.WriteWord((ushort) sprite.OffsetX);
            _writer.WriteWord((ushort) sprite.Height);

            _offset += sprite.Sprite.Length + 8; // Add Control words and termination words as well, 
        }

        private void StartSpriteList(string name, int count, bool disk)
        {
            _converted = new List<byte[]>();
            _writer.StartObject(disk ? ObjectType.Chip : ObjectType.Sprite, name);
            WriteSpriteCommentsOnce();
            _writer.WriteWord((ushort) count);
            _offset = 2 + count * 8; // 4 words each frame for the "header"
        }

        private void WriteSpriteCommentsOnce()
        {
            if (_once) return;
            _once = true;

            _writer.WriteCode(Code.Normal, @"
;---- SPRITES STRUCTURES ----
    structure   SpriteStructure, 0
    word        SpriteCellCount_w
    label       SPRITE_STRUCT_SIZE
; This is followed by SpriteCellCount_w CelStructure

;---- CEL STRUCTURE ----
    structure   CelStructure, 0
    word        CelOffset_w         ; Offset from beginning of binary
    word        CelPeriod_w
    word        CelYOffset_w
    word        CelHeight_w
    label       CEL_STRUCT_SIZE ; This will be 8 so you can shift
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
    }

    internal class TrimmedSprite
    {
        public byte[] Sprite;
        public int OffsetX;
        public int Height;

        public static TrimmedSprite TrimSprite(byte[] sprite)
        {
            int firstLine = 0;
            int lastLine = -1;
            bool first = true;
            for (int i = 0; i < sprite.Length; i += 4)
            {
                var isZero = sprite[i] == 0 && sprite[i + 1] == 0 && sprite[i + 2] == 0 && sprite[i + 3] == 0;
                if (!isZero)
                {
                    if (first)
                    {
                        first = false;
                        firstLine = i/4;
                    }
                    lastLine = i/4;
                }
            }

            var height = lastLine-firstLine+1;
            // return new TrimmedSprite {
            //     OffsetX = firstLine,
            //     Height = height,
            //     Sprite = sprite.Skip(firstLine*4).Take(height*4).ToArray()
            // };
            return new TrimmedSprite
            {
                OffsetX = 0,
                Height = height == 0 ? 0 : sprite.Length/4,
                Sprite = sprite
            };

        }

    }

    internal class AsepriteDefinition
    {
        public List<AsepriteCelDefinition> Frames;
    }

    internal class AsepriteCelDefinition
    {
        public AsepriteFrameDefinition Frame;
        public int Duration;
    }

    internal class AsepriteFrameDefinition
    {
        public int X;
        public int Y;
        public int W;
        public int H;
    }
}