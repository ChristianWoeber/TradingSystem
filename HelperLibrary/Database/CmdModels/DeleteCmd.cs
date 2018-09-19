using HelperLibrary.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Database.CmdModels
{
    public class DeleteCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"Delete from {db}.{table}";
        }
    }
}
