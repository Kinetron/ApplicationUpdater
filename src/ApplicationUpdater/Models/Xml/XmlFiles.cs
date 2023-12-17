using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Информация о файлах в обновлении.
	/// </summary>
	public class XmlFiles
	{
		/// <summary>
		/// Договора
		/// </summary>
		[XmlElement("File")]
		public List<XmlFile> Files { get; set; } = new List<XmlFile>();
	}
}
