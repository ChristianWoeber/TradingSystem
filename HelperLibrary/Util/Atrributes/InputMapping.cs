using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Util.Atrributes
{
    public class InputMapping : Attribute
    {
        public string[] KeyWords { get; set; }
    }
}
