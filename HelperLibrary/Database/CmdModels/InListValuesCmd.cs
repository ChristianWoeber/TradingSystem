using System;
using HelperLibrary.Database.Interfaces;
using System.Text;

namespace HelperLibrary.Database.CmdModels
{
    public class InListValuesCmd : ISqlValueCmd
    {
        public string CreateCmd(string field, params object[] values)
        {
            var sb = new StringBuilder();
            sb.Append($" where {field}");
            sb.Append(" in(");
            GetValues(sb, values);

            sb.Append(")");
            return sb.ToString();
        }

        private void GetValues(StringBuilder sb, object[] values)
        {
            var cnt = 0;
            foreach (var item in values)
            {
                cnt++;
                if (cnt == values.Length)
                    sb.Append($"{item}");
                else
                    sb.Append($"{item},");
            }
        }
    }
}