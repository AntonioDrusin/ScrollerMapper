﻿using System;
using System.IO;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommandLine;
using ScrollerMapper.Converters;

namespace ScrollerMapper
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new WindsorContainer();

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
                            .Where(t=>t.Name.EndsWith("Renderer"))
                            .WithServiceSelf()
                            .WithServiceAllInterfaces(),
                        Component.For<Options>().Instance(o),
                        Component.For<IWriter>().ImplementedBy<FileWriter>()
                    );
                    ExecuteConverter(container, o);
                })
                .WithNotParsed((e) => { Environment.ExitCode = -1; });
        }

        private static void ExecuteConverter(WindsorContainer container, Options o)
        {
            var converter = container.Resolve<LevelConverter>();

            if (!Directory.Exists(o.OutputFolder))
            {
                Directory.CreateDirectory(o.OutputFolder);
            }

            var sourcePath = Path.GetDirectoryName(Path.GetFullPath(o.InputFile));
            FileExtensions.SetSourceFolder(sourcePath);

            try
            {
                converter.ConvertAll();
                container.Release(converter); // disposes objects
                container.Dispose();
            }
            catch (ConversionException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}