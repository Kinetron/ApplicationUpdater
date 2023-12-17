using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	public class XmlDeletedFolder
	{
		/// <summary>
		/// Путь.
		/// </summary>
		[XmlAttribute("Path")]
		public string Path { get; set; }
	}
}
