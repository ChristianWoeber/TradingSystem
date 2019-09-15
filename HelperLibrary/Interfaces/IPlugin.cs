using System.Collections.Generic;

namespace HelperLibrary.Interfaces
{
    public interface IPluginBase
    {
        string Name { get; }
        string Description { get; }
    }

    public interface IDownloadPlugin : IPluginBase
    {
        void Download(ICollection<string> keys, bool needsHistoryUpdate = false);
    }

    public interface ICaclulationPlugin : IPluginBase
    {
        void Calculate(string key);
    }
}
