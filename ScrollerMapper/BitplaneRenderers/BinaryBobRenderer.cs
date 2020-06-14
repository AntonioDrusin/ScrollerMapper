using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal interface IBobRenderer
    {
        void Render(string name, Bitmap bitmap, int bobWidth, int planeCount);
    }

    internal class BinaryBobRenderer : IBobRenderer
    {
        private readonly IBitmapTransformer _transformer;
        private readonly IWriter _writer;
        private int _bobWordWidth;
        private int _planeCount;

        public BinaryBobRenderer(IWriter writer, IBitmapTransformer transformer)
        {
            _writer = writer;
            _transformer = transformer;
        }

        public void Render(string name, Bitmap bitmap, int bobWidth, int planeCount)
        {
            if (bobWidth % 8 != 0)
            {
                throw new ConversionException("Bob width must be a multiple of 8.");
            }

            _transformer.SetBitmap(bitmap);

            _writer.StartObject(ObjectType.Bob, name);

            var planes = _transformer.GetBitplanes(planeCount);
            _planeCount = planeCount;
            SaveBobsWithMasks(planes, _transformer.GetByteWidth(), _transformer.GetHeight(), bobWidth / 8, 0);

            _writer.EndObject();
        }


        private void SaveBobsWithMasks(byte[] planes, int byteWidth, int height, int tileByteWidth, int initialOffset)
        {
            var numTiles = byteWidth / tileByteWidth;
            _bobWordWidth = (tileByteWidth + 1) / 2;
            var info = new BobData[numTiles];


            _writer.WriteWord((ushort) _bobWordWidth);
            initialOffset += 2;
            _writer.WriteWord((ushort) numTiles);
            initialOffset += 2;

            for (int t = 0; t < numTiles; t++)
            {
                var currentBob = new BobData();
                info[t] = currentBob;

                for (int y = 0; y < height; y++)
                {
                    byte[] rowData = new byte[_bobWordWidth * 2 * _planeCount];
                    byte[] rowMask = new byte[_bobWordWidth * 2 * _planeCount];

                    for (int xb = 0; xb < tileByteWidth; xb++)
                    {
                        byte mask = 0;

                        for (int bpl = 0; bpl < _planeCount; bpl++)
                        {
                            var value = planes[
                                t * tileByteWidth + xb + y * byteWidth + bpl * byteWidth * height
                            ];
                            mask |= value;
                            rowData[xb + bpl * (_bobWordWidth * 2)] = value;
                        }

                        for (var mPlane = 0; mPlane < _planeCount; mPlane++)
                        {
                            rowMask[xb + (mPlane) * (_bobWordWidth * 2)] = mask;
                        }
                    }

                    currentBob.PlaneRows.Add(rowData);
                    currentBob.MaskRows.Add(rowMask);
                }
            }

            // All Bob info added to the initial offset
            initialOffset += numTiles * 16; // 8 bytes per tile of information in the header

            AdjustBobs(info, initialOffset);
            WriteBobs(info);
        }

        private void WriteBobs(BobData[] info)
        {
            foreach (var bob in info)
            {
                _writer.WriteLong((ushort) bob.BobOffset);
                _writer.WriteLong((ushort) bob.MaskOffset);
                _writer.WriteWord((ushort) bob.LineCount);
                _writer.WriteWord((ushort) bob.YAdjustment);
                _writer.WriteWord(0); //modulo for pre calculations
                // blitsize (H*planes)<<6 + W(in words)
                _writer.WriteWord((ushort) (((bob.LineCount * _planeCount) << 6) + _bobWordWidth + 1)); // Adds 1 word as we over-read to shift.
            }

            foreach (var bob in info)
            {
                foreach (var row in bob.PlaneRows)
                {
                    _writer.WriteBlob(row);
                }

                foreach (var row in bob.MaskRows)
                {
                    _writer.WriteBlob(row);
                }
            }
        }

        private void AdjustBobs(BobData[] info, int offset)
        {
            foreach (var bob in info)
            {
                bool firstDataEncountered = false;
                var firstRow = 0;
                var lastRow = 0;
                var currentRow = 0;

                foreach (var row in bob.PlaneRows)
                {
                    var empty = row.All(_ => _ == 0);
                    if (!firstDataEncountered)
                    {
                        firstRow = currentRow;
                        if (!empty)
                        {
                            firstDataEncountered = true;
                        }
                    }

                    if (!empty)
                    {
                        lastRow = currentRow;
                    }

                    currentRow++;
                }

                bob.LineCount = (short) (lastRow - firstRow + 1);
                bob.YAdjustment = (short) (firstRow);
                // Remove top and bottom rows with just zeroes
                bob.PlaneRows = bob.PlaneRows.Skip(firstRow).Take(bob.LineCount).ToList();
                bob.MaskRows = bob.MaskRows.Skip(firstRow).Take(bob.LineCount).ToList();
                bob.BobOffset = (short) offset;
                offset += bob.PlaneRows.Sum(r => r.Length);
                bob.MaskOffset = (short) offset;
                offset += bob.MaskRows.Sum(r => r.Length);
            }
        }
    }

    internal class BobData
    {
        public short LineCount;
        public short BobOffset;
        public short MaskOffset;
        public short YAdjustment;
        public List<byte[]> PlaneRows = new List<byte[]>();
        public List<byte[]> MaskRows = new List<byte[]>();
    }
}