namespace ApplicationUpdater.Models.ProgramSettings
{
	/// <summary>
	/// Настройки прокси сервера.
	/// </summary>
	public class Proxy
	{
		public string Server { get; set; } = string.Empty;
		public int Port { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}
}
