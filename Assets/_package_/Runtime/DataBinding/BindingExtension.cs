using System;
using System.Linq;
using System.Reflection;
using DataBinding;
using UIElementsKits.DataBinding;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIElementsKits.DataBinding
{
    public static class BindingExtension
    {
        private static readonly MethodInfo BindSameTypeMethod;
        private static readonly MethodInfo BindDiffTypeMethod;

        static BindingExtension()
        {
            var type = typeof(BindingExtension);

            BindSameTypeMethod = type.GetMethod(nameof(_bindSameType),
                BindingFlags.Static | BindingFlags.NonPublic);

            BindDiffTypeMethod = type.GetMethod(nameof(_bindDiffType),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        public static void Bind(this Binding binding, VisualElement element)
        {
            var queryBuilder = element.Query<BindableElement>();
            queryBuilder.ForEach(be =>
            {
                if (string.IsNullOrEmpty(be.bindingPath) || be.bindingPath == "")
                {
                    return;
                }

                binding.Bind(be);
            });
        }

        public static void Bind<T>(this Binding binding, T element) where T : BindableElement
        {
            /*1. 组件是否继承自BindableElement*/
            if (!(element is BindableElement bindable))
            {
                Debug.LogError("element is not BindableElement");
                return;
            }

            /*2. 组件是否实现INotifyValueChanged<>接口,有则获取组件泛型类型*/
            /*todo cache*/
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

            /*3. 数据类型*/
            var propertyName = bindable.bindingPath;
            var propertyInfo = binding.GetPropertyInfoByName(propertyName);
            if (propertyInfo == null)
            {
                Debug.LogError($"propertyInfo is null, propertyName is {propertyName}");
                return;
            }

            var propertyType = propertyInfo.PropertyType;

            var index = binding.GetIndexByPropertyName(propertyName);
            if (propertyType == genericType)
            {
                var method = BindSameTypeMethod.MakeGenericMethod(propertyType);
                /*todo delegate*/
                method.Invoke(bindable, new object[] {binding, index, element});
            }
            else
            {
                var method = BindDiffTypeMethod.MakeGenericMethod(propertyType, genericType);
                /*todo delegate*/
                method.Invoke(bindable, new object[] {binding, index, element});
            }
        }

        internal static void _bindSameType<T>(Binding binding, int index, BindableElement element)
        {
            INotifyValueChanged<T> valueChanged = (INotifyValueChanged<T>) element;
            /*绑定赋初始值*/
            /*todo delegate?*/
            valueChanged.value = (T) binding.GetPropertyInfoByIndex(index).GetValue(binding.BindingObject);

            /*数据绑定组件*/
            void action(T o)
            {
                valueChanged.value = o;
            }

            binding.RegisterPostSetEvent<T>(index, action);

            /*组件绑定数据*/
            void callback(ChangeEvent<T> evt)
            {
                var value = binding.GetPropertyValue<T>(index);
                if (value == null || value.Equals(evt.newValue) == false)
                {
                    binding.SetPropertyValue(index, evt.newValue);
                }
            }

            valueChanged.RegisterValueChangedCallback(callback);

            /*取消绑定的委托*/
            void unbind()
            {
                binding.UnregisterPostSetEvent<T>(index, action);
                valueChanged.UnregisterValueChangedCallback(callback);
            }

            element.RecordUnbindAction(unbind);
        }

        internal static void _bindDiffType<T, T2>(Binding binding, int index, BindableElement element)
        {
            INotifyValueChanged<T2> valueChanged = (INotifyValueChanged<T2>) element;
            /*绑定赋初始值*/
            var v = (T) binding.GetPropertyInfoByIndex(index).GetValue(binding.BindingObject);
            /*todo delegate?*/
            valueChanged.value = (T2) Convert.ChangeType(v, typeof(T2));

            /*数据绑定组件*/
            void action(T o)
            {
                valueChanged.value = (T2) Convert.ChangeType(o, typeof(T2));
            }

            binding.RegisterPostSetEvent<T>(index, action);

            /*组件绑定数据*/
            void callback(ChangeEvent<T2> evt)
            {
                /*todo cache*/
                var method = typeof(T).GetMethod("Parse", new[] {typeof(string)});
                T value = default;
                if (method != null)
                {
                    /*todo delegate*/
                    value = (T) method.Invoke(null, new object[] {evt.newValue});
                }
                else
                {
                    value = (T) Convert.ChangeType(evt.newValue, typeof(T));
                }

                if (value == null || binding.GetPropertyValue<T>(index).Equals(value) == false)
                {
                    binding.SetPropertyValue(index, value);
                }
            }

            valueChanged.RegisterValueChangedCallback(callback);

            /*取消绑定的委托*/
            void unbind()
            {
                binding.UnregisterPostSetEvent<T>(index, action);
                valueChanged.UnregisterValueChangedCallback(callback);
            }

            element.RecordUnbindAction(unbind);
        }

        public static void UnBind(this VisualElement element)
        {
            var queryBuilder = element.Query<BindableElement>();
            queryBuilder.ForEach(be =>
            {
                if (string.IsNullOrEmpty(be.bindingPath) || be.bindingPath == "")
                {
                    return;
                }

                be.InvokeUnbindAction();
            });
        }
    }
}