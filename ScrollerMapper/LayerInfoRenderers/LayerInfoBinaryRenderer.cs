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
        public void Render(LayerDefinition layer)
        {
            _writer.StartObject(ObjectType.Layer, null);
            _writer.WriteWord((ushort) layer.Width);
            _writer.WriteWord((ushort) layer.Height);
            foreach (var tileId in layer.TileIds)
            {
                _writer.WriteWord((ushort) tileId);
            }
            _writer.CompleteObject();

        }
    }
}