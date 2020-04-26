using System.IO;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommandLine;
using ScrollerMapper.ImageRenderers;
using ScrollerMapper.PaletteRenderers;
using ScrollerMapper.LayerInfoRenderers;

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
                        Component.For<IFileNameGenerator>().ImplementedBy<FileNameGenerator>() ,
                        Component.For<IBitplaneRenderer>().ImplementedBy<BinaryBitplaneRenderer>()
                    );

                    if (!Directory.Exists(o.OutputFolder))
                    {
                        Directory.CreateDirectory(o.OutputFolder);
                    }

                    var converter = container.Resolve<ITileSetConverter>();
                    converter.ConvertAll();
                });


        }


    }
}
