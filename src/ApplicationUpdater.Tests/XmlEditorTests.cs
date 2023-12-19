using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplicationUpdater.Services;

namespace ApplicationUpdater.Tests
{
	[TestClass]
	public class XmlEditorTests
	{
		/// <summary>
		/// Добавление узла в xml файл.
		/// </summary>
		[TestMethod]
		public void AddNode()
		{
			XmlEditor editor = new XmlEditor();
			editor.AddNode("Config.xml");
		}
	}
}
