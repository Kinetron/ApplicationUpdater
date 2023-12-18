using ApplicationUpdater.Contracts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DbAdaptor;
using ApplicationUpdater.Models;
using System.IO;

namespace ApplicationUpdater.Services
{
	/// <summary>
	/// Выполняет скрипты обновления БД.
	/// </summary>
	internal class DbScriptExecutor : IDbScriptExecutor
	{
		/// <summary>
		/// Разделитель строк в запросе.
		/// </summary>
		private const string ScriptLineDelimeter = "Go;";
		private readonly Action<string, Color> _printText;
		MsAccessAdaptor _dbAdaptor = new MsAccessAdaptor("db\\ClientData.mdb");

		public DbScriptExecutor(Action<string, Color> printText)
		{
			_printText = printText;
		}

		public bool BeginUpdate(List<DbScriptDto> scripts)
		{
			//SetCurrentProgress(0);
			_printText("Проверка версии структуры базы данных", Color.Black);

			//Возможно БД не полностью развернута или чистая.
			if (!_dbAdaptor.ExistsSetting("LastDbUpdateDate"))
			{
				_printText("Не удалось получить версию структуры базы данных.", Color.Red);
				return false;
			}

			DateTime lastDbUpdateDate = DateTime.ParseExact(_dbAdaptor.GetSettingValue("LastDbUpdateDate"), "dd.MM.yyyy HH:mm:ss", null);
			var actualScripts = scripts.Where(x => x.DbVersion > lastDbUpdateDate)
				.OrderBy(x => x.DbVersion).ToList();

			if (!actualScripts.Any())
			{
				_printText("База данных в актуальном состоянии.", Color.Green);
				return true;
			}

			_printText("Создание резервной копии БД...", Color.Black);
			_dbAdaptor.CreateBackup();

			foreach (var script in actualScripts)
			{
				//setCurrentProgress((scriptsCount * ic++) / 100);
				_printText($"Обновление структуры базы до версии от {script.Version}", Color.Green);
				List<string> listSql = GetSqlFromFile("DbUpdateScripts\\" + script.Name);

				_dbAdaptor.ExecuteSqlQueries(listSql, script.DbVersion, "LastDbUpdateDate");
				_printText($"Обновление структуры базы до версии от {script.Version} завершено успешно", Color.Green);
			}

			_printText($" Обновления БД установлены", Color.Green);

			return true;
		}


		/// <summary>
		/// Получает список запросов из файла
		/// </summary>
		public List<string> GetSqlFromFile(string path)
		{
			var data = File.ReadAllText(path, Encoding.UTF8);
			var queries = data.Split(new string[] { ScriptLineDelimeter }, StringSplitOptions.RemoveEmptyEntries);
			return new List<string>(queries);
		}
	}
}
