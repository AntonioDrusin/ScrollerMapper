using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommandLine;
using ScrollerMapper.PaletteRenderers;

namespace ScrollerMapper
{
    class Program
    {
        private static readonly List<PixelFormat> supportedFormats = new List<PixelFormat> {PixelFormat.Format1bppIndexed, PixelFormat.Format4bppIndexed, PixelFormat.Format8bppIndexed};

        static void Main(string[] args)
        {
            var container = new WindsorContainer();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    container.Register(
                        Component.For<Options>().Instance(o),
                        Component.For<IPaletteRenderer>().ImplementedBy<BinaryPaletteRenderer(),
                        Component.For<ITileSetConverter>().ImplementedBy<TileSetConverter>()
                    );
                });
        }


        private static void GenerateImage(string tileFile)
        {
            var tileInfo = tileFile.ReadJsonFile();
            var imageFile = tileInfo["image"]?.Value<string>();

            var bitmap = new Bitmap(imageFile.FromFolderOf(tileFile));

            if (! supportedFormats.Contains(bitmap.PixelFormat))
            {
                throw new InvalidOperationException("Only indexed formats are supported");
            }
            var sourceData = bitmap.GetImageBytes();
            
        }

    }
}
