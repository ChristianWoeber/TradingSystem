using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
