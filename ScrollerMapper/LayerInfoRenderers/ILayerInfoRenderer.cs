namespace ScrollerMapper.LayerInfoRenderers
{
    internal interface ILayerInfoRenderer
    {
        void Render(LayerDefinition layer, int tileBpl, int tileWidth, int tileHeight);
    }
}
