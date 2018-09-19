using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
