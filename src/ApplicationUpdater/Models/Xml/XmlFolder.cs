using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	[Serializable()]
	public class XmlFolder
	{
		/// <summary>
		/// Путь.
		/// </summary>
		[XmlAttribute("Path")]
		public string Path { get; set; }

		/// <summary>
		/// Имя создаваемой папки.
		/// </summary>

		[XmlAttribute("Name")]
		public string Name { get; set; }
	}
}
