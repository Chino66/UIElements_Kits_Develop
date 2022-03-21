using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UIElementsKits.UIFramework
{
    public class UI : View
    {
        public Dictionary<Type, View> Views;

        public UI()
        {
            Views = new Dictionary<Type, View>();
        }

        public T AddView<T>() where T : View
        {
            var type = typeof(T);
            if (Views.TryGetValue(type, out var v))
            {
                return (T) v;
            }

            var view = System.Activator.CreateInstance<T>();
            view.SetUI(this);
            view.Initialize(Self);
            Views.Add(type, view);
            return view;
        }

        public T GetView<T>() where T : View
        {
            var type = typeof(T);
            if (Views.TryGetValue(type, out var v))
            {
                return (T) v;
            }

            return null;
        }
    }
}