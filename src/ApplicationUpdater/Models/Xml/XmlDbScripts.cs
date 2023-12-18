using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Скрипты для базы данных.
	/// </summary>
	public class XmlDbScripts
	{
		[XmlElement("DbScript")]
		public List<XmlDbScript> Scripts { get; set; } = new List<XmlDbScript>();
	}
}
