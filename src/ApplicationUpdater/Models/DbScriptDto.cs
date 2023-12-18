using System.Xml.Serialization;
using System;

namespace ApplicationUpdater.Models
{
	/// <summary>
	/// Информация о скрипте.
	/// </summary>
	internal class DbScriptDto
	{
		private DateTime _version = DateTime.Now;
		public DateTime DbVersion => _version;

		/// <summary>
		/// Имя файла скрипта.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Версия базы данных, которая будет после установки скрипта.
		/// </summary>
		public string Version
		{
			get { return _version.ToString("dd.MM.yyyy HH:mm:ss"); }
			set { _version = DateTime.ParseExact(value, "dd.MM.yyyy HH:mm:ss", null); }
		}
	}
}
