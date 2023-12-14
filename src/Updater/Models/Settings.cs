using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Models
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
	}
}
