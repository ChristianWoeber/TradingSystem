using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database.CmdModels
{
    public class UpdateValuesCmd : ISqlValueCmd
    {
        public string CreateCmd(string field, params object[] values)
        {
            //triviale Implementierung gehe von value pairs aus 1 FieldName, 2 Value, FieldName, Value //
            // daher werden alle Fieldnames und alle Values gesammelt um folgende Syntax zu erhalten//
            // (Fieldname1,Fieldnmae2)Values(Value1,value2)

            return MultipleValuesCmd(values);
        }

        private List<KeyValuePair<string, object>> _lsKeyValue = new List<KeyValuePair<string, object>>();
        private string MultipleValuesCmd(object[] values)
        {
            for (int i = 0; i < values.Length; i += 2)
                _lsKeyValue.Add(new KeyValuePair<string, object>((string)values[i], values[i + 1]));

            return BuildCmdString();
        }

        private string BuildCmdString()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < _lsKeyValue.Count; i++)
            {
                if (i == _lsKeyValue.Count - 1)
                {
                    sb.Append($"{_lsKeyValue[i].Key}=");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value) ?? "Null"}");
                }
                else
                {
                    sb.Append($"{_lsKeyValue[i].Key}=");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value) ?? "Null"},");
                }
            }
            return sb.ToString();
        }
    }
}

