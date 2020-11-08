using System;
using System.IO;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using CommandLine;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Processors;

namespace ScrollerMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new WindsorContainer();
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel, true));
            
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    container.Register(
                        Classes.FromThisAssembly()
                            .IncludeNonPublicTypes()
                            .InNamespace("ScrollerMapper.Converters")
                            .WithServiceSelf(),
                        Classes.FromThisAssembly()
                            .IncludeNonPublicTypes()
                            .InNamespace("ScrollerMapper.Transformers")
                            .WithServiceAllInterfaces(),
                        Classes.FromThisAssembly()
                            .IncludeNonPublicTypes()
                            .InNamespace("ScrollerMapper.Processors")
                            .WithServiceAllInterfaces(),
                        Classes.FromThisAssembly()
                            .IncludeNonPublicTypes()
                            .Where(t=>t.Name.EndsWith("Renderer"))
                            .WithServiceSelf()
                            .WithServiceAllInterfaces(),
                        Component.For<MainConverter>(),
                        Component.For<Options>().Instance(o),
                        Component.For<IWriter>().ImplementedBy<FileWriter>()
                    );
                    ExecuteConverter(container, o);
                })
                .WithNotParsed((e) => { Environment.ExitCode = -1; });
        }

        private static void ExecuteConverter(WindsorContainer container, Options o)
        {
            var converter = container.Resolve<MainConverter>();

            if (!Directory.Exists(o.OutputFolder))
            {
                Directory.CreateDirectory(o.OutputFolder);
            }

            if (!Directory.Exists(Path.Combine(o.OutputFolder, "disk")))
            {
                Directory.CreateDirectory(Path.Combine(o.OutputFolder, "disk"));
            }


            var sourcePath = Path.GetDirectoryName(Path.GetFullPath(o.InputFile));
            FileExtensions.SetSourceFolder(sourcePath);

            try
            {
                converter.Convert();
                container.Release(converter); // disposes objects
                container.Dispose();
            }
            catch (ConversionException ex)
            {
                Console.WriteLine("ERROR converting " + o.InputFile + ":");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Level has been partially generated");
            }
        }
    }

    internal class MainConverter
    {
        private readonly Options _options;
        private readonly GameProcessor _gameProcessor;

        public MainConverter(Options options, GameProcessor gameProcessor)
        {
            _options = options;
            _gameProcessor = gameProcessor;
        }

        public void Convert()
        {
            var definition = _options.InputFile.ReadJsonFile<GameDefinition>();
            definition.Validate();
            _gameProcessor.Process(definition);
        }
    }
}