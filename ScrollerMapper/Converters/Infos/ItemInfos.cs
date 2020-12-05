using System.Collections;
using System.Collections.Generic;

namespace ScrollerMapper.Converters.Infos
{
    internal class ItemInfos : IEnumerable<ItemInfo>
    {
        private readonly string _infoName;
        private readonly Dictionary<string, ItemInfo> _infos = new Dictionary<string, ItemInfo>();

        public ItemInfos(string infoName)
        {
            _infoName = infoName;
        }

        public void Add(string name, int index, uint offset)
        {
            _infos.Add(name, new ItemInfo { Name = name, Offset = offset, Index = index });
        }

        public IEnumerator<ItemInfo> GetEnumerator()
        {
            return _infos.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _infos.Values.GetEnumerator();
        }

        public ItemInfo Get(string name, string sourceName)
        {
            try
            {
                return _infos[name];
            }
            catch (KeyNotFoundException)
            {
                throw new ConversionException($"Cannot find {_infoName} '{name}' for '{sourceName}'");
            }
        }

        public int Count => _infos.Count;
    }
}