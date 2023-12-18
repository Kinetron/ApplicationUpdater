using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DbAdaptor
{
	/// <summary>
	/// Основные функции для работы с БД MS Access. Внимание! Драйвер работает только с x86 конфигурацией.
	/// </summary>
	public class MsAccessAdaptor
	{
		private readonly string _pathToDb;

		public MsAccessAdaptor(string pathToDb)
		{
			if (string.IsNullOrEmpty(pathToDb))
				throw new ArgumentNullException($"База данных по пути {pathToDb} не найдена");
			_pathToDb = pathToDb;
		}

		public OdbcConnection OpenConnection()
		{
			OdbcConnection connection = null;
			string str = "Driver={Microsoft Access Driver (*.mdb)};DBQ=" + _pathToDb + ";";
			connection = new OdbcConnection(str);
			connection.Open();
			return connection;
		}

		public IEnumerable<T> Query<T>(string sql, DbConnection connection, DbTransaction transaction = null,
			object param = null)
		{
			var dtBeg = DateTime.Now;
			var Mess = "";
			try
			{
				DbCommand command = connection.CreateCommand();
				command.Transaction = transaction;
				command.CommandTimeout = 3000;
				command.CommandText = sql;

				List<T> entryes = new List<T>();
				PropertyInfo[] Props =
					typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
				Dictionary<string, PropertyInfo> propDict = new Dictionary<string, PropertyInfo>();
				foreach (var prop in Props)
				{
					propDict.Add(prop.Name.ToLower(), prop);
				}

				if (param != null)
				{
					PropertyInfo[] PropsParam = param.GetType()
						.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
					foreach (var prop in PropsParam)
					{
						if (prop.PropertyType == typeof(string))
						{
							var text = (string)prop.GetValue(param, null) ?? "";
							if (text.Length > 255)
							{
								OdbcParameter pBigText = new OdbcParameter("@" + prop.Name, OdbcType.Text);
								pBigText.Value = text;
								command.Parameters.Add(pBigText);
							}
							else
							{
								OdbcParameter p = new OdbcParameter("@" + prop.Name, text);
								command.Parameters.Add(p);
							}
						}
						else
						{
							OdbcParameter p = new OdbcParameter("@" + prop.Name, prop.GetValue(param, null));
							command.Parameters.Add(p);
						}
					}

					var CommandParams = new List<OdbcParameter>();
					command.CommandText = Regex.Replace(command.CommandText, "(@\\w*)", match =>
					{
						var parameter = command.Parameters.OfType<OdbcParameter>().FirstOrDefault(a =>
							a.ParameterName.ToLower() == match.Groups[1].Value.ToLower());
						if (parameter != null)
						{
							var parameterIndex = CommandParams.Count;

							var newParameter = (OdbcParameter)command.CreateParameter();
							newParameter.OdbcType = parameter.OdbcType;
							newParameter.ParameterName = "@parameter" + parameterIndex.ToString();
							newParameter.Value = parameter.Value;

							CommandParams.Add(newParameter);
						}

						return "?";
					});
					command.Parameters.Clear();
					command.Parameters.AddRange(CommandParams.ToArray());
				}

				DbDataReader rdr = command.ExecuteReader();
				List<Type> PrimaryTypes = new List<Type>()
				{
					typeof(int),
					typeof(long),
					typeof(string),
					typeof(float),
					typeof(double),
					typeof(decimal),
					typeof(bool),
					typeof(DateTime),

					typeof(int?),
					typeof(long?),
					typeof(float?),
					typeof(decimal?),
					typeof(bool?),
					typeof(DateTime?)
				};
				while (rdr.Read())
				{
					T entry = default(T);
					var type = typeof(T);
					var UnderlyingType = Nullable.GetUnderlyingType(type);
					if (UnderlyingType != null)
					{
						type = UnderlyingType;
					}

					if (type == typeof(string))
					{
						entry = (T)(object)"";
					}
					else
					{
						entry = (T)Activator.CreateInstance(typeof(T));
					}

					for (int ic = 0; ic < rdr.FieldCount; ic++)
					{
						var fieldName = rdr.GetName(ic).ToLower();
						if (PrimaryTypes.Contains(type))
						{
							if (rdr.IsDBNull(ic))
							{
								entry = default(T);
							}
							else
							{
								if (type == typeof(string))
								{
									entry = (T)(object)rdr.GetValue(ic).ToString();
								}
								else
								{
									entry = (T)rdr.GetValue(ic);
								}
							}
						}
						else if (propDict.ContainsKey(fieldName))
						{
							var prop = propDict[fieldName];
							if (prop.PropertyType == typeof(string))
							{
								prop.SetValue(entry, rdr.IsDBNull(ic) ? null : rdr.GetValue(ic).ToString(), null);
							}
							else
							{
								prop.SetValue(entry, rdr.IsDBNull(ic) ? null : rdr.GetValue(ic), null);
							}
						}
					}

					entryes.Add(entry);
				}

				rdr.Close();
				return entryes;
			}
			catch (Exception ex)
			{
				Mess = ex.Message;
				throw;
			}
			finally
			{
				//Добавить логирование.
			}
		}

		public void Execute(string sql, DbConnection connection, DbTransaction transaction = null, object param = null)
		{
			var dtBeg = DateTime.Now;
			var Mess = "";
			try
			{
				DbCommand command = connection.CreateCommand();
				command.Transaction = transaction;
				command.CommandTimeout = 3000;
				command.CommandText = sql;

				if (param != null)
				{
					PropertyInfo[] PropsParam = param.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
					foreach (var prop in PropsParam)
					{
						if (prop.PropertyType == typeof(string))
						{
							var text = (string)prop.GetValue(param, null) ?? "";
							if (text.Length > 255)
							{
								OdbcParameter pBigText = new OdbcParameter("@" + prop.Name, OdbcType.Text);
								pBigText.Value = text;
								command.Parameters.Add(pBigText);
							}
							else
							{
								OdbcParameter p = new OdbcParameter("@" + prop.Name, text);
								command.Parameters.Add(p);
							}
						}
						else
						{
							OdbcParameter p = new OdbcParameter("@" + prop.Name, prop.GetValue(param, null));
							command.Parameters.Add(p);
						}
					}
					var CommandParams = new List<OdbcParameter>();
					command.CommandText = Regex.Replace(command.CommandText, "(@\\w*)", match =>
					{
						var parameter = command.Parameters.OfType<OdbcParameter>().FirstOrDefault(a => a.ParameterName.ToLower() == match.Groups[1].Value.ToLower());
						if (parameter != null)
						{
							var parameterIndex = CommandParams.Count;

							var newParameter = (OdbcParameter)command.CreateParameter();
							newParameter.OdbcType = parameter.OdbcType;
							newParameter.ParameterName = "@parameter" + parameterIndex.ToString();
							newParameter.Value = parameter.Value;

							CommandParams.Add(newParameter);
						}
						return "?";
					});
					command.Parameters.Clear();
					command.Parameters.AddRange(CommandParams.ToArray());
				}
				command.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Mess = ex.Message;
				throw;
			}
			finally
			{
			}
		}

		/// <summary>
		/// Проверяет существования настройки в базе данных.
		/// </summary>
		/// <param name="key">Код параметра</param>
		/// <returns></returns>
		public bool ExistsSetting(string key)
		{
			using (var con = OpenConnection())
			{
				return ExistsSetting(con, null, key);
			}
		}

		private bool ExistsSetting(DbConnection con, DbTransaction trans, string key)
		{
			var sql = "SELECT COUNT(*) FROM GlobalParams WHERE Param = @Code";
			return Query<int>(sql, con, trans, new { Code = key }).First() > 0;
		}

		/// <summary>
		/// Получает значение настройки.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string GetSettingValue(string key)
		{
			using (var con = OpenConnection())
			{
				var sql = "SELECT Value FROM GlobalParams WHERE Param = @Code";
				return Query<string>(sql, con, null, new { Code = key }).First();
			}
		}

		private void InsertParam(DbConnection con, DbTransaction trans, string code, string value)
		{
			string sql = "INSERT INTO GlobalParams ([Param], [Value]) VALUES(@Code, @Value)";
			Execute(sql, con, trans, new { Code = code, Value = value });
		}

		private void UpdateParam(DbConnection con, DbTransaction trans, string code, string value)
		{
			string sql = "UPDATE GlobalParams SET [Value] = @Value WHERE [Param] = @Code";
			Execute(sql, con, trans, new { Code = code, Value = value });
		}

		/// <summary>
		/// Выполняет пакет запросов
		/// </summary>
		/// <param name="listSql">Список запросов</param>
		/// <param name="version">Версия структуры БД</param>
		public void ExecuteSqlQueries(List<string> listSql, DateTime version, string paramKey)
		{
			using (var con = OpenConnection())
			{
				var trans = con.BeginTransaction();
				foreach (var sql in listSql)
				{
					Execute(sql, con, trans);
				}

				bool hasDbVersion = ExistsSetting(con, trans, paramKey);

				string ver = version.ToString("dd.MM.yyyy HH:mm:ss");

				if (hasDbVersion)
				{
					UpdateParam(con, trans, paramKey, ver);
				}
				else
				{
					InsertParam(con, trans, paramKey, ver);
				}

				trans.Commit();
			}
		}

		/// <summary>
		/// Создает бэкап базы данных.
		/// </summary>
		public void CreateBackup()
		{
			string dir = Path.GetDirectoryName(_pathToDb);
			string fileName = $"[DbUpdate]{Path.GetFileNameWithoutExtension(_pathToDb)} {DateTime.Now.ToString("yyyy-MM-dd HHmmss")}.mdb";
			File.Copy(_pathToDb, 
				Path.Combine(dir, fileName));
		}
	}
}
