using CommandLine;

namespace ScrollerMapper
{
    internal class BaseOptions
    {
        [Option('o', "output", Required = false, HelpText = "Output folder.", Default = ".")]
        public string OutputFolder { get; set; }

        [Option('n', "name", Required = true , HelpText = "Output file name root")]
        public string OutputName { get; set; }
    }

    [Verb("tiled", HelpText = "Converts tiles exported to json from Tiled")]
    internal class TileOptions : BaseOptions
    {
        [Option('t', "tiles", Required = true, HelpText = "Name of tile file json formatted.")]
        public string TileFileName { get; set; }

        [Option('p', "planes", Required = true, HelpText = "Plane count for every image")]
        public int PlaneCount { get; set; }
    }

    [Verb("sprites", HelpText = "Converts an animation exported by aseprite as numbered png files into raw sprite files")]
    internal class SpritesOptions : BaseOptions
    {
        [Option('d', "double", Required = false, Default = false,
            HelpText = " uses two sprites for each image to support 16 colors")]
        private bool Double { get; set; }

        [Option('s', "sprites", Required = true, HelpText = "Pattern to select all files for a sprite.")]
        public string SpritesFileName { get; set; }

    }
}