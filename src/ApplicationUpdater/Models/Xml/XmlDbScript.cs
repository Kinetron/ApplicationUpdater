using System;
using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Скрипт для базы данных.
	/// </summary>
	[Serializable()]
	public class XmlDbScript
	{
		[XmlAttribute("Version")]
		public string Version { get; set; }

		[XmlAttribute("Name")]
		public string Name { get; set; }
	}
}
