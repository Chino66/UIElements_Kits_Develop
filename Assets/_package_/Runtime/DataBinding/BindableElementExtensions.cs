using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace UIElementsKits.DataBinding
{
    internal class UnbindAction
    {
        public Action Action;

        public void Invoke()
        {
            Action?.Invoke();
            Action = null;
        }
    }
    
    internal static class BindableElementExtensions
    {
        private static readonly ConditionalWeakTable<BindableElement, UnbindAction>
            BindableElementUnbindActions;

        static BindableElementExtensions()
        {
            BindableElementUnbindActions = new ConditionalWeakTable<BindableElement, UnbindAction>();
        }

        internal static void RecordUnbindAction(this BindableElement element, Action action)
        {
            if (BindableElementUnbindActions.TryGetValue(element, out var unbindAction) == false)
            {
                unbindAction = new UnbindAction();
                BindableElementUnbindActions.Add(element, unbindAction);
            }

            unbindAction.Action += action;
        }

        internal static void InvokeUnbindAction(this BindableElement element)
        {
            BindableElementUnbindActions.TryGetValue(element, out var unbindAction);
            unbindAction?.Invoke();
        }
    }
}