using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UIElementsKits.DataBinding
{
    public static class BindableElementExtensions
    {
        private static readonly Dictionary<BindableElement, BindableElementEvent> BindableElementEvents;

        static BindableElementExtensions()
        {
            BindableElementEvents = new Dictionary<BindableElement, BindableElementEvent>();
        }

        internal static BindableElementEvent<T> GetBindableElementEvent<T>(this BindableElement element)
        {
            BindableElementEvents.TryGetValue(element, out var elementEvent);
            return (BindableElementEvent<T>) elementEvent;
        }


        public static void RecordDataBindingCallback<T>(this INotifyValueChanged<T> valueChanged,
            Action<T> action)
        {
            var bindableElement = valueChanged as BindableElement;
            if (BindableElementEvents.TryGetValue(bindableElement, out var elementEvent) == false)
            {
                elementEvent = new BindableElementEvent<T>();
                BindableElementEvents.Add(bindableElement, elementEvent);
            }

            ((BindableElementEvent<T>) elementEvent).DataBindingCallback += action;
        }

        public static void RecordBindableElementCallback<T>(this INotifyValueChanged<T> valueChanged,
            EventCallback<ChangeEvent<T>> action)
        {
            var bindableElement = valueChanged as BindableElement;
            if (BindableElementEvents.TryGetValue(bindableElement, out var elementEvent) == false)
            {
                elementEvent = new BindableElementEvent<T>();
                BindableElementEvents.Add(bindableElement, elementEvent);
            }

            ((BindableElementEvent<T>) elementEvent).BindableElementCallback += action;
        }
    }

    internal class BindableElementEvent
    {
    }

    internal class BindableElementEvent<T> : BindableElementEvent
    {
        public Action<T> DataBindingCallback;
        public EventCallback<ChangeEvent<T>> BindableElementCallback;
    }
}