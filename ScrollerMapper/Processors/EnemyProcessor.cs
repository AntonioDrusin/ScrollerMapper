using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Processors
{
    internal class EnemyProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;

        public EnemyProcessor(IWriter writer, ItemManager items)
        {
            _writer = writer;
            _items = items;
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
            WriteEnemyComments();
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


        private void WriteEnemyComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for Enemies
** EnemyBobOffset is an offset in bytes from the Enemies label
    structure   EnemyStructure, 0
    long        EnemyBobPtr_l
    word        EnemyPeriod_w       ; Period in frames between switching bobs
    word        EnemyHp_w           ; HP for this enemy
    long        EnemyPoints_l       ; BCD coded points for this enemy
    word        EnemySound_w
    long        EnemyPortalBobPtr_l ; for appearing and possibly disappearing
    label       ENEMY_STRUCT_SIZE
");
        }
    }
}