using System;
using System.Collections.Generic;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Processors
{
    internal enum EnemyFireMovements
    {
        SlowDirect = 1,
        NormalDirect,
        FastDirect,
    }

    internal class EnemyFireProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;
        private EnemyFireDefinition _definition;

        public EnemyFireProcessor(IWriter writer, ItemManager items)
        {
            _writer = writer;
            _items = items;
        }

        public void Process(LevelDefinition definition)
        {
            if (definition.EnemyFire == null) return;
            _definition = definition.EnemyFire;

            WriteEnemyFireComments();
            _writer.StartObject(ObjectType.Fast, "EnemyFireTypes");
            foreach (var fireKeyValue in _definition.Types)
            {
                var fire = fireKeyValue.Value;
                var fireName = fireKeyValue.Key;
                var offset = _writer.GetCurrentOffset(ObjectType.Fast);
                _writer.WriteWord((ushort) _items.Get(ItemTypes.Sound, fire.Sound, fireName).Offset);
                _writer.WriteWord((ushort) fire.Movement);
                _writer.WriteWord((ushort) fire.Period);
                var bob = _items.Get(ItemTypes.Bob, fire.Bob, fireName);
                _writer.WriteOffset(ObjectType.Chip, bob.Offset);
                _items.Add(ItemTypes.EnemyFire, fireName, offset);
            }

            _writer.EndObject();

            WriteSupportingCode();
        }

        private void WriteEnemyFireComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Enemy Fire Definitions
");
            foreach (var name in Enum.GetNames(typeof(EnemyFireMovements)))
            {
                var value = (int) Enum.Parse(typeof(EnemyFireMovements), name);
                _writer.WriteCode(Code.Normal, $"FIREMOV_{name}\tequ\t{value}");
            }

            _writer.WriteCode(Code.Normal, @"
    structure FireStructure, 0
    word    FireSoundLUT_w
    word    FireMovement_w
    word    FirePeriod_w
    long    FireBobPtr_l
    label   FIRE_STRUCT_SIZE
");
        }

        private void WriteSupportingCode()
        {
            WriteDirectFireTable("FastDirectFire", _definition.Direct.FastSpeed);
            WriteDirectFireTable("NormalDirectFire", _definition.Direct.NormalSpeed);
            WriteDirectFireTable("SlowDirectFire", _definition.Direct.SlowSpeed);
        }

        private void WriteDirectFireTable(string label, double directFastSpeed)
        {
            int precision = 9;
            _writer.WriteCode(Code.Data, "\tsection data");
            _writer.WriteCode(Code.Data, $"{label}Lookup:");
            for (double x = 0; x <= precision; x++)
            {
                var list = new List<byte>();
                for (double y = 0; y < precision; y++)
                {
                    var num = directFastSpeed / Math.Sqrt((x * x) + (y * y));

                    list.Add((byte) Math.Round(x * num * 16));
                }

                _writer.WriteCode(Code.Data, $"\tdc.b\t{string.Join(",", list)}");
            }

            _writer.WriteCode(Code.Data, "\teven");
        }

        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Bob;
            yield return ItemTypes.Sound;
        }
    }
}