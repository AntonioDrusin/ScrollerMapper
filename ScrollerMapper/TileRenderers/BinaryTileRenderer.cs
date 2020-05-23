using System.Drawing;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.TileRenderers
{
    internal class BinaryTileRenderer : ITileRenderer
    {
        private readonly IBitmapTransformer _transformer;
        private readonly IWriter _writer;

        public BinaryTileRenderer(IBitmapTransformer transformer, IWriter writer)
        {
            _transformer = transformer;
            _writer = writer;
        }

        /// <summary>
        /// Renders tiles so each tile's bitplanes are next to each other.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bitmap"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        public void Render(string name, Bitmap bitmap, int tileWidth, int tileHeight, int planeCount)
        {
            if (tileWidth % 8 != 0)
                throw new ConversionException($"Tile rendering {name}. Tile width must be a multiple of 8.");
            if (bitmap.Width % tileWidth != 0)
                throw new ConversionException($"Bitmap must be a width that is a multiple of the tile width.");
            if (bitmap.Height % tileHeight != 0)
                throw new ConversionException($"Bitmap must be a height that is a multiple of the tile height.");

            _transformer.SetBitmap(bitmap);

            var byteWidth = _transformer.GetByteWidth();
            var height = _transformer.GetHeight();
            var tileByteWidth = tileWidth / 8;
            var tileSize = tileByteWidth * tileHeight * planeCount;

            var data = _transformer.GetBitplanes(planeCount);
            var destination = new byte[data.Length + tileSize]; // For one blank tile at the beginning
            var bplSize = byteWidth * height;

            var tileCounter = 0;
            for (int tileY = 0; tileY < bitmap.Height / tileHeight; tileY++)
            {
                for (int tileX = 0; tileX < bitmap.Width / tileWidth; tileX++)
                {
                    for (int tileRow = 0; tileRow < tileHeight; tileRow++)
                    {
                        for (int bpl = 0; bpl < planeCount; bpl++)
                        {
                            var src = tileX * tileByteWidth
                                      + tileY * tileHeight * byteWidth
                                      + bpl * bplSize
                                      + tileRow * byteWidth;

                            var dst = tileByteWidth * tileHeight * planeCount * tileCounter
                                      + bpl * tileByteWidth
                                      + tileRow * tileByteWidth * planeCount
                                      + tileSize // One blank tile at the beginning
                                ;

                            for (int b = 0; b < tileByteWidth; b++)
                            {
                                var s = data[src++];
                                destination[dst++] = s;
                            }
                        }
                    }

                    tileCounter++;
                }
            }

            _writer.WriteCode(Code.Chip, "; Tiles are in interlaced format one after another.");
            _writer.WriteCode(Code.Chip,
                "; The first tile is all zeros and is to be used when Tiled does not have a tile assigned.");
            _writer.WriteCode(Code.Chip,
                $"; Tiles are {tileByteWidth} byte wide, {tileHeight} pixel tall and have {planeCount} biplanes");

            _writer.WriteCode(Code.Normal, $"; Tile definitions for {name}");
            _writer.WriteCode(Code.Normal, $"TILE_BWIDTH_{name}\t\t\tequ\t{tileByteWidth}");
            _writer.WriteCode(Code.Normal, $"TILE_HEIGHT_{name}\t\t\tequ\t{tileHeight}");
            _writer.WriteCode(Code.Normal, $"TILE_PLANE_COUNT_{name}\tequ\t{planeCount}");


            _writer.StartObject(ObjectType.Tile, name);
            _writer.WriteBlob(destination);
            _writer.EndObject();
        }
    }
}