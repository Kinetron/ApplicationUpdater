using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Файлы, которые необходимо удалить.
	/// </summary>
	public class XmlDeletedFiles
	{
		[XmlElement("DeletedFile")]
		public List<XmlDeletedFile> DeletedFiles { get; set; } = new List<XmlDeletedFile>();
	}
}
