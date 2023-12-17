using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Файл подлежащий удалению.
	/// </summary>
	[Serializable()]
	public class XmlDeletedFile
	{
		/// <summary>
		/// Путь.
		/// </summary>
		[XmlAttribute("Path")]
		public string Path { get; set; }

		/// <summary>
		/// Имя удаляемого файла.
		/// </summary>

		[XmlAttribute("Name")]
		public string Name { get; set; }
	}
}
