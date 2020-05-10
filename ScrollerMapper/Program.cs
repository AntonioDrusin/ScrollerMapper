using System;
using System.IO;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommandLine;
using ScrollerMapper.Converters;
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

            Parser.Default.ParseArguments<SpritesOptions, TileOptions>(args)
                .WithParsed<TileOptions>(o =>
                {
                    container.Register(
                        Component.For<TileOptions, BaseOptions>().Instance(o),
                        Component.For<IPaletteRenderer>().ImplementedBy<BinaryPaletteRenderer>(),
                        Component.For<IConverter>().ImplementedBy<TiledConverter>(),
                        Component.For<ILayerInfoRenderer>().ImplementedBy<LayerInfoBinaryRenderer>(),
                        Component.For<IWriter>().ImplementedBy<FileWriter>(),
                        Component.For<IBitplaneRenderer>().ImplementedBy<BinaryBitplaneRenderer>(),
                        Component.For<ITileRenderer>().ImplementedBy<BinaryTileRenderer>(),
                        Component.For<IBitmapTransformer>().ImplementedBy<BitmapTransformer>()
                    );
                    ExecuteConverter(container, o);
                })
                .WithParsed<SpritesOptions>(o =>
                {
                    container.Register(
                        Component.For<SpritesOptions>().Instance(o),
                        Component.For<IWriter>().ImplementedBy<FileWriter>(),
                        Component.For<IConverter>().ImplementedBy<SpritesConverter>());

                        ExecuteConverter(container, o);
                });
        }

        private static void ExecuteConverter(WindsorContainer container, BaseOptions o)
        {
            var converter = container.Resolve<IConverter>();

            if (!Directory.Exists(o.OutputFolder))
            {
                Directory.CreateDirectory(o.OutputFolder);
            }

            try
            {
                converter.ConvertAll();
                container.Release(converter); // disposes objects
            }
            catch (ConversionException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}