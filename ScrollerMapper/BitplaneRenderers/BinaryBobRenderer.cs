using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ScrollerMapper.Converters;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Transformers;

namespace ScrollerMapper.BitplaneRenderers
{
    internal interface IBobRenderer
    {
        void Render(string name, Bitmap bitmap, BobDefinition definition, int planeCount, BobMode colorFlip,
            Destination destination = Destination.Executable);
    }

    internal class BinaryBobRenderer : IBobRenderer
    {
        private readonly IBitmapTransformer _transformer;
        private readonly IWriter _writer;
        private int _planeCount;
        private BobDefinition _definition;
        private int _bobWordWidth;
        private bool _colorFlip;

        public BinaryBobRenderer(IWriter writer, IBitmapTransformer transformer)
        {
            _writer = writer;
            _transformer = transformer;
        }

        // colorFlip:
        // 1. color 0 becomes color 15
        // 1. color 15 becomes color 0
        // 1. all of color 15 are now transparent
        public void Render(string name, Bitmap bitmap, BobDefinition definition, int planeCount, BobMode colorFlip,
            Destination destination = Destination.Executable)
        {
            _definition = definition;
            _planeCount = planeCount;
            _bobWordWidth = (_definition.Width / 8 + 1) / 2;
            _colorFlip = colorFlip == BobMode.ColorFlip;

            var width = definition.Width;
            if (width % 8 != 0)
            {
                throw new ConversionException("Bob width must be a multiple of 8.");
            }

            _writer.StartObject(destination == Destination.Disk ? ObjectType.Chip : ObjectType.Bob, name);

            try
            {
                SaveBobsWithMasks(bitmap, 0);
            }
            catch(ConversionException ex)
            {
                throw new ConversionException($"Converting bob {name}: {ex.Message}");
            }

            _writer.EndObject();
        }


        private void SaveBobsWithMasks(Bitmap bitmap, int initialOffset)
        {
            var width = _definition.Width;
            var height = _definition.Height.GetValueOrDefault(bitmap.Height);
            var numTiles = _definition.Count.GetValueOrDefault(bitmap.Width / _definition.Width);
            var bobX = _definition.StartX;
            var bobY = _definition.StartY;

            var info = new BobData[numTiles];

            _writer.WriteWord((ushort) (_bobWordWidth));
            initialOffset += 2;
            _writer.WriteWord((ushort) numTiles);
            initialOffset += 2;

            for (var i = 0; i < numTiles;)
            {
                var currentBob = new BobData();
                info[i] = currentBob;

                if (bobX + width > bitmap.Width) throw new ConversionException("Bob out of bitmap bounds");
                if (bobY + height > bitmap.Height) throw new ConversionException("Bob out of bitmap bounds");

                var bobBitmap = bitmap.Clone(new Rectangle(bobX, bobY, width, height),
                    bitmap.PixelFormat);

                _transformer.SetBitmap(bobBitmap);

                byte transparent = 0;
                if (_colorFlip)
                {
                    transparent = (byte) ((1 << _planeCount) - 1);
                    _transformer.FlipColors(0, transparent);
                }

                var planes = _transformer.GetInterleaved(_planeCount); // This will bump up to word size...

                if (planes.Any(_ => _ != 0))
                {
                    for (var y = 0; y < height; y++)
                    {
                        var rowData = new byte[_bobWordWidth * 2 * _planeCount];
                        var rowMask = new byte[_bobWordWidth * 2 * _planeCount];

                        if (_colorFlip)
                        {
                            ArrayFill(rowData, transparent);
                        }

                        for (var x = 0; x < width / 8; x++)
                        {
                            byte mask = _colorFlip ? (byte) 0xff : (byte) 0x00;

                            for (var bpl = 0; bpl < _planeCount; bpl++)
                            {
                                var value = planes[y * _planeCount * _bobWordWidth * 2 + bpl * _bobWordWidth * 2 + x];
                                rowData[bpl * _bobWordWidth * 2 + x] = value;

                                if (_colorFlip)
                                {
                                    mask &= value;
                                }
                                else
                                {
                                    mask |= value;
                                }
                            }

                            for (var bpl = 0; bpl < _planeCount; bpl++)
                            {
                                rowMask[bpl * _bobWordWidth * 2 + x] = _colorFlip ? (byte) ~mask : (byte) mask;
                            }
                        }

                        currentBob.PlaneRows.Add(rowData);
                        currentBob.MaskRows.Add(rowMask);
                    }

                    i++;
                }

                if (i < numTiles)
                {
                    bobX+=width;
                    if (bobX >= bitmap.Width)
                    {
                        bobY+=height;
                        if (bobY >= bitmap.Height)
                            throw new ConversionException(
                                $"Converting {_definition.ImageFile} reached the end of the image trying to get {numTiles} tiles.");
                        bobX = _definition.StartX;
                    }
                }
            }

            // All Bob info added to the initial offset
            initialOffset += numTiles * 16; // 8 bytes per tile of information in the header

            AdjustBobs(info, initialOffset);
            WriteBobs(info);
        }

        private void ArrayFill(byte[] rowData, byte transparent)
        {
            for (int i = 0; i < rowData.Length; i++)
            {
                rowData[i] = transparent;
            }
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
                _writer.WriteWord((ushort) (((bob.LineCount * _planeCount) << 6) + _bobWordWidth +
                                            1)); // Adds 1 word as we over-read to shift.
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