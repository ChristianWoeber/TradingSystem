using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Database.Interfaces
{
    public interface ISqlOperatorCmd
    {
        string CreateOperatorCmd(params object[] values);
    }
}
