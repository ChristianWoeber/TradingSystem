using System.Data.Linq.Mapping;

namespace HelperLibrary.Database.Models
{
    public class ValidationTypes
    {
        [Column(Storage = "ID_")]
        public int Id { get; set; }

        [Column(Storage = "TYPE")]
        public string TypeDescription { get; set; }

    }
}
