using System.Collections.Generic;
using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Каталоги которые необходимо создавать.
	/// </summary>
	public class XmlFolders
	{
		[XmlElement("Folder")]
		public List<XmlFolder> Folders { get; set; } = new List<XmlFolder>();
	}
}
