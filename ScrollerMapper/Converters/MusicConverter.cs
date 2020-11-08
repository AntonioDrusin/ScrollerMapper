using System.IO;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Converters
{
    internal class MusicConverter
    {
        private readonly IWriter _writer;

        public MusicConverter(IWriter writer)
        {
            _writer = writer;
        }

        public void ConvertAll(MusicDefinition definition)
        {
            var module = File.ReadAllBytes(definition.Module.FromInputFolder());
            _writer.StartObject(ObjectType.DiskFast, "menuMusic");
            _writer.WriteBlob(module);
            _writer.EndObject();

            var samples = File.ReadAllBytes(definition.Samples.FromInputFolder());
            _writer.StartObject(ObjectType.DiskChip, "menuSamples");
            _writer.WriteBlob(samples);
            _writer.EndObject();
        }
    }
}
