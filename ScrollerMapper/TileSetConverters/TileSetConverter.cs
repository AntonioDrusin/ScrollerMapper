using System;
using System.Drawing;
using System.Web.ModelBinding;

namespace ScrollerMapper
{
    internal class TileSetConverter : ITileSetConverter
    {
        private Options _options;

        public TileSetConverter(Options options)
        {
            this._options = options;
        }

        public void ConvertAll()
        {
            var definition = LevelDefinition.Load(_options.TileFileName);
        }
    }
}