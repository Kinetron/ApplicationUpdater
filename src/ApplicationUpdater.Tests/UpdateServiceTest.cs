using DbAdaptor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationUpdater.Tests
{
	[TestClass]
	public class UpdateServiceTest
	{
		[TestMethod]
		public void ReadSettings()
		{
			UpdateService updateService = new UpdateService((text, color) =>
			{
				Console.WriteLine(text, color);
			}, (progress) => { });

			updateService.ReadSettings("Config.xml");
		}

		[TestMethod]
		public void InstallUpdateTest()
		{
			UpdateService updateService = new UpdateService((text, color) =>
			{
				Console.WriteLine(text, color);
			}, (progress) => { });

			updateService.InstallUpdate(DateTime.Now, ".\\UpdateExample.zip");
		}
	}
}
