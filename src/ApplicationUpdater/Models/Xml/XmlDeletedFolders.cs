using System.Collections.Generic;
using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Удаляемые каталоги.
	/// </summary>
	public class XmlDeletedFolders
	{
		[XmlElement("DeletedFolder")]
		public List<XmlDeletedFolder> DeletedFolders { get; set; } = new List<XmlDeletedFolder>();
	}
}
