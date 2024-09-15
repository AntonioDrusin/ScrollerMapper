using System.Collections.Generic;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal class BonusProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ICodeWriter _codeWriter;
        private readonly ItemManager _items;

        public BonusProcessor(IWriter writer, ICodeWriter codeWriter, ItemManager items)
        {
            _writer = writer;
            _codeWriter = codeWriter;
            _items = items;
        }

        public void Process(LevelDefinition definition)
        {
            WriteBonusComments();

            _writer.StartObject(ObjectType.Fast, "Bonuses");
            if (definition.Bonuses != null)
            {
                var i = 0;
                foreach (var bonus in definition.Bonuses)
                {
                    var bobForBonus = _items.Get(ItemTypes.Bob, bonus.Bob, $"bonus[{i}]");
                    _writer.WriteOffset(ObjectType.Chip, bobForBonus.Offset);
                    i++;
                }
            }
            _writer.EndObject();
        }

        private void WriteBonusComments()
        {
         _codeWriter.WriteStructureDeclaration<BonusStructure>();   
        }

        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Bob;
        }
    }

    internal class BonusStructure
    {
        public int BonusBobPtr;
    }
}
