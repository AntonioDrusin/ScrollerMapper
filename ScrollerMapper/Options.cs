using CommandLine;

namespace ScrollerMapper
{
    internal class Options
    {
        [Option('o', "output", Required = false, HelpText = "Output folder.", Default = ".")]
        public string OutputFolder { get; set; }

        [Option('i', "input", Required = true, HelpText = "Level definition file name.")]
        public string InputFile { get; set; }
    }

}