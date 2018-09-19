using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database.CmdModels
{
    public class UpdateCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"UPDATE {db}.{table} set ";
        }
    }
}
