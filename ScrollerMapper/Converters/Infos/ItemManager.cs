using System.Collections.Generic;
using System.Linq;

namespace ScrollerMapper.Converters.Infos
{
    internal class ItemManager
    {
        private readonly Dictionary<string, ItemInfos> _lists = new Dictionary<string, ItemInfos>();

        public void Add(string itemType, string name, uint offset)
        {
            if (!_lists.ContainsKey(itemType))
            {
                _lists.Add(itemType, new ItemInfos(itemType));
            }

            _lists[itemType].Add(name, _lists[itemType].Count, offset);
        }

        public ItemInfo Get(string itemType, string name, string sourceName)
        {
            return _lists[itemType].Get(name, sourceName);
        }

        public ItemInfos Get(string itemType)
        {
            return _lists[itemType];
        }

        public bool HasAll(IEnumerable<string> itemTypes)
        {
            return itemTypes.All(s => _lists.Keys.Contains(s));
        }

        public IEnumerable<string> AvailableTypes()
        {
            return _lists.Keys;
        }
    }
}