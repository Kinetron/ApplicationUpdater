using System;
using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Файл конфигурации устанавливаемого обновления. Действия, которые должен выполнить обновлятор.
	/// </summary>
	[Serializable()]
	[System.Xml.Serialization.XmlRoot("UpdateProgram")]
	public class XmlUpdateInfo
	{
		[XmlElement("DbScripts")]
		public XmlDbScripts DbScripts { get; set; }

		[XmlElement("Folders")]
		public XmlFolders Folders { get; set; }

		[XmlElement("Files")]
		public XmlFiles Files { get; set; }

		[XmlElement("DeletedFiles")]
		public XmlDeletedFiles DeletedFiles { get; set; }

		[XmlElement("DeletedFolders")]
		public XmlDeletedFolders DeletedFolders { get; set; }
	}
}
