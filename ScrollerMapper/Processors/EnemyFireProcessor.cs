using System;
using System.Collections.Generic;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal enum EnemyFireMovements
    {
        AtPlayer = 1,
        SlowDirect,
        NormalDirect,
        FastDirect,
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class FireStructure
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public short FireSoundLUT;
        public short FireMovement;
        public short FirePeriod;
        public short FireSpeed;
        public int FireBobPtr;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }

    internal class EnemyFireProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;
        private readonly ICodeWriter _codeWriter;
        private EnemyFireDefinition _definition;

        public EnemyFireProcessor(IWriter writer, ItemManager items, ICodeWriter codeWriter)
        {
            _writer = writer;
            _items = items;
            _codeWriter = codeWriter;
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
                _writer.WriteWord((ushort) fire.Speed);
                var bob = _items.Get(ItemTypes.Bob, fire.Bob, fireName);
                _writer.WriteOffset(ObjectType.Chip, bob.Offset);
                _items.Add(ItemTypes.EnemyFire, fireName, offset);
            }

            _writer.EndObject();

            WriteSupportingCode();
        }

        private void WriteEnemyFireComments()
        {
            _codeWriter.WriteIncludeComments("Enemy Fire Definitions");

            foreach (var name in Enum.GetNames(typeof(EnemyFireMovements)))
            {
                var value = (int) Enum.Parse(typeof(EnemyFireMovements), name);
                _codeWriter.WriteNumericConstant($"FIREMOV_{name}", value);
            }
            _codeWriter.WriteNumericConstant("FIRE_LOOKUP_PRECISION", 9);
            _codeWriter.WriteStructureDeclaration<FireStructure>();
        }

        private void WriteSupportingCode()
        {
            WriteDirectFireTable("FastDirectFire", _definition.Direct.FastSpeed);
            WriteDirectFireTable("NormalDirectFire", _definition.Direct.NormalSpeed);
            WriteDirectFireTable("SlowDirectFire", _definition.Direct.SlowSpeed);
        }

        private void WriteDirectFireTable(string name, double directFastSpeed)
        {
            const int precision = 9;
            var list = new List<byte>();
            for (double x = 0; x <= precision; x++)
            {
                for (double y = 0; y < precision; y++)
                {
                    var num = directFastSpeed / Math.Sqrt((x * x) + (y * y));

                    list.Add((byte) Math.Round(x * num * 16));
                }
            }
            _codeWriter.WriteArray(name, 8, list.ToArray());
        }

        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Bob;
            yield return ItemTypes.Sound;
        }
    }
}