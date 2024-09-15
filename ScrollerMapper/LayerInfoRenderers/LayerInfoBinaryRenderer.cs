using ScrollerMapper.DefinitionModels;
using ScrollerMapper.Writers;

namespace ScrollerMapper.LayerInfoRenderers
{
    internal class LayerInfoBinaryRenderer : ILayerInfoRenderer
    {
        private readonly IWriter _writer;
        private readonly ICodeWriter _codeWriter;

        public LayerInfoBinaryRenderer(IWriter writer, ICodeWriter codeWriter)
        {
            _writer = writer;
            _codeWriter = codeWriter;
        }

        /// <summary>
        /// Writes a binary file with this format:
        ///   WORD width;
        ///   WORD height;
        /// 
        /// Followed by one word for each tile id
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        /// <param name="tileBpl"></param>
        /// <param name="tileWidth"></param>
        /// <param name="tileHeight"></param>
        public void Render(string name, LayerDefinition layer, int tileBpl, int tileWidth, int tileHeight)
        {
            _writer.StartObject(ObjectType.Layer, name);
            _codeWriter.WriteNumericConstant($"LAYER_WIDTH_{name.ToUpperInvariant()}", layer.Width);
            _codeWriter.WriteNumericConstant($"LAYER_HEIGHT_{name.ToUpperInvariant()}", layer.Height);
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