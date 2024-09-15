using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.GameProcessors
{
    internal interface IGameProcessor
    {
        void Process(GameDefinition definition);
    }
}