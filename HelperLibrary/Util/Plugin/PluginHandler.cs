using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HelperLibrary.Interfaces;
using HelperLibrary.Extensions;

namespace HelperLibrary.Util.Plugin
{
    public static class PluginHandler
    {
        private const string MAINROOT = @"Z:\VisualStudio\Plugins";
        private const string MAINROOT_SERVER = @"F:\VisualStudio\Plugins";

        private static Dictionary<string, AssemblyObj> _pluginCache;

        private static IDownloadPlugin GetDownloadPlugin(AssemblyObj obj, string pluginName)
        {
            var plugin = (IDownloadPlugin)obj.Instance;
            return plugin.Name.ToLower().Equals(pluginName.ToLower()) ? plugin : null;
        }

        public static IPluginBase InvokePlugin(string pluginName)
        {
            if (_pluginCache == null)
                LoadPlugins(pluginName);

            var obj = GetPlugin(pluginName);

            return obj.Instance as IPluginBase;
            //var derivedInterface = obj.Interfaces.Where(x => !x.Name.ToLower().Contains("base")).FirstOrDefault();

            //if (derivedInterface.Name == nameof(IDownloadPlugin))
            //{
            //    return GetDownloadPlugin(obj, pluginName);
            //}
            //else if (derivedInterface.Name == nameof(ICaclulationPlugin))
            //{
            //    var plugin = (ICaclulationPlugin)obj.Instance;
            //    return plugin.Name.ToLower().Equals(pluginName.ToLower()) ? plugin : null;
            //}
            //return null;
        }

        public static AssemblyObj GetPlugin(string pluginName)
        {
            if (pluginName == null)
                throw new NullReferenceException("Achtung pluginName ist null!!");

            return _pluginCache.ContainsKey(pluginName) ? _pluginCache[pluginName] : null;
        }

        private static IPluginBase LoadPlugins(string plugin)
        {
            _pluginCache = new Dictionary<string, AssemblyObj>();
            string[] files;
            if (Environment.UserName.ContainsIC("Administrator"))
                files = Directory.GetFiles(MAINROOT_SERVER, "*.dll", SearchOption.AllDirectories);
            else
                files = Directory.GetFiles(MAINROOT, "*.dll", SearchOption.AllDirectories);

            var baseType = typeof(IPluginBase);

            foreach (var file in files.Where(x => !x.Contains("obj")))
            {
                var fileInfo = new FileInfo(file);

                if (fileInfo.Extension.Equals(".dll"))
                {
                    var key = fileInfo.Name.Substring(0, fileInfo.Name.LastIndexOf("."));
                    if (!_pluginCache.ContainsKey(file))
                    {
                        var value = GetModul(file, baseType);
                        if (value != null)
                            _pluginCache.Add(key, value);
                    }
                }
            }
            return null;
        }

        public class AssemblyObj : Tuple<object, Type[]>
        {
            public AssemblyObj(object instance, Type[] interfaces) : base(instance, interfaces)
            {

            }

            public object Instance => Item1;
            public Type[] Interfaces => Item2;
        }

        private static AssemblyObj GetModul(string filename, Type basetype)
        {
            if (filename.ContainsIC("HelperLibrary") || filename.ContainsIC("HtmlAgilityPack") ||
                filename.ContainsIC("Sql") || filename.ContainsIC("YahooLibrary"))
                return null;
            //Loads an assembly given its file name or path.
            var assembly = Assembly.LoadFrom(filename);
            // Assembly Eigenschaften checken
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsPublic) // Ruft einen Wert ab, der angibt, ob der Type als öffentlich deklariert ist. 
                {
                    if (!type.IsAbstract)  //nur Assemblys verwenden die nicht Abstrakt sind
                    {
                        if (type.IsClass)
                        {
                            try
                            {
                                var instance = Activator.CreateInstance(type);
                                var interfaces = type.GetInterfaces();

                                if (instance != null && interfaces.Length > 0)
                                {
                                    return new AssemblyObj(instance, interfaces);
                                }
                            }
                            catch (Exception exception)
                            {
                                System.Diagnostics.Debug.WriteLine(exception);
                            }
                        }
                    }
                }
            }

            return null;
        }


    }
}
