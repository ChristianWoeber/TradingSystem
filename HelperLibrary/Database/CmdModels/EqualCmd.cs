using System;
using System.Collections.Generic;
using System.Text;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database.CmdModels
{

    public class GreaterCmd : ISqlOperatorCmd
    {
        private List<KeyValuePair<string, object>> _lsKeyValue = new List<KeyValuePair<string, object>>();

        public string CreateOperatorCmd(params object[] values)
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
                    sb.Append($"{_lsKeyValue[i].Key}>");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value)}");
                }
                else
                {
                    sb.Append($"{_lsKeyValue[i].Key}>");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value)} AND ");
                }
            }
            return sb.ToString();
        }
    }

    public class LessCmd : ISqlOperatorCmd
    {
        private List<KeyValuePair<string, object>> _lsKeyValue = new List<KeyValuePair<string, object>>();

        public string CreateOperatorCmd(params object[] values)
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
                    sb.Append($"{_lsKeyValue[i].Key}<");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value)}");
                }
                else
                {
                    sb.Append($"{_lsKeyValue[i].Key}<");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value)} AND ");
                }
            }
            return sb.ToString();
        }
    }

    public class EqualCmd : ISqlOperatorCmd
    {
        private List<KeyValuePair<string, object>> _lsKeyValue = new List<KeyValuePair<string, object>>();

        public string CreateOperatorCmd(params object[] values)
        {      
            for (int i = 0; i < values.Length; i += 2)
                _lsKeyValue.Add(new KeyValuePair<string, object>((string)values[i], values[i + 1]));

            return BuildCmdString();
        }

        private string BuildCmdString()
        {
            var sb = new StringBuilder();
            //sb.Append(" where ");

            for (int i = 0; i < _lsKeyValue.Count; i++)
            {
                if (i == _lsKeyValue.Count - 1)
                {
                    sb.Append($"{_lsKeyValue[i].Key}=");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value)}");
                }
                else
                {
                    sb.Append($"{_lsKeyValue[i].Key}=");
                    sb.Append($"{DbTools.ParseObject(_lsKeyValue[i].Value)} AND ");
                }
            }
            return sb.ToString();
        }
    }
}
