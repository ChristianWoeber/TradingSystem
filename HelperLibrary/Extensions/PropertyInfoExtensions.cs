using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Extensions
{
    public static class PropertyInfoExtensions
    {
        /// <summary>
        /// Erstellt dynamisch einen Getter für das Property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Func<T, object> CreateGetter<T>(this PropertyInfo propertyInfo)
        {

            if (propertyInfo.DeclaringType == null || !propertyInfo.DeclaringType.IsInstanceOf<T>())
            {
                throw new ArgumentException("Der Typ " + propertyInfo.DeclaringType + "" +
                                            " kann dem Typ " + typeof(T) + " nicht zugewiesen werden");
            }

            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var property = Expression.Property(instance, propertyInfo);
            var convert = Expression.TypeAs(property, typeof(object));
            return (Func<T, object>)Expression.Lambda(convert, instance).Compile();
        }

        /// <summary>
        /// Erstellt dynamisch einen Setter für das Property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Action<T, object> CreateSetter<T>(this PropertyInfo propertyInfo)
        {
            if (propertyInfo.DeclaringType == null || !propertyInfo.DeclaringType.IsInstanceOf<T>())
            {
                throw new ArgumentException("Der Typ " + propertyInfo.DeclaringType + "" +
                                            " kann dem Typ " + typeof(T) + " nicht zugewiesen werden");
            }

            var instance = Expression.Parameter(propertyInfo.DeclaringType, "i");
            var argument = Expression.Parameter(typeof(object), "a");

            var setterCall = Expression.Call(
                instance, propertyInfo.GetSetMethod(), Expression.Convert(argument, propertyInfo.PropertyType));
            return (Action<T, object>)Expression.Lambda(setterCall, instance, argument).Compile();
        }

        /// <summary>
        /// Gibt an, ob der Typ entweder eine Subklasse, der Typ selbst oder eine 
        /// Interfaceimplementierung des Typs ist
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsInstanceOf<T>(this Type type)
        {
            var cmp = typeof(T);
            if (cmp == type)
                return true;

            if (cmp.IsSubclassOf(type))
                return true;

            if (cmp.IsAssignableFrom(type))
                return true;

            return false;
        }
    }
}
