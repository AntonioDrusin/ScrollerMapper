using ScrollerMapper.StreamExtensions;

namespace ScrollerMapper.LayerInfoRenderers
{
    internal class LayerInfoBinaryRenderer : ILayerInfoRenderer
    {
        private readonly IFileNameGenerator _fileNameGenerator;

        public LayerInfoBinaryRenderer(IFileNameGenerator fileNameGenerator)
        {
            _fileNameGenerator = fileNameGenerator;
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
            using (var writer = _fileNameGenerator.GetLayerFileName(layer.Name).GetBinaryWriter())
            {
                writer.WriteWord((ushort) layer.Width);
                writer.WriteWord((ushort) layer.Height);
                foreach (var tileId in layer.TileIds)
                {
                    writer.WriteWord((ushort)tileId);
                }
            }
        }
    }
}