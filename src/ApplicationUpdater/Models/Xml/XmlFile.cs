using System.Xml.Serialization;

namespace ApplicationUpdater.Models.Xml
{
	/// <summary>
	/// Файл обновления.
	/// </summary>
	[Serializable()]
	public class XmlFile
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }

		[XmlAttribute("Date")]
		public string Date { get; set; }

		[XmlAttribute("From")]
		public string From { get; set; }

		[XmlAttribute("To")]
		public string To { get; set; }
	}
}