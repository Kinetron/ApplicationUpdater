using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DesktopAppUpdater;
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
		private const string zipPath = "7zip\\7z.exe";

		ServerTypes _activeServer = ServerTypes.Prymary;

		private Settings _settings;

		List<UpdateItem> listUpdates = new List<UpdateItem>();
		List<FileBackUp> listFileBackUp = new List<FileBackUp>();

		WebProxy _proxy = new WebProxy();
		ZipExtractor _zipExtractor = new ZipExtractor();

		/// <summary>
		/// Флаг использования прокси сервера.
		/// </summary>
		bool _useProxy = false;

		//флаг окончания загрузки\копирования файла
		bool fileCompleted = false;

		/// <summary>
		/// Возвращает пользователю сообщение об ошибке.
		/// </summary>
		private Action<string> ShowError;
		private Action<string> ShowInfo;
		private Action<string, Color> _printText;

		private Action<int> setProgress;
		
		public string LastError { get; private set; }

		/// <summary>
		/// Читает настройки.
		/// </summary>
		public void ReadSettings()
		{
			//if UpdateMode == 1) activeServer = ServerTypes.Local;
			_settings = new Settings();
			if (!string.IsNullOrEmpty(_settings.ProxyServer))
			{
				AddProxy();
				_useProxy = true;
			}
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
		/// Создает и настривает прокси сервер.
		/// </summary>
		private void AddProxy()
		{
			WebProxy proxy = new WebProxy();
			string proxiUrl = $"http://{_settings.ProxyServer}:{_settings.ProxyPort}";
			proxy.Address = new Uri(proxiUrl);
			proxy.BypassProxyOnLocal = false;
			proxy.Credentials = new NetworkCredential(_settings.ProxyUserName, _settings.ProxyPassword);
		}

		/// <summary>
		/// Получает информацию с сервера о текущих обновлениях.
		/// </summary>
		public void GetUpdateInfo(string serverUrl, string fileName)
		{
			WebClient wcl = new WebClient();

			wcl.DownloadFileCompleted += new AsyncCompletedEventHandler(OnCompletedDownloadFile);
			wcl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnDownloadProgressChanged);

			if (_useProxy)
			{
				wcl.Proxy = _proxy;
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
					WebClient wcl = new WebClient();
					wcl.DownloadFileCompleted += new AsyncCompletedEventHandler(OnCompletedDownload);
					wcl.DownloadProgressChanged += new DownloadProgressChangedEventHandler(OnDownloadProgressChanged);
					if (_useProxy)
					{
						wcl.Proxy = _proxy;
					}

					wcl.DownloadFileAsync(new Uri(server + "/" + item.Path), Path.Combine(tempDirPath, item.Path));
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

		/// <summary>
		/// Обработка события загрузки файла.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCompletedDownload(object sender, AsyncCompletedEventArgs e)
		{
			if (!e.Cancelled)
			{
				if (e.Error != null)
				{
					//this.BeginInvoke((MethodInvoker)(() => setTextes(e.Error.Message + "...\r\n", Color.Red)));
					//flagUpdate = false;
				}
				else
				{
					//fileCompleted = true;
				}
			}
		}

		/// <summary>
		/// Устанавливает обновления.
		/// </summary>
		/// <param name="zipDate"></param>
		/// <param name="zipFile"></param>
		public void InstallUpdate(DateTime zipDate, string zipFile)
		{
			while (true)
			{
				Thread.Sleep(100);
				if(!fileCompleted) return;

					OdbcTransaction transaction = null;
					OdbcConnection con = null;
					string dbDate = "";
					//dbDate = config.Read("Settings", "DateDB");
					string msgDate = "";
					//msgDate = config.Read("Settings", "MessageDate");
					string updateDate = zipDate.ToString("dd.MM.yyyy HH:mm");
					int baseCount = 0;

				//flagUpdate = true;


					listFileBackUp.Clear();
					//try
					//{
						_printText("Распаковка...\r\n", Color.Green);

						//Распаковка архива
						_zipExtractor.ExtractFromArchive(zipPath, zipFile, tempDirPath);

						XmlDocument doc = new XmlDocument();
						doc.Load(Path.Combine(tempDirPath, "update\\config.xml"));

					//	//Обновление базы данных
					//	XmlNodeList elemList = doc.GetElementsByTagName("Base");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		string fileContent = string.Empty;
					//		baseCount = elemList.Count;
					//		DataBaseMdbUpdater dbUpdater = new DataBaseMdbUpdater();
					//		con = dbUpdater.GetOpenedConnection(config.Read("Settings", "DBPath"));

					//		foreach (XmlElement element in elemList)
					//		{
					//			string dbpath = Path.Combine(tempDirPath, "update\\BaseUpdate\\" + element.GetAttribute("Version"));
					//			if (DateTime.Parse(element.GetAttribute("Date")) > DateTime.Parse(config.Read("Settings", "DateDB")))
					//			{
					//				dbDate = element.GetAttribute("Date");
					//				fileContent += File.ReadAllText(dbpath, Encoding.GetEncoding("windows-1251"));
					//			}
					//		}

					//		if (fileContent.Length > 0)
					//		{
					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Обновление базы...\r\n", Color.Green)));
					//			string[] sqlqueries = fileContent.Split(new[] { config.Read("Settings", "Delimiter") }, StringSplitOptions.RemoveEmptyEntries);
					//			//Проверка на последний пустой запрос и удаление если есть
					//			int countsql = (fileContent.Length - fileContent.Replace(config.Read("Settings", "Delimiter"), "").Length) / config.Read("Settings", "Delimiter").Length;
					//			while (sqlqueries.Length > countsql)
					//			{
					//				sqlqueries = (from x in sqlqueries where x != sqlqueries[sqlqueries.Length - 1] select x).ToArray();
					//			}

					//			transaction = con.BeginTransaction();

					//			var Mess = dbUpdater.ExecuteSqlCommands(sqlqueries, con, transaction);
					//			if (Mess.Length > 0)
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes(Mess, Color.Red)));
					//				flagUpdate = false;
					//			}
					//		}
					//	}

					//	//Создание директорий
					//	elemList = doc.GetElementsByTagName("Folder");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			string dir = Path.Combine(
					//				element.GetAttribute("Path") == "root" ? pathRoot : Path.Combine(pathRoot, element.GetAttribute("Path")),
					//				element.GetAttribute("Name")
					//				);
					//			if (!Directory.Exists(dir))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Создание директории " + dir + "...\r\n", Color.Green)));

					//				FileBackUp fb = new FileBackUp() { Path = dir, PathBackUp = Path.Combine(backUpFileDirPath, dir), IsFile = false };
					//				fb.AddToBackUp();
					//				listFileBackUp.Add(fb);

					//				Directory.CreateDirectory(dir);
					//			}

					//			if (!flagUpdate)
					//				break;
					//		}
					//	}

					//	//Копирование файлов
					//	elemList = doc.GetElementsByTagName("File");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			string from = Path.Combine(tempDirPath, "update\\" + element.GetAttribute("From"));
					//			string to = Path.Combine(pathRoot, element.GetAttribute("To"));
					//			string name = Path.Combine(pathRoot, element.GetAttribute("Name"));

					//			if (!File.Exists(to) || File.Exists(to) && GetChecksum(from) != GetChecksum(to))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes((!File.Exists(to) ? "Копирование файла " : "Замена файла ") + name + "...\r\n", Color.Green)));

					//				FileBackUp fb = new FileBackUp() { Path = to, PathBackUp = Path.Combine(backUpFileDirPath, name), IsFile = true };
					//				fb.AddToBackUp();
					//				listFileBackUp.Add(fb);

					//				CopyFile(from, to);
					//			}
					//			if (!flagUpdate)
					//				break;
					//		}
					//	}

					//	//Удаление файлов
					//	elemList = doc.GetElementsByTagName("DeletedFile");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			string delFileName = Path.Combine(pathRoot, element.GetAttribute("Name"));
					//			string delFilePath = Path.Combine(pathRoot, element.GetAttribute("Path"));
					//			if (File.Exists(delFilePath))
					//			{
					//				FileBackUp fb = new FileBackUp() { Path = delFilePath, PathBackUp = Path.Combine(backUpFileDirPath, delFileName), IsFile = true };
					//				fb.AddToBackUp();
					//				listFileBackUp.Add(fb);

					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Удаление файла " + delFilePath + "...\r\n", Color.Green)));

					//				File.Delete(delFilePath);
					//			}
					//		}
					//	}

					//	//Удаление папок
					//	elemList = doc.GetElementsByTagName("DeleteFolder");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			string delDir = Path.Combine(pathRoot, element.GetAttribute("Path"));

					//			FileBackUp fb = new FileBackUp() { Path = delDir, PathBackUp = Path.Combine(backUpFileDirPath, delDir), IsFile = false };
					//			fb.AddToBackUp();
					//			listFileBackUp.Add(fb);

					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Удаление папки " + delDir + "...\r\n", Color.Green)));

					//			Directory.Delete(delDir, true);

					//			if (!flagUpdate)
					//				break;
					//		}
					//	}

					//	if (transaction != null)
					//		transaction.Commit();
					//	//=============================
					//	//обновление файла конфигурации
					//	//=============================
					//	this.BeginInvoke((MethodInvoker)(() => setTextes("Обновление файла конфигурации...\r\n", Color.Green)));
					//	FileBackUp conf_backup = new FileBackUp() { Path = Path.Combine(pathRoot, programConfig), PathBackUp = Path.Combine(backUpFileDirPath, programConfig), IsFile = true };
					//	conf_backup.AddToBackUp();
					//	listFileBackUp.Add(conf_backup);
					//	//добавление секций
					//	elemList = doc.GetElementsByTagName("AddConfigSection");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Добавление секции " + element.GetAttribute("Name") + "...\r\n", Color.Green)));
					//			if (!config.AddSection(element.GetAttribute("Name")))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Секция " + element.GetAttribute("Name") + " не добавлена...\r\n", Color.Red)));
					//				flagUpdate = false;
					//				break;
					//			}
					//		}
					//	}
					//	//удаление секций
					//	elemList = doc.GetElementsByTagName("DeleteConfigSection");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Удаление секции " + element.GetAttribute("Name") + "...\r\n", Color.Green)));
					//			if (!config.DelSection(element.GetAttribute("Name")))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Секция " + element.GetAttribute("Name") + " не удалена...\r\n", Color.Red)));
					//				flagUpdate = false;
					//				break;
					//			}
					//		}
					//	}
					//	//добавление ключей
					//	elemList = doc.GetElementsByTagName("AddConfigKey");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Добавление ключа " + element.GetAttribute("Name") + "...\r\n", Color.Green)));
					//			if (!config.AddKey(element.GetAttribute("Section"), element.GetAttribute("Name")))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Ключ " + element.GetAttribute("Name") + " не добавлен...\r\n", Color.Red)));
					//				flagUpdate = false;
					//				break;
					//			}
					//		}
					//	}
					//	//удаление ключей
					//	elemList = doc.GetElementsByTagName("DeleteConfigKey");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Удаление ключа " + element.GetAttribute("Name") + "...\r\n", Color.Green)));
					//			if (!config.DelKey(element.GetAttribute("Section"), element.GetAttribute("Name")))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Ключ " + element.GetAttribute("Name") + " не удален...\r\n", Color.Red)));
					//				flagUpdate = false;
					//				break;
					//			}
					//		}
					//	}
					//	//редактирование значений ключей
					//	elemList = doc.GetElementsByTagName("AddConfigValue");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			this.BeginInvoke((MethodInvoker)(() => setTextes("Изменение значения ключа " + element.GetAttribute("Name") + "...\r\n", Color.Green)));
					//			if (!config.AddValue(element.GetAttribute("Section"), element.GetAttribute("Name"), element.GetAttribute("Value")))
					//			{
					//				this.BeginInvoke((MethodInvoker)(() => setTextes("Значение " + element.GetAttribute("Value") + " ключу " + element.GetAttribute("Name") +
					//					" не присвоено...\r\n", Color.Red)));
					//				flagUpdate = false;
					//				break;
					//			}
					//		}
					//	}

					//	elemList = doc.GetElementsByTagName("Message");
					//	if (elemList.Count > 0 && flagUpdate)
					//	{
					//		foreach (XmlElement element in elemList)
					//		{
					//			if (DateTime.Parse(element.GetAttribute("Date")) > DateTime.Parse(config.Read("Settings", "MessageDate")))
					//			{
					//				string messPath = Path.Combine(tempDirPath, "update\\Messages\\" + element.GetAttribute("Name"));
					//				msgDate = element.GetAttribute("Date");

					//				string html = File.ReadAllText(messPath, Encoding.GetEncoding("windows-1251"));

					//				var th = new Thread(() => { ShowMess(html); });
					//				th.SetApartmentState(ApartmentState.STA);
					//				th.Start();
					//				th.Join();
					//			}
					//		}
					//	}

					//	if (flagUpdate)
					//	{
					//		//Обновление файла конфигурации
					//		// if (baseCount > 0)
					//		config.Write("Settings", "DateDB", dbDate);
					//		config.Write("Settings", "MessageDate", msgDate);
					//		config.Write("Settings", "UpdateDate", updateDate);

					//		this.BeginInvoke((MethodInvoker)(() => setTextes("\r\nОбновление " + updateDate + " успешно установлено...\r\n", Color.Green)));
					//	}

					//	//Удаление временных папок и файлов
					//	Directory.Delete(Path.Combine(tempDirPath, "update"), true);
					//	if (Directory.Exists(backUpFileDirPath))
					//		Directory.Delete(backUpFileDirPath, true);
					//	File.Delete(zipFile);
					//}
					//catch (Exception e)
					//{
					//	this.BeginInvoke((MethodInvoker)(() => setTextes(e.Message + "...\r\n", Color.Red)));
					//	flagUpdate = false;

					//	if (transaction != null)
					//	{
					//		this.BeginInvoke((MethodInvoker)(() => setTextes("Восстановление базы данных...\r\n", Color.Green)));
					//		transaction.Rollback();
					//		transaction = null;
					//	}

					//	foreach (FileBackUp item in listFileBackUp)
					//	{
					//		string[] mess = item.RestoreFromBackUp();
					//		if (mess[0].Length > 0)
					//			this.BeginInvoke((MethodInvoker)(() => setTextes(mess[0] + " \r\n", Color.Red)));
					//		else
					//			this.BeginInvoke((MethodInvoker)(() => setTextes(mess[1] + "\r\n", Color.Green)));
					//	}

					//	this.BeginInvoke((MethodInvoker)(() => setTextes("\r\nОбновление " + updateDate + " не установлено...\r\n", Color.Red)));
					//}

					//if (con != null)
					//	con.Close();
					//break;
				
			}
		}

	}
}
