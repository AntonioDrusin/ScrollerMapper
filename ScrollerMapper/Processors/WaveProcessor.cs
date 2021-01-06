using System.Collections.Generic;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Processors
{
    internal class WaveProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;

        public WaveProcessor(IWriter writer, ItemManager items)
        {
            _writer = writer;
            _items = items;
        }


        public void Process(LevelDefinition definition)
        {
            WriteWaveComments();
            _writer.WriteCode(Code.Normal, $"MaxActiveWaves\t\tequ\t{definition.MaxActiveWaves}");
            _writer.WriteCode(Code.Normal, $"MaxActiveEnemies\t\tequ\t{definition.MaxActiveEnemies}");
            _writer.WriteCode(Code.Data, "; Waves");
            _writer.WriteCode(Code.Data, "; final wave has a special WaveDelay of $ffff to mark the end");

            _writer.StartObject(ObjectType.Fast, "Waves");

            foreach (var wavePair in definition.Waves)
            {
                var wave = wavePair.Value;
                var path = _items.Get(ItemTypes.Path, wave.Path, wavePair.Key);
                var enemy = _items.Get(ItemTypes.Enemy, wave.Enemy, wavePair.Key);

                _writer.WriteWord(wave.FrameDelay);
                _writer.WriteWord(wave.OnExistingWaves);
                _writer.WriteWord(wave.Count);
                _writer.WriteOffset(ObjectType.Fast, enemy.Offset);
                _writer.WriteOffset(ObjectType.Fast, path.Offset);
                _writer.WriteWord(wave.Period);
                _writer.WriteWord((ushort)wave.StartX);
                _writer.WriteWord((ushort)wave.StartY);
                _writer.WriteWord((ushort)wave.StartXOffset);
                _writer.WriteWord((ushort)wave.StartYOffset);

                var fire = _items.Get(ItemTypes.EnemyFire, wave.Fire, wavePair.Key);
                _writer.WriteOffset(ObjectType.Fast, fire.Offset);

                if (wave.Bonus.Any(b => b > 7))
                {
                    throw new ConversionException("Bonus lookup table can only be 0-7");
                }

                _writer.WriteByte(wave.Bonus[0]);
                _writer.WriteByte(wave.Bonus[1]);
                _writer.WriteByte(wave.Bonus[2]);
                _writer.WriteByte(wave.Bonus[3]);
            }

            _writer.WriteWord(0xffff);

            _writer.EndObject();
        }

        private void WriteWaveComments()
        {
            _writer.WriteCode(Code.Normal, @"
** Structure for wave
** WaveEnemyOffset_b is an offset off of the Enemies label to point to the enemy
    structure   WaveStructure, 0
    word        WaveDelay_w         ; Frame delay before wave is considered for spawn
    word        WaveOnCount_w       ; no more than OnCount waves remaining before start
    word        WaveEnemyCount_w    
    long        WaveEnemyPtr_l      
    long        WavePathPtr_l           
    word        WavePeriod_w        ; Frames between enemy spawn
    word        WaveSpawnX_w        ; spawn location X
    word        WaveSpawnY_w        ; spawn location Y
    word        WaveSpawnXOffset_w   
    word        WaveSpawnYOffset_w  
    long        WaveFirePtr_l
    byte        WaveBonus0_b        ; Which bonus to drop
    byte        WaveBonus1_b
    byte        WaveBonus2_b
    byte        WaveBonus3_b
    label       WAVE_STRUCT_SIZE
");
        }
        
        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Enemy;
            yield return ItemTypes.Path;
            yield return ItemTypes.EnemyFire;
        }
    }
}

