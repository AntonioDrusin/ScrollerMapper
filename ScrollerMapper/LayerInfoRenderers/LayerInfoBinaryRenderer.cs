namespace ScrollerMapper.LayerInfoRenderers
{
    internal class LayerInfoBinaryRenderer : ILayerInfoRenderer
    {
        private readonly IWriter _writer;

        public LayerInfoBinaryRenderer(IWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// Writes a binary file with this format:
        ///   WORD width;
        ///   WORD height;
        /// 
        /// Followed by one word for each tile id
        /// 
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="tileBpl"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        public void Render(LayerDefinition layer, int tileBpl, int tileWidth, int tileHeight)
        {
            _writer.StartObject(ObjectType.Layer, null);
            foreach (var tileId in layer.TileIds)
            {

                // Pointer to tile instead of id would be
                // tileId * tileWinBytes * tileHeight * tileBpl
                _writer.WriteWord((ushort)((tileId) * tileWidth * tileHeight * tileBpl / 8));
                
            }
            _writer.EndObject();

        }

        
        
    }
}