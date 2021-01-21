using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.Converters.Infos
{

    internal class OutputPathStepInfo
    {
        public short FrameCount;
        public short VelocityX;
        public short VelocityY;
        public string Label;
        public PathInstructionDefinition Instruction;

        public short JumpDelta
        {
            get => VelocityX;
            set => VelocityX = value;
        }

        public override string ToString()
        {
            return $"FrameCount: {FrameCount}, VelocityX: {VelocityX}, VelocityY: {VelocityY}, Label: '{Label}', Instruction: '{Instruction}', JumpDelta: {JumpDelta}";
        }
    }
}