using System;
using System.IO;

namespace ScrollerMapper
{
    internal interface IFileNameGenerator
    {
        string GetPaletteFileName(string name);
        string GetTileInfoFileName(string name);
        string GetBitmapFileName(string name);
        string GetLayerFileName(string name);
    }

    internal class FileNameGenerator : IFileNameGenerator
    {
        private readonly Options _options;

        public FileNameGenerator(Options options)
        {
            _options = options;
        }

        public string GetPaletteFileName(string name)
        {
            return Path.Combine(_options.OutputFolder, name + ".PAL");
        }

        public string GetTileInfoFileName(string name)
        {
            return Path.Combine(_options.OutputFolder, name + ".TILE");
        }

        public string GetLayerFileName(string name)
        {
            return Path.Combine(_options.OutputFolder, name + ".LAYER");
        }

        public string GetBitmapFileName(string name)
        {
            return Path.Combine(_options.OutputFolder, name + ".BMP");
        }
    }
}
