using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UIElementsKits
{
    public class VisualElementCache
    {
        private Dictionary<string, VisualElement> _cache = new Dictionary<string, VisualElement>();

        private VisualElement _root;

        public VisualElementCache(VisualElement root)
        {
            _root = root;
        }

        private T Create<T>(string query) where T : VisualElement
        {
            return _root.Q<T>(query);
        }

        public VisualElement Get(string query)
        {
            return Get<VisualElement>(query);
        }

        public T Get<T>(string query) where T : VisualElement
        {
            if (!_cache.ContainsKey(query))
            {
                _cache[query] = Create<T>(query);
            }

            return _cache[query] as T;
        }

        private VisualElement Create(string query)
        {
            return _root.Q(query);
        }
    }
}