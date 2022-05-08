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
            /*1. 组件是否有BindableElement,有则获取绑定属性名称*/
            if (!(element is BindableElement bindable))
            {
                Debug.LogError("element is not BindableElement");
                return;
            }

            var propertyName = bindable.bindingPath;

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

            /*else
            {
                var method = BindDiffTypeMethod.MakeGenericMethod(propertyType, genericType);
                /*todo delegate#1#
                method.Invoke(bindable, new object[] {binding, index, element});
            }*/
        }

        internal static void _bindSameType<T>(Binding binding, int index, INotifyValueChanged<T> valueChanged)
        {
            /*绑定赋初始值*/
            /*todo delegate?*/
            valueChanged.value = (T) binding.GetPropertyInfoByIndex(index).GetValue(binding.BindingObject);

            /*数据绑定组件*/
            void action(T o)
            {
                valueChanged.value = o;
            }

            valueChanged.RecordDataBindingCallback(action);
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

            valueChanged.RecordBindableElementCallback(callback);
            valueChanged.RegisterValueChangedCallback(callback);
        }

        internal static void _bindDiffType<T, T2>(Binding binding, int index, INotifyValueChanged<T2> valueChanged)
        {
            /*绑定赋初始值*/
            var v = (T) binding.GetPropertyInfoByIndex(index).GetValue(binding.BindingObject);
            /*todo delegate?*/
            valueChanged.value = (T2) Convert.ChangeType(v, typeof(T2));

            /*数据绑定组件*/
            binding.RegisterPostSetEvent<T>(index,
                o => { valueChanged.value = (T2) Convert.ChangeType(o, typeof(T2)); });

            /*组件绑定数据*/
            valueChanged.RegisterValueChangedCallback(evt =>
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
            });
        }

        /*
         * 关于数据和组件的绑定方向问题思考
         *  一个数据属性可以绑定多个组件
         *  一个组件是否允许绑定多个数据属性?
         *      实现是可以的,但是否合理?
         *          一个组件可以同时修改多个属性,但组件如何显示多个属性?
         *          在表现上有歧义,所以一个组件只绑定一个属性
         *          但是如果一个组件可以绑定多个属性,则可以实现一个修改关联,即几个属性都被关联起来了,是否需要?
         *          大大增加逻辑复杂度和数据结构复杂度,解决的问题也不重要,不实现
         * 
         */
        public static void UnBind(this BindableElement element)
        {
            /*1. 组件是否有BindableElement,有则获取绑定属性名称*/
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
        }

        internal static void _unbindSameType<T>(BindableElement element)
        {
            /*
             * todo need Binding or PropertyEvent
             */
            var elementEvent = element.GetBindableElementEvent<T>();

            var delegates = elementEvent.BindableElementCallback.GetInvocationList();

            for (int i = 0; i < delegates.Length; i++)
            {
                ((INotifyValueChanged<T>) element).UnregisterValueChangedCallback(
                    (EventCallback<ChangeEvent<T>>) delegates[i]);
            }
            
            /*todo clear record*/
            
            /*todo remove databinding event*/
        }
    }
}