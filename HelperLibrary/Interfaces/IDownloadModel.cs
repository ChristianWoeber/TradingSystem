using HelperLibrary.Database.Models;

namespace HelperLibrary.Interfaces
{
    public interface IDownloadModel
    {
        Security DbSecurity { get; set; }

        bool IsValid { get; set; }
    }
}
