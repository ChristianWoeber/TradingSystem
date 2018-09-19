using System;
using System.Text;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database
{
    public class FieldsCmd : ISqlValueCmd
    {
        public string CreateCmd(string field, params object[] fields)
        {
            var sb = new StringBuilder();
            if (fields.Length == 1)
            {
                sb.Append($"{fields[0]}");
            }
            else
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (i == fields.Length - 1)
                        sb.Append($"{fields[i]}");
                    else
                        sb.Append($"{fields[i]},");
                }
            }
            return sb.ToString();
        }
    }
}