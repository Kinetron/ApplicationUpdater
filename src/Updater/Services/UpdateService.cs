using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Updater.Enums;
using Updater.Models;

namespace Updater.Services
{
	public class UpdateService
	{
		/// <summary>
		/// Папка в которую скачиваются обновления.
		/// </summary>
		private const string tempDirPath = "TempUpdate";
		private const string updateConfig = "DateUpdate.xml";
		private const string localDirPath = "LocalUpdate";

		ServerTypes _activeServer = ServerTypes.Prymary;

		private Settings _settings;

		List<UpdateItem> listUpdates = new List<UpdateItem>();

		/// <summary>
		/// Возвращает пользователю сообщение об ошибке.
		/// </summary>
		private Action<string> ShowError;
		private Action<string> ShowInfo;
		private Action<int> setProgress;


		public string LastError { get; private set; }

		/// <summary>
		/// Читает настройки.
		/// </summary>
		public void ReadSettings()
		{
			//if UpdateMode == 1) activeServer = ServerTypes.Local;
			_settings = new Settings();
		}

		/// <summary>
		/// Очистка временной папки.
		/// </summary>
		/// <returns></returns>
		public bool ClearTempDir()
		{
			if (Directory.Exists(tempDirPath))
				try
				{
					Directory.Delete(tempDirPath, true);
				}
				catch (Exception ex)
				{
					LastError = ex.Message;
					return false;
				}

			if (!Directory.Exists(tempDirPath))
			{
				Directory.CreateDirectory(tempDirPath);
			}

			return true;
		}

		/// <summary>
		/// Получает информацию с сервера о текущих обновлениях.
		/// </summary>
		public void GetUpdateInfo(string serverUrl, string fileName)
		{
			WebClient wcl = new WebClient();

			wcl.DownloadFileCompleted += new AsyncCompletedEventHandler(OnCompletedDownloadFile);
			wcl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnDownloadProgressChanged);

			if (!string.IsNullOrEmpty(_settings.ProxyServer))
			{
				WebProxy proxy = new WebProxy();
				string proxiUrl = $"http://{_settings.ProxyServer}:{_settings.ProxyPort}";
				proxy.Address = new Uri(proxiUrl);
				proxy.BypassProxyOnLocal = false;
				proxy.Credentials = new NetworkCredential(_settings.ProxyUserName, _settings.ProxyPassword);
				wcl.Proxy = proxy;
			}

			listUpdates.Clear();

			if (File.Exists(Path.Combine(tempDirPath, fileName)))
			{
				File.Delete(Path.Combine(tempDirPath, fileName));
			}

			if (!Directory.Exists(tempDirPath))
			{
				Directory.CreateDirectory(tempDirPath);
			}
			
			if (!string.IsNullOrEmpty(serverUrl))
			{
				wcl.DownloadFileAsync(new Uri($"{serverUrl}/{fileName}"), Path.Combine(tempDirPath, fileName));
			}
			else
			{
				if (_activeServer == ServerTypes.Prymary)
				{
					ShowError("Ошибка! Отсутствует адрес сервера или не найден файл конфигурации");
					_activeServer = ServerTypes.Secondary;
					GetUpdateInfo(_settings.SecondServer, updateConfig);
				}
			}
		}

		//wcl.DownloadFileCompleted += new AsyncCompletedEventHandler(OnCompletedDownloadFile);
		//wcl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnDownloadProgressChanged);

		private void OnCompletedDownloadFile(object sender, AsyncCompletedEventArgs e)
		{
			if (!e.Cancelled)
			{
				if (e.Error != null)
				{
					ShowError(e.Error.Message);

					if (_activeServer == ServerTypes.Prymary)
					{
						_activeServer = ServerTypes.Secondary;
						GetUpdateInfo(_settings.SecondServer, updateConfig);
					}
				}
				else
				{
					ReadUpdatesInfo(Path.Combine(tempDirPath, updateConfig));
				}
			}
		}

		private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			//e.ProgressPercentage;
		}

		/// <summary>
		/// Получает сведения о текущих обновлениях.
		/// </summary>
		private void ReadUpdatesInfo(string path)
		{
			XmlDocument xml = new XmlDocument();
			xml.Load(path);
			XmlElement root = xml.DocumentElement;
			foreach (XmlElement element in root.ChildNodes)
			{
				listUpdates.Add(new UpdateItem
				{
					Date = DateTime.Parse(element.GetAttribute("Date")),
					Path = element.GetAttribute("Path")
				});
			}

			//Актуальными считаются все обновления у которых дата больше заданной в конфиге.
			listUpdates = listUpdates.Where(l => l.Date > 
			                                     DateTime.Parse(_settings.UpdateDate)).ToList();
			if (listUpdates.Count > 0)
			{
				ShowInfo("Загрузка файлов обновления...");
				//flagUpdate = true;
				//DownloadUpdates();
			}
			else
			{
				ShowInfo("Нет доступных обновлений программы...");
				//StartProgramm();
			}
		}

		public void DownloadUpdates()
		{
			string server = "";
			//if (srvActive != ServerTypes.Local)
			//	server = (srvActive == ServerTypes.Prymary ? srvPrimaty : srvSecondary);

			foreach (UpdateItem item in listUpdates)
			{
				//	fileCompleted = false;
				DeleteFile(Path.Combine(tempDirPath, item.Path));

				//	if exeption	flagUpdate = false;

				setProgress(0);
				ShowInfo($"Загрузка {item.Path}...");

				if (_activeServer == ServerTypes.Local)
				{
					CopyFile(Path.Combine(localDirPath, item.Path), Path.Combine(tempDirPath, item.Path));
				}
				else
				{
					//WebClient wcl = new WebClient();
					//wcl.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed_Download);
					//wcl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged_Client);
					//if (proxyFlag)
					//	wcl.Proxy = proxy;
					//wcl.DownloadFileAsync(new Uri(server + "/" + item.Path), Path.Combine(tempDirPath, item.Path));
				}

				//	InstallUpdate(item.Date, Path.Combine(tempDirPath, item.Path));

				//	if (!flagUpdate)
				//		break;
			}
			////запуск программы 
			//if (flagUpdate)
			// StartProgramm());
			//flagUpdate = false;
		}
		public void DeleteFile(string file)
		{
			if (File.Exists(file))
			{
				try
				{
					File.Delete(file);
				}
				catch (Exception e)
				{
					ShowError(e.Message);
				}
			}
		}

		public void CopyFile(string from, string to)
		{
			if (File.Exists(from))
			{
				try
				{
					if (File.Exists(to))
					{
						File.Delete(to);
					}
						
					CustomFileCopier copyer = new CustomFileCopier(from, to);
					copyer.OnComplete += OnCompleteCopyFile;
					copyer.OnProgressChanged += OnCopyFileProgressChanged;
					copyer.Copy();
				}
				catch (Exception e)
				{
					ShowError(e.Message);
					//flagUpdate = false;
				}
			}
			else
			{
				ShowError($"Файл  {from} не найден..."); //Color.Red
				//flagUpdate = false;
			}
		}

		void OnCopyFileProgressChanged(double Persentage, ref bool Cancel)
		{
			 setProgress((int)Persentage);
		}

		void OnCompleteCopyFile()
		{
			//fileCompleted = true;
		}

	}
}
