using HelperLibrary.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Reflection;

namespace HelperLibrary.Database
{
    internal class ObjectMapper<T>
    {
        
        private static Dictionary<string, Action<T, object>> _setterFuncsDictionary { get; } = new Dictionary<string, Action<T, object>>();

        internal static T Create(IDataReader rd)
        {
            //Create instance of the object 
            var obj = Activator.CreateInstance<T>();

            foreach (var pi in typeof(T).GetProperties())
            {
                if (!_setterFuncsDictionary.ContainsKey(pi.Name))
                {
                    var setterFunc = pi.CreateSetter<T>();
                    _setterFuncsDictionary.Add(pi.Name, setterFunc);
                }

                var storageAttribute = pi.GetCustomAttribute<DataAttribute>()?.Storage;

                if (!string.IsNullOrWhiteSpace(storageAttribute))
                {
                    try
                    {                     
                        var dbValue = rd[storageAttribute];
                        var propertyValue = TypeConversion(dbValue, pi.PropertyType);

                        if (_setterFuncsDictionary.ContainsKey(pi.Name))
                        {
                            //Get the setterFunc from Dictionary
                            var setter = _setterFuncsDictionary[pi.Name];
                            //set value via expression
                            setter(obj, propertyValue);
                        }
                       // pi.SetValue(obj, propertyValue);
                      
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(ex.Message);
                    }
                }
            }
            return obj;
        }

        private static object TypeConversion(object dbValue, Type propertyType)
        {
            if (propertyType == typeof(int))
            {
                return Convert.ToInt32(dbValue);
            }
            else if (propertyType == typeof(int?))
            {
                if (dbValue == DBNull.Value)
                    return null;

                return Convert.ToInt32(dbValue);
            }

            else if (propertyType == typeof(string))
            {
                var tmp = dbValue.ToString();
                return tmp;
            }
            else if (propertyType == typeof(DateTime))
            {
                var tmp = (DateTime)dbValue;
                return tmp;
            }
            else if (propertyType == typeof(decimal))
            {
                var tmp = (decimal)dbValue;
                return tmp;
            }
            else if (propertyType == typeof(double))
            {
                var tmp = (double)dbValue;
                return tmp;
            }

            return null;
        }
    }
}