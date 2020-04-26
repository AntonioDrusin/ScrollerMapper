using CommandLine;

namespace ScrollerMapper
{
    internal class Options
    {
        [Option('n', "name", Required = true, HelpText = "Name of converted object. For labels, files and all related parts.")]
        public string Name { get; set; }
 
        [Option('t', "tiles", Required = true, HelpText = "Name of tile file json formatted.")]
        public string TileFileName { get; set; }
    }
}
