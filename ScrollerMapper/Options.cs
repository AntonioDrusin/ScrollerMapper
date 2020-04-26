using CommandLine;

namespace ScrollerMapper
{
    internal class Options
    {
 
        [Option('t', "tiles", Required = true, HelpText = "Name of tile file json formatted.")]
        public string TileFileName { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output folder.", Default = ".")]
        public string OutputFolder { get; set; }

        [Option('p', "planes", Required = true, HelpText = "Plane count for every image")]
        public int PlaneCount { get; set; }
    }
}
