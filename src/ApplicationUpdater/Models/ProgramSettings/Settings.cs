using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplicationUpdater.Models.ProgramSettings;

namespace ApplicationUpdater.Models
{
	public class Settings
	{
		public UpdateServers UpdateServers { get; set; } = new UpdateServers();

		public Proxy ProxyServer { get; set; } = new Proxy();
		public string MainServer { get; set; }
		public string SecondServer { get; set; }
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

		/// <summary>
		/// Обновление программы из локальной папки.
		/// </summary>
		public bool UpdateFromLocalFolder { get; set; }
	}
}
