using System;
using System.CodeDom.Compiler;
using System.IO;
using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper
{
    internal class FileWriter : IWriter, IDisposable
    {
        private readonly Options _options;
        private readonly Lazy<IndentedTextWriter> _textWriter;
        private BinaryWriter _currentWriter;

        private IndentedTextWriter TextWriter => _textWriter.Value;

        public FileWriter(Options options)
        {
            _options = options;

            var mainFileName = GetFileNameFor(ObjectType.Assembly, Path.GetFileNameWithoutExtension(options.TileFileName));
            _textWriter =
                new Lazy<IndentedTextWriter>(() =>
                    new IndentedTextWriter(new StreamWriter(File.Create(mainFileName)), "\t"));
        }

        public void StartObject(ObjectType type, string name)
        {
            var fileName = GetFileNameFor(type, name);
            TextWriter.WriteLine($"{name}{GetLabelPostfix(type)}:");
            TextWriter.Indent++;
            TextWriter.WriteLine($"incbin {Path.GetFileName(fileName)}");

            _currentWriter = new BinaryWriter(File.Create(fileName));
        }

        public void CompleteObject()
        {
            TextWriter.WriteLine("even");
            TextWriter.Indent--;
            TextWriter.WriteLine();
            _currentWriter?.Dispose();
            _currentWriter = null;
            TextWriter.Flush();
        }

        public void WriteByte(byte data)
        {
            _currentWriter.Write(data);
        }

        public void WriteWord(ushort data)
        {
            _currentWriter.Write(Endian.ConvertWord(data));
        }

        public void WriteLong(uint data)
        {
            _currentWriter.Write(Endian.ConvertLong(data));
        }

        public void WriteBlob(byte[] data)
        {
            _currentWriter.Write(data);
        }

        public void WriteCode(string code)
        {
            TextWriter.WriteLine(code);
        }

        public void Dispose()
        {
            if (_textWriter.IsValueCreated)
            {
                _textWriter.Value.Close();
                _textWriter.Value.Dispose();
            }

            _currentWriter?.Dispose();
        }

        private string GetFileNameFor(ObjectType type, string name)
        {
            string extension;
            switch (type)
            {
                case ObjectType.Assembly:
                    extension = "S";
                    break;
                case ObjectType.Palette:
                    extension = "PAL";
                    break;
                case ObjectType.TileInfo:
                    extension = "INFO";
                    break;
                case ObjectType.Bitmap:
                    extension = "BMP";
                    break;
                case ObjectType.Layer:
                    extension = "LAYER";
                    break;
                case ObjectType.Tile:
                    extension = "TILE";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            if (name == null) name = Path.GetFileNameWithoutExtension(_options.TileFileName);
            return Path.Combine(_options.OutputFolder, $"{name}.{extension}");
        }

        private string GetLabelPostfix(ObjectType type)
        {
            return type.ToString();
        }
    }
}