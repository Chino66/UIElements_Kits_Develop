using System;
using System.Linq;
using System.Reflection;
using DataBinding;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIElementsKits
{
    public class UIElementBinding
    {
        private readonly Binding _binding;
        public Binding Binding => _binding;

        public UIElementBinding(Binding binding)
        {
            this._binding = binding;
        }

        public UIElementBinding(IBindable bindable)
        {
            _binding = new Binding(bindable);
        }

        
        public void Bind<T>(T element) where T : BindableElement
        {
            _binding.Bind(element);
            /*/*1. 组件是否有BindableElement,有则获取绑定属性名称#1#
            if (!(element is BindableElement bindable))
            {
                Debug.LogError("element is not BindableElement");
                return;
            }

            var propertyName = bindable.bindingPath;

            /*2. 组件是否实现INotifyValueChanged<>接口,有则获取组件泛型类型#1#
            /*todo cache#1#
            var type = element.GetType();
            var interfaces = type.GetInterfaces();
            var genericType =
                (from itf in interfaces
                    where itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(INotifyValueChanged<>)
                    select itf.GenericTypeArguments[0]).FirstOrDefault();

            if (genericType == null)
            {
                Debug.LogError("genericType is null");
                return;
            }

            /*3. 数据类型#1#
            var propertyInfo = _binding.GetPropertyInfoByName(propertyName);
            if (propertyInfo == null)
            {
                Debug.LogError($"propertyInfo is null, propertyName is {propertyName}");
                return;
            }

            var propertyType = propertyInfo.PropertyType;

            var index = _binding.GetIndexByPropertyName(propertyName);
            if (propertyType == genericType)
            {
                var method = this.GetType()
                    .GetMethod(nameof(_bindSameType), BindingFlags.Static | BindingFlags.NonPublic);
                method = method.MakeGenericMethod(propertyType);
                /*todo delegate#1#
                method.Invoke(this, new object[] {_binding, index, element});
            }
            else
            {
                var method = this.GetType()
                    .GetMethod(nameof(_bindDiffType), BindingFlags.Static | BindingFlags.NonPublic);
                method = method.MakeGenericMethod(propertyType, genericType);
                /*todo delegate#1#
                method.Invoke(this, new object[] {_binding, index, element});
            }*/
        }
    }
}