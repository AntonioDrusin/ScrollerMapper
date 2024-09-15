using System.Collections.Generic;
using System.Linq;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal class WaveProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private readonly ItemManager _items;
        private readonly ICodeWriter _codeWriter;

        public WaveProcessor(IWriter writer, ItemManager items, ICodeWriter codeWriter)
        {
            _writer = writer;
            _items = items;
            _codeWriter = codeWriter;
        }


        public void Process(LevelDefinition definition)
        {
            WriteWaveComments();
            _codeWriter.WriteNumericConstant("MaxActiveWaves", definition.MaxActiveWaves);
            _codeWriter.WriteNumericConstant("MaxActiveEnemies", definition.MaxActiveEnemies);
            _codeWriter.WriteIncludeComments("Waves: final wave has a special WaveDelay of $ffff to mark the end");

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
                _writer.WriteWord(wave.ExtraBonus);
            }

            _writer.WriteWord(0xffff);

            _writer.EndObject();
        }

        private void WriteWaveComments()
        {
            _codeWriter.WriteStructureDeclaration<WaveStructure>();
        }
        
        public IEnumerable<string> RequiredTypes()
        {
            yield return ItemTypes.Enemy;
            yield return ItemTypes.Path;
            yield return ItemTypes.EnemyFire;
        }
    }

    
    internal class WaveStructure
    {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public short WaveDelay;
        public short WaveOnCount;
        public short WaveEnemyCount;
        public int WaveEnemyPtr;
        public int WavePathPtr;
        public short WavePeriod;
        public short WaveSpawnX;
        public short WaveSpawnY;
        public short WaveSpawnXOffset;
        public short WaveSpawnYOffset;
        public int WaveFirePtr;
        public byte WaveBonus0;
        public byte WaveBonus1;
        public byte WaveBonus2;
        public byte WaveBonus3;
        public short WaveExtraBonus;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
    }
}

