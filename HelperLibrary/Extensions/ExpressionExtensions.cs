#region Usings

using System;
using System.Reflection;
using System.Reflection.Emit;

#endregion

namespace HelperLibrary.Extensions
{
    public static class ExpressionExtensions
    {
        /// <summary>
        ///     Creates a dynamic setter for the property
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Action<object, object> CreateSetMethod(this PropertyInfo propertyInfo)
        {
            /*
            * If there's no setter return null
            */
            var setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                return null;

            /*
            * CreateAsync the dynamic method
            */
            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof (object);

            if (propertyInfo.DeclaringType != null)
            {
                var setter = new DynamicMethod(
                    string.Concat("_Set", propertyInfo.Name, "_"),
                    typeof (void), arguments, propertyInfo.DeclaringType);
                var generator = setter.GetILGenerator();
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                generator.Emit(OpCodes.Ldarg_1);

                if (propertyInfo.PropertyType.IsClass)
                    generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
                else
                    generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

                generator.EmitCall(OpCodes.Callvirt, setMethod, null);
                generator.Emit(OpCodes.Ret);

                /*
            * CreateAsync the delegate and return it
            */
                return (Action<object, object>) setter.CreateDelegate(typeof (Action<object, object>));
            }

            return null;
        }

        /// <summary>
        ///     Creates a dynamic getter for the property
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static Func<object, object> CreateGetMethod(this PropertyInfo propertyInfo)
        {
            /*
            * If there's no getter return null
            */
            var getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
                return null;

            /*
            * CreateAsync the dynamic method
            */
            var arguments = new Type[1];
            arguments[0] = typeof (object);

            if (propertyInfo.DeclaringType != null)
            {
                var getter = new DynamicMethod(
                    string.Concat("_Get", propertyInfo.Name, "_"),
                    typeof (object), arguments, propertyInfo.DeclaringType);
                var generator = getter.GetILGenerator();
                generator.DeclareLocal(typeof (object));
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
                generator.EmitCall(OpCodes.Callvirt, getMethod, null);

                if (!propertyInfo.PropertyType.IsClass)
                    generator.Emit(OpCodes.Box, propertyInfo.PropertyType);

                generator.Emit(OpCodes.Ret);

                /*
            * CreateAsync the delegate and return it
            */
                return (Func<object, object>) getter.CreateDelegate(typeof (Func<object, object>));
            }
            return null;
        }
    }
}