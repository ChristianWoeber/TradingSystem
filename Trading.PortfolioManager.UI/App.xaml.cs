using System.Linq;
using System.Reflection;
using System.Windows;

namespace Trading.PortfolioManager.UI.Wpf
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Löst das <see cref="E:System.Windows.Application.Startup" />-Ereignis aus.</summary>
        /// <param name="e">Ein <see cref="T:System.Windows.StartupEventArgs" />, das die Ereignisdaten enthält.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            //var container = ContainerConfig.Configure();
            //using (var scope = container.BeginLifetimeScope())
            //{
            //    var win = scope.Resolve<MainWindow>();
            //    win.Show();
            //}

        }
    }

    //public static class ContainerConfig
    //{
    //    public static IContainer Configure()
    //    {
    //        var builder = new ContainerBuilder();
    //        builder.RegisterType<MainWindow>().AsSelf();
    //        builder.RegisterType<P>().As<IPortfolioManager>();

    //        foreach (var t in Assembly.Load(nameof(HelperLibrary)).GetTypes().Where(t => t.Namespace?.Contains("Trading") == true || t.Namespace?.Contains("Models") == true || t.Namespace?.Contains("DataStructure") == true))
    //        {
    //            System.Diagnostics.Trace.TraceInformation(t.Namespace);
    //        }

    //        builder.RegisterAssemblyTypes(Assembly.Load(nameof(Trading.DataStructures)), Assembly.Load(nameof(HelperLibrary.Trading)), Assembly.Load(nameof(HelperLibrary.Database.Models)))
    //            .Where(t => t.Namespace?.Contains("Trading") == true || t.Namespace?.Contains("Models") == true || t.Namespace?.Contains("Interfaces") == true)
    //            .As(t => t?.GetInterfaces().FirstOrDefault(i => i.Name == $"I{t.Name}"));

    //        return builder.Build();
    //    }
    //}
}
