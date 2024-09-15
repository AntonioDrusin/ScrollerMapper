using ScrollerMapper.DefinitionModels;

namespace ScrollerMapper.LayerInfoRenderers
{
    internal interface ILayerInfoRenderer
    {
        void Render(string name, LayerDefinition layer, int tileBpl, int tileWidth, int tileHeight);
    }
}
