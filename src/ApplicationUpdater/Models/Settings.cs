using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationUpdater.Models
{
	internal class Settings
	{
		public string MainServer { get; set; }
		public string SecondServer { get; set; }
		public string ProxyServer { get; set; }
		public string ProxyPort { get; set; }
		public string ProxyUserName { get; set; }
		public string ProxyPassword { get; set; }
		public string UpdateMode { get; set; }

		public string UpdateDate { get; set; }
		public string MessageDate { get; set; }

		/// <summary>
		/// Строка подключения к базе данных.
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// Последняя дата обновления базы данных.
		/// </summary>
		public string LastDbUpdateDate { get; set; }
	}
}
