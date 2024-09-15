using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal class EnemyProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;
        private readonly ICodeWriter _codeWriter;

        public EnemyProcessor(IWriter writer, ItemManager items, ICodeWriter codeWriter)
        {
            _writer = writer;
            _items = items;
            _codeWriter = codeWriter;
        }

        public void Process(LevelDefinition definition)
        {
            WriteEnemies(definition);
        }

        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Sound;
        }

        private void WriteEnemies(LevelDefinition definition)
        {
            _codeWriter.WriteStructureDeclaration<EnemyStructure>();
            _writer.StartObject(ObjectType.Fast, "Enemies");
            foreach (var enemyKeyValue in definition.Enemies)
            {
                var enemy = enemyKeyValue.Value;
                var offset = _writer.GetCurrentOffset(ObjectType.Fast);

                WriteBobOffset(enemy.Bob, enemyKeyValue);

                _writer.WriteWord(enemy.FrameDelay);
                _writer.WriteWord(enemy.Hp);

                var pointString = enemy.Points.ToString("D8");
                foreach (var s in Enumerable.Range(0, 4).Select(i =>
                             byte.Parse($"{pointString.Substring(i * 2, 2)}", NumberStyles.HexNumber)))
                {
                    _writer.WriteByte(s);
                }

                var soundOffset = (ushort)_items.Get(ItemTypes.Sound, enemy.ExplosionSound, enemyKeyValue.Key).Offset;
                _writer.WriteWord(soundOffset);

                WriteBobOffset(enemy.PortalBob, enemyKeyValue);

                _items.Add(ItemTypes.Enemy, enemyKeyValue.Key, offset);
            }

            _writer.EndObject();
        }

        private void WriteBobOffset(string bob, KeyValuePair<string, EnemyDefinition> enemyKeyValue)
        {
            var bobForEnemy = _items.Get(ItemTypes.Bob, bob, enemyKeyValue.Key);
            _writer.WriteOffset(ObjectType.Chip, bobForEnemy.Offset);
        }

    }

    [Comments("Structure for Enemies")]
    internal class EnemyStructure
    {
        public int EnemyBobPtr;
        [Comments("Period in frames between switching bobs")]
        public short EnemyPeriod;
        [Comments("HP for this enemy")]
        public short EnemyHp;
        [Comments("BCD coded points for this enemy")]
        public int EnemyPoints;
        public short EnemySound;
        [Comments("For appearing and possibly disappearing")]
        public int EnemyPortalBobPtr;
    }

}