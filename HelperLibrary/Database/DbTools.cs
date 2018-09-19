using System;
using System.Data;
using System.Data.Common;

namespace HelperLibrary.Database
{
    public class DbTools
    {
        private static DbCommand _dbCmd { get; set; }
        private static DbConnection _dbCon { get; set; }

        public static void Exec(DbConnection con, MySQLCommandBuilder builder)
        {
            try
            {
                //using (_dbCon = con)
                using (_dbCmd = con.CreateCommand())
                {
                    if (builder.SQLCmdType == SQLCommandTypes.Insert || builder.SQLCmdType == SQLCommandTypes.Delete
                        || builder.SQLCmdType == SQLCommandTypes.Update)
                    {

                        if (builder.OperatorsCmdBuilder.Length > 0)
                        {
                            builder.CmdBuilder.Append(" where ");
                            builder.CmdBuilder.Append(builder.OperatorsCmdBuilder);
                        }

                        _dbCmd.CommandText = builder.CmdBuilder.ToString();
                        _dbCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // Console.ReadKey();
            }
            finally
            {
                if (_dbCmd != null)
                    _dbCmd.Dispose();
                //if (_dbCon != null)
                //    _dbCon.Dispose();
            }
        }

        public static IDataReader CreateQuery(SQLCmd sqlCmd, DbConnection con, MySQLCommandBuilder builder)
        {
            try
            {
                _dbCon = con;
                _dbCmd = con.CreateCommand();

                if (builder.OperatorsCmdBuilder.Length > 0)
                {
                    if (!builder.CmdBuilder.ToString().EndsWith("And "))
                    {
                        // Achtung wird für alle Equals Conditions benötigt
                        builder.CmdBuilder.Append(" where ");
                    }
                    builder.CmdBuilder.Append(builder.OperatorsCmdBuilder);
                }

                _dbCmd.CommandText = builder.CmdBuilder.ToString();
                return _dbCmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        public static string ParseObject(object value)
        {
            if (value is string)
            {
                var tmp = value as string;
                return $"'{tmp}'";
            }
            else if (value is DateTime)
            {
                var tmp = (DateTime)value;
                return $"'{tmp.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else if (value is int)
            {
                var tmp = (int)value;
                return tmp.ToString();
            }
            else if (value is decimal)
            {
                var tmp = Math.Round((decimal)value, 3);
                return tmp.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
                return null;

        }

        public static object CastType(object value, Type type)
        {
            if (type.GetType() == typeof(int))
                return (int)value;

            return value;
        }
    }
}