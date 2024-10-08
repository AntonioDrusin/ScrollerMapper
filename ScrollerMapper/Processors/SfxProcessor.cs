﻿using System.Collections.Generic;
using NAudio.Wave;
using ScrollerMapper.Converters.Infos;
using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Writers;

namespace ScrollerMapper.Processors
{
    internal class SfxProcessor : IProcessor
    {
        private readonly IWriter _writer;
        private const int BufferSize = 128;
        private readonly Dictionary<string, int> _waveOffsets = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _waveLengths = new Dictionary<string, int>();
        private const double TicksPerSecond = 3546895.0; // HWM PAL
        private readonly ItemManager _items;
        private readonly ICodeWriter _codeWriter;

        public SfxProcessor(IWriter writer, ItemManager items, ICodeWriter codeWriter)
        {
            _writer = writer;
            _items = items;
            _codeWriter = codeWriter;
        }

        public void Process(LevelDefinition definition)
        {
            if (definition.Sfx != null)
            {
                Render(definition.Sfx);
            }

        }

        public IEnumerable<string> RequiredTypes()
        {
            return null;
        }

        private void Render(SfxDefinition sfxDefinition)
        {
            var inBuffer = new float [BufferSize];
            var outBuffer = new byte[BufferSize];

            WriteWaveFile(sfxDefinition, inBuffer, outBuffer);
            WriteSounds(sfxDefinition);
        }

        private void WriteSounds(SfxDefinition sfxDefinition)
        {
            _codeWriter.WriteStructureDeclaration<SoundStructure>();

            _writer.StartObject(ObjectType.Data, "sounds");

            _writer.WriteWord(0); // So the first sound is not 0 which could mean no-sound

            foreach (var soundTuple in sfxDefinition.Sounds)
            {
                var sound = soundTuple.Value;
                _items.Add(ItemTypes.Sound, soundTuple.Key, _writer.GetCurrentOffset(ObjectType.Data));
                
                var startOffset = (ushort) _waveOffsets[sound.Waveform];
                var period = (ushort) (TicksPerSecond / sound.Frequency);
                var length = (ushort) (_waveLengths[sound.Waveform] / 2);
                _writer.WriteWord(startOffset);
                _writer.WriteWord(period);
                _writer.WriteWord(length);
                _writer.WriteByte((byte) sound.Volume);
                _writer.WriteByte(0);
            }

            _writer.EndObject();
        }

        private void WriteWaveFile(SfxDefinition sfxDefinition, float[] inBuffer, byte[] outBuffer)
        {
            var offset = 0;
            var startOffset = 0;
            _writer.StartObject(ObjectType.Audio, "sfx");
            foreach (var wave in sfxDefinition.Waveforms)
            {
                using (var audioFile = new AudioFileReader(wave.Value.SoundFile.FromInputFolder()))
                {
                    while (audioFile.HasData(BufferSize))
                    {
                        var sampleCount = audioFile.Read(inBuffer, 0, BufferSize);
                        if (sampleCount > 0)
                        {
                            for (var i = 0; i < BufferSize; i++)
                            {
                                outBuffer[i] = (byte) (127 * inBuffer[i]);
                            }

                            _writer.WriteBlob(outBuffer, sampleCount);

                            if (sampleCount % 2 != 0) // make wave ends on even bytes
                            {
                                sampleCount++;
                                _writer.WriteByte(0);
                            }

                            offset += sampleCount;
                        }
                    }
                }

                _waveOffsets.Add(wave.Key, startOffset);
                _waveLengths.Add(wave.Key, offset - startOffset);
                startOffset = offset;
            }

            _writer.EndObject();
        }
        
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal class SoundStructure
    {
        public short SoundStartOffset;
        public short SoundPeriod;
        public short SoundLength;
        public byte SoundVolume;
        public byte SoundUnused;
    }
    
}