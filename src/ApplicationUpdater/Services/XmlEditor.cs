using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ApplicationUpdater.Services
{
	/// <summary>
	/// Позволяет изменять xml файл настроек. Добавлять или удалять новые секции.
	/// </summary>
	public class XmlEditor
	{
		/// <summary>
		/// Добавляет узел в конфигурационный файл.
		/// </summary>
		public void AddNode(string path)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(path);
			XmlNode rootNode = doc.SelectSingleNode(@"Settings");
			
			XmlNode xName = doc.CreateElement("Name1");
			xName.InnerText = "hi";
			rootNode.AppendChild(xName);
			doc.Save(path);
		}
	}
}
