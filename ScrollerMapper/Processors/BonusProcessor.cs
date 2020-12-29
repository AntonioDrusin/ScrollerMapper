using System.Collections.Generic;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Processors
{
    internal class BonusProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;

        public BonusProcessor(IWriter writer, ItemManager items)
        {
            _writer = writer;
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
            _writer.WriteCode(Code.Normal, @"
; Bonus structure
        structure    BonusStructure, 0
        long         BonusBobPtr_l
        label        BONUS_STRUCT_SIZE
");
        }

        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Bob;
        }
    }
}
