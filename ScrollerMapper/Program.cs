using System;
using System.IO;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommandLine;
using ScrollerMapper.ImageRenderers;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.LayerInfoRenderers;
using ScrollerMapper.TileRenderers;
using ScrollerMapper.Transformers;

namespace ScrollerMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new WindsorContainer();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    container.Register(
                        Component.For<Options>().Instance(o),
                        Component.For<IPaletteRenderer>().ImplementedBy<BinaryPaletteRenderer>(),
                        Component.For<ITileSetConverter>().ImplementedBy<TileSetConverter>(),
                        Component.For<ILayerInfoRenderer>().ImplementedBy<LayerInfoBinaryRenderer>(),
                        Component.For<IWriter>().ImplementedBy<FileWriter>(),
                        Component.For<IBitplaneRenderer>().ImplementedBy<BinaryBitplaneRenderer>(),
                        Component.For<ITileRenderer>().ImplementedBy<BinaryTileRenderer>(),
                        Component.For<IBitmapTransformer>().ImplementedBy<BitmapTransformer>()
                    );

                    if (!Directory.Exists(o.OutputFolder))
                    {
                        Directory.CreateDirectory(o.OutputFolder);
                    }

                    var converter = container.Resolve<ITileSetConverter>();

                    try
                    {
                        converter.ConvertAll();
                        container.Release(converter); // disposes objects
                    }
                    catch (ConversionException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });
        }
    }
}