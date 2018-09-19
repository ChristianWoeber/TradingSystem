using System;
using System.Collections.Generic;
using System.Text;
using HelperLibrary.Database.CmdModels;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database
{
    public class MySQLCommandBuilder
    {
        private Dictionary<SQLCommandTypes, ISqlCmdText> _cmdCache;
        private Dictionary<SQLValueTypes, ISqlValueCmd> _cmdValuesCache;
        private Dictionary<SQLOperators, ISqlOperatorCmd> _cmdOperatorCache;
        public StringBuilder CmdBuilder = new StringBuilder();
        public StringBuilder OperatorsCmdBuilder = new StringBuilder();
        public readonly SQLCommandTypes SQLCmdType;
        public SQLValueTypes SQLValueType;
        public SQLOperators SQLOperator;
        public List<SQLOperators> SQLOperatorsCollection = new List<SQLOperators>();


        public MySQLCommandBuilder(SQLCommandTypes cmdtype, string db, string table)
        {
            SQLCmdType = cmdtype;
            GetCmdText(cmdtype, db, table);
        }

        public void IsNotNull(string field)
        {
            SQLOperator = SQLOperators.NotNull;
            if (SQLOperatorsCollection.Count > 0)
                OperatorsCmdBuilder.Append(" And ");
            SQLOperatorsCollection.Add(SQLOperator);
            CreateOperatorsCmdText(SQLOperator, field);
        }

        private string GetCmdText(SQLCommandTypes cmdtype, string db, string table)
        {
            if (_cmdCache == null)
                LoadCache();

            var ret = _cmdCache.ContainsKey(cmdtype) ? _cmdCache[cmdtype].CreateCmd(db, table) : null;
            CmdBuilder.Append(ret);
            return ret;
        }

        private void LoadCache()
        {
            _cmdCache = new Dictionary<SQLCommandTypes, ISqlCmdText>();
            foreach (SQLCommandTypes cmdEnum in typeof(SQLCommandTypes).GetEnumValues())
            {
                switch (cmdEnum)
                {
                    case SQLCommandTypes.Insert:
                        if (!_cmdCache.ContainsKey(cmdEnum))
                            _cmdCache.Add(cmdEnum, new InsertCmd());
                        break;
                    case SQLCommandTypes.Delete:
                        if (!_cmdCache.ContainsKey(cmdEnum))
                            _cmdCache.Add(cmdEnum, new DeleteCmd());
                        break;
                    case SQLCommandTypes.Update:
                        if (!_cmdCache.ContainsKey(cmdEnum))
                            _cmdCache.Add(cmdEnum, new UpdateCmd());
                        break;
                    case SQLCommandTypes.Select:
                        if (!_cmdCache.ContainsKey(cmdEnum))
                            _cmdCache.Add(cmdEnum, new SelectCmd());
                        break;
                    case SQLCommandTypes.Call:
                        if (!_cmdCache.ContainsKey(cmdEnum))
                            _cmdCache.Add(cmdEnum, new CallCmd());
                        break;
                    default:
                        break;
                }
            }
        }

        public void Greater(object[] values)
        {
            SQLOperator = SQLOperators.Equal;
            if (SQLOperatorsCollection.Count > 0)
                OperatorsCmdBuilder.Append(" And ");
            else if (CmdBuilder.ToString().Contains("where"))
                CmdBuilder.Append(" And ");

            SQLOperatorsCollection.Add(SQLOperator);

            CreateOperatorsCmdText(SQLOperator, values);
        }

        public void Equal(params object[] values)
        {
            SQLOperator = SQLOperators.Equal;
            if (SQLOperatorsCollection.Count > 0)
                OperatorsCmdBuilder.Append(" And ");
            else if (CmdBuilder.ToString().Contains("where"))
                CmdBuilder.Append(" And ");

            SQLOperatorsCollection.Add(SQLOperator);

            CreateOperatorsCmdText(SQLOperator, values);
        }

        public void Less(object[] values)
        {
            SQLOperator = SQLOperators.Less;
            if (SQLOperatorsCollection.Count > 0)
                OperatorsCmdBuilder.Append(" And ");
            else if (CmdBuilder.ToString().Contains("where"))
                CmdBuilder.Append(" And ");

            SQLOperatorsCollection.Add(SQLOperator);

            CreateOperatorsCmdText(SQLOperator, values);
        }

        private string CreateOperatorsCmdText(SQLOperators cmdType, params object[] values)
        {
            if (_cmdOperatorCache == null)
                LoadOperatorsCmdCache();

            var ret = _cmdOperatorCache.ContainsKey(cmdType) ? _cmdOperatorCache[cmdType].CreateOperatorCmd(values) : null;
            OperatorsCmdBuilder.Append(ret);


            return ret;
        }

        public void Fields(string[] fields)
        {
            SQLValueType = SQLValueTypes.Fields;
            CreateFieldsCmdText(SQLValueTypes.Fields, fields);
        }

        private string CreateFieldsCmdText(SQLValueTypes cmdType, string[] fields)
        {
            if (_cmdValuesCache == null)
                LoadValuesCmdCache();

            var ret = _cmdValuesCache.ContainsKey(cmdType) ? _cmdValuesCache[cmdType].CreateCmd(null, fields) : null;
            CmdBuilder.Replace("@", ret);
            return ret;
        }

        private void LoadOperatorsCmdCache()
        {
            _cmdOperatorCache = new Dictionary<SQLOperators, ISqlOperatorCmd>();
            foreach (SQLOperators cmdEnum in typeof(SQLOperators).GetEnumValues())
            {
                switch (cmdEnum)
                {
                    case SQLOperators.Greater:
                        if (!_cmdOperatorCache.ContainsKey(cmdEnum))
                            _cmdOperatorCache.Add(cmdEnum, new GreaterCmd());
                        break;
                    case SQLOperators.Less:
                        if (!_cmdOperatorCache.ContainsKey(cmdEnum))
                            _cmdOperatorCache.Add(cmdEnum, new LessCmd());
                        break;
                    case SQLOperators.Equal:
                        if (!_cmdOperatorCache.ContainsKey(cmdEnum))
                            _cmdOperatorCache.Add(cmdEnum, new EqualCmd());
                        break;
                    case SQLOperators.NotNull:
                        if (!_cmdOperatorCache.ContainsKey(cmdEnum))
                            _cmdOperatorCache.Add(cmdEnum, new IsNotNullCmd());
                        break;
                    default:
                        break;
                }
            }
        }

        public void Values(params string[] values)
        {
            SQLValueType = SQLValueTypes.Values;
            CreateValuesCmdText(SQLValueTypes.Values, values);
        }

        private string CreateValuesCmdText(SQLValueTypes cmdType, object[] values, string field = null)
        {
            if (_cmdValuesCache == null)
                LoadValuesCmdCache();

            // UpdateValues
            var ret = _cmdValuesCache.ContainsKey(cmdType) ? _cmdValuesCache[cmdType].CreateCmd(field, values) : null;
            CmdBuilder.Append(ret);
            return ret;
        }

        private void LoadValuesCmdCache()
        {
            _cmdValuesCache = new Dictionary<SQLValueTypes, ISqlValueCmd>();
            foreach (SQLValueTypes cmdEnum in typeof(SQLValueTypes).GetEnumValues())
            {
                switch (cmdEnum)
                {
                    case SQLValueTypes.Values:
                        if (!_cmdValuesCache.ContainsKey(cmdEnum))
                            _cmdValuesCache.Add(cmdEnum, new ValuesCmd());
                        break;
                    case SQLValueTypes.Fields:
                        if (!_cmdValuesCache.ContainsKey(cmdEnum))
                            _cmdValuesCache.Add(cmdEnum, new FieldsCmd());
                        break;
                    case SQLValueTypes.UpdateValues:
                        if (!_cmdValuesCache.ContainsKey(cmdEnum))
                            _cmdValuesCache.Add(cmdEnum, new UpdateValuesCmd());
                        break;
                    case SQLValueTypes.InList:
                        if (!_cmdValuesCache.ContainsKey(cmdEnum))
                            _cmdValuesCache.Add(cmdEnum, new InListValuesCmd());
                        break;
                    default:
                        break;
                }
            }
        }

        public void CallProcedure(string procedureName, object[] arguments)
        {
            CmdBuilder.Append($"{procedureName}(");
            var cnt = 0;
            foreach (var arg in arguments)
            {
                cnt++;
                if (arguments?.Length > 1)
                {
                    if (cnt == arguments?.Length)
                        CmdBuilder.Append($"{Parse(arg)}");
                    else
                        CmdBuilder.Append($"{Parse(arg)},");
                }
                else
                {
                    CmdBuilder.Append($"{Parse(arg)}");
                }
            }
            CmdBuilder.Append(")");

        }

        private string Parse(object arg)
        {
            if (!(arg is DateTime))
                return arg.ToString();

            return $"'{((DateTime)arg).ToString("yyyy-MM-dd")}'";
        }

        public void CreateValueTypesCmd(SQLValueTypes type, object[] values, string field = null)
        {
            CreateValuesCmdText(type, values, field);
        }
    }
}