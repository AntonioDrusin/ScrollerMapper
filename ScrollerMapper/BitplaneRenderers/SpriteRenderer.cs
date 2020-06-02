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
        private readonly Options _options;
        private readonly IBitmapTransformer _transformer;
        private static bool once = false;

        public SpriteRenderer(IWriter writer, Options options, IBitmapTransformer transformer)
        {
            _writer = writer;
            _options = options;
            _transformer = transformer;
        }

        public void Render(string name, SpriteDefinition definition)
        {
            var result = Aseprite.ConvertAnimation(definition.File, _options.OutputFolder);
            var aseprite = result.JsonFile.ReadJsonFile<AsepriteDefinition>();
            var palette = definition.Palette.FromInputFolder().LoadIndexedBitmap();
            var bitmap = result.BitmapFile.LoadBitmap();

            _writer.StartObject(ObjectType.Sprite, name);

            WriteSpriteCommentsOnce();


            _writer.WriteWord((ushort) aseprite.Frames.Count);
            var offset = 2 + aseprite.Frames.Count * 6; // 4 words each frame for the "header"

            var converted = new List<byte[]>();
            foreach (var frame in aseprite.Frames)
            {
                if (frame.Frame.W != 16)
                {
                    throw new ConversionException($"Sprite {name} in {definition.File} frames must be 16xx");
                }

                var celBitmap = bitmap.Clone(new Rectangle(frame.Frame.X, frame.Frame.Y, frame.Frame.W, frame.Frame.H),
                    bitmap.PixelFormat);

                if (string.Compare(definition.SpriteNumber, "attached", StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    celBitmap = MapSpriteColors(definition, frame, palette, celBitmap);
                }

                _transformer.SetBitmap(celBitmap);
                var sprite = TrimmedSprite.TrimSprite(_transformer.GetInterleaved(PlaneCount));
                            

                converted.Add(sprite.Sprite);

                _writer.WriteWord((ushort) offset);
                _writer.WriteByte(0);
                _writer.WriteByte((byte) (frame.Duration / 20)); // 20ms frames = 1/50th of a second.
                _writer.WriteByte((byte)sprite.OffsetX); 
                _writer.WriteByte((byte)sprite.Height); 

                offset += sprite.Sprite.Length + 8; // Add Control words and termination words as well, 
            }

            foreach (var sprite in converted)
            {
                _writer.WriteWord(0);
                _writer.WriteWord(0);
                _writer.WriteBlob(sprite);
                _writer.WriteWord(0); // Termination word (assuming no sprite reuse)
                _writer.WriteWord(0);
            }

            _writer.EndObject();
        }

        private void WriteSpriteCommentsOnce()
        {
            if (once) return;
            once = true;

            _writer.WriteCode(Code.Normal, ";---- SPRITES STRUCTURES ----");
            _writer.WriteCode(Code.Normal, ";----------------------------");
            _writer.WriteCode(Code.Normal, "");
            _writer.WriteCode(Code.Normal, $"SPRITES_CELLCOUNT_W\t\tequ\t0");
            _writer.WriteCode(Code.Normal, $"SPRITES_STRUCT_SIZE\t\tequ\t2");
            _writer.WriteCode(Code.Normal, "");
            _writer.WriteCode(Code.Normal, $"SPRITE_OFFSET_W\t\tequ\t0");
            _writer.WriteCode(Code.Normal, $"SPRITE_UNUSED_B\t\tequ\t2");
            _writer.WriteCode(Code.Normal, $"SPRITE_PERIOD_B\t\tequ\t3\t\t; In 1/50th of a second");
            _writer.WriteCode(Code.Normal, $"SPRITE_YOFFSET_B\tequ\t4");
            _writer.WriteCode(Code.Normal, $"SPRITE_HEIGHT_W\t\tequ\t5");
            _writer.WriteCode(Code.Normal, $"SPRITE_STRUCT_SIZE\tequ\t6");
            _writer.WriteCode(Code.Normal, "");
            _writer.WriteCode(Code.Normal, "");

        }

        private static Bitmap MapSpriteColors(SpriteDefinition definition, AsepriteCelDefinition frame, Bitmap palette, Bitmap celBitmap)
        {
            var resultBitmap = new Bitmap(frame.Frame.W, frame.Frame.H, PixelFormat.Format8bppIndexed);
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
            int lastLine = 0;
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
            return new TrimmedSprite {
                OffsetX = firstLine,
                Height = height,
                Sprite = sprite.Skip(firstLine*4).Take(height*4).ToArray()
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