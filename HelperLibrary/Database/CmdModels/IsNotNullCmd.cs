using System;
using System.Text;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database
{
    internal class IsNotNullCmd : ISqlOperatorCmd
    {
        public string CreateOperatorCmd(params object[] values)
        {
            var sb = new StringBuilder();
            //sb.Append(" Where ");

            var cnt = 0;
            foreach (var field in values)
            {
                cnt++;
                if (cnt == values.Length)
                    sb.Append($"{field} is Not Null");
                else
                    sb.Append($"{field} is Not Null AND");
            }

            return sb.ToString();
        }
    }
}