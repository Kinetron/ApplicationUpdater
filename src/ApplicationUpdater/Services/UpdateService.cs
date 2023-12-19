using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ApplicationUpdater.Contracts;
using ApplicationUpdater.Models;
using ApplicationUpdater.Models.Xml;
using ApplicationUpdater.Services;
using Updater.Enums;
using Updater.Models;

namespace ApplicationUpdater
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
		private const string pathRoot = "";
		private const string backUpFileDirPath = "backUpFiles";
		private const string programConfig = "Config.ini";

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
		bool flagUpdate = false; // флаг прерывания\завершения обновления удалить

		/// <summary>
		/// Возвращает пользователю сообщение об ошибке.
		/// </summary>
		private Action<string> ShowError;
		private Action<string> ShowInfo;
		private Action<string, Color> _printText;
		private Action<int> _setProgress;

		/// <summary>
		/// Информация для пользователя, после установки обновления.
		/// </summary>
		private Action<string> _printUpdateInfo;

		public string LastError { get; private set; }

		public UpdateService(Action<string, Color> printText, Action<int> setProgress)
		{
			_printText = printText;
			_setProgress = setProgress;
			flagUpdate = true;
		}

		/// <summary>
		/// Читает настройки.
		/// </summary>
		public void ReadSettings(string filePath)
		{
			using (var stream = System.IO.File.OpenRead(filePath))
			{
				var serializer = new XmlSerializer(typeof(Settings));
				_settings =  serializer.Deserialize(stream) as Settings;
			}
			
			//Задан прокси сервер.
			if (!string.IsNullOrEmpty(_settings.ProxyServer.Server))
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

				_setProgress(0);
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
					//ShowError(e.Message);
					//flagUpdate = false;
				}
			}
			else
			{
				//ShowError($"Файл  {from} не найден..."); //Color.Red
				//flagUpdate = false;
			}
		}

		void OnCopyFileProgressChanged(double persentage, ref bool cancel)
		{
			 _setProgress((int)persentage);
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
			Settings settings = new Settings();
			settings.UpdateServers.MainServer = "dskhdhk";
			settings.UpdateServers.SecondServer = "389";
			settings.ConnectionString = "123";
			settings.LastDbUpdateDate = "1";
			settings.ProxyPassword = "2";
			settings.ProxyUserName = "2";

			using (var writer = new System.IO.StreamWriter("123"))
			{
				var serializer = new XmlSerializer(typeof(Settings));
				serializer.Serialize(writer, settings);
				writer.Flush();
			}

			//.Sleep(100);
			//if (!fileCompleted) return;

			string updateDate = zipDate.ToString("dd.MM.yyyy HH:mm");
			int baseCount = 0;

			//flagUpdate = true;


			listFileBackUp.Clear();
			try
			{
				_printText("Распаковка...\r\n", Color.Green);

				//Распаковка архива
				//_zipExtractor.ExtractFromArchive(zipPath, zipFile, tempDirPath);
				//Десиарилизирую объект
				XmlSerializer serializer = new XmlSerializer(typeof(XmlUpdateInfo));

				XmlUpdateInfo info = null;
				using (StreamReader reader = new StreamReader(Path.Combine(tempDirPath, "update\\config.xml")))
				{
					info = (XmlUpdateInfo)serializer.Deserialize(reader);
				}

				//Обновление базы данных

				//Создание директорий
				CreateFoldersInUserProgram(info.Folders.Folders);

				//Копирование файлов
				CopyUpdateFilesInUserProgram(info.Files.Files);

				//Удаление файлов
				DeleteFilesInUserProgram(info.DeletedFiles.DeletedFiles);

				//Удаление папок
				DeleteFoldersInUserProgram(info.DeletedFolders.DeletedFolders);

				//Установка обновлений базы данных.
				IDbScriptExecutor dbScriptExecutor = new DbScriptExecutor(_printText);
				var dbScripts = info.DbScripts.Scripts.Select(x=>new DbScriptDto()
				{
					Name = x.Name,
					Version = x.Version,
				}).ToList();

				dbScriptExecutor.BeginUpdate(dbScripts);

				//Обновление файла конфигурации
				_printText("Обновление файла конфигурации...\r\n", Color.Green);

				FileBackUp conf_backup = new FileBackUp()
				{
					Path = Path.Combine(pathRoot, programConfig),
					PathBackUp = Path.Combine(backUpFileDirPath, programConfig),
					IsFile = true
				};

				conf_backup.AddToBackUp();
				listFileBackUp.Add(conf_backup);

				//добавление секций
				//	elemList = doc.GetElementsByTagName("AddConfigSection");
				//	//удаление секций
				//	elemList = doc.GetElementsByTagName("DeleteConfigSection");
				//	//добавление ключей
				//	elemList = doc.GetElementsByTagName("AddConfigKey");
				//	//удаление ключей
				//	elemList = doc.GetElementsByTagName("DeleteConfigKey");
				//	//редактирование значений ключей
				//	elemList = doc.GetElementsByTagName("AddConfigValue");

				//Заставляем пользователя читать что изменилось в программе.
				//elemList = doc.GetElementsByTagName("Message");
				//if (elemList.Count > 0 && flagUpdate)
				//{
				//	PrintUpdateInfoForUser(elemList);
				//}

				if (flagUpdate)
				{
					//Обновление файла конфигурации
					// if (baseCount > 0)
					//"DateDB", dbDate);
					//"MessageDate", msgDate);
					//"UpdateDate", updateDate);
					_printText($"\r\nОбновление {updateDate} успешно установлено...\r\n", Color.Green);
				}

				//Удаление временных папок и файлов
				Directory.Delete(Path.Combine(tempDirPath, "update"), true);
				if (Directory.Exists(backUpFileDirPath))
				{
					Directory.Delete(backUpFileDirPath, true);
				}

				File.Delete(zipFile);
			}
			catch (Exception e)
			{
				_printText($"{e.Message}...\r\n", Color.Red);
				flagUpdate = false;

				//if (transaction != null)
				//{
				//	_printText($"Восстановление базы данных...\r\n", Color.Green);
				//	transaction.Rollback();
				//	transaction = null;
				//}

				foreach (FileBackUp item in listFileBackUp)
				{
					string[] mess = item.RestoreFromBackUp();
					if (mess[0].Length > 0)
					{
						_printText($"{mess[0]}\r\n", Color.Red);
					}
					else
					{
						_printText($"{mess[1]}\r\n", Color.Green);
					}
				}

				_printText($"\r\nОбновление {updateDate}  не установлено...\r\n", Color.Red);
			}

			//if (con != null)
			//	con.Close();
			//break;
		}

		/// <summary>
		/// Создает каталоги в программе, заданные в настройках  обновления.
		/// </summary>
		private void CreateFoldersInUserProgram(List<XmlFolder> folders)
		{
			foreach (var folder in folders)
			{
				string path = folder.Path == "root" ? pathRoot:
					   Path.Combine(pathRoot, folder.Path);
				
				string dir = Path.Combine(path, folder.Name);

				if (!Directory.Exists(dir))
				{
					_printText("Создание директории " + dir + "...\r\n", Color.Green);

					FileBackUp fb = new FileBackUp()
					{
						Path = dir, 
						PathBackUp = Path.Combine(backUpFileDirPath, dir),
						IsFile = false
					};
					fb.AddToBackUp();
					listFileBackUp.Add(fb);

					Directory.CreateDirectory(dir);
				}

				if (!flagUpdate) break;
			}
		}

		/// <summary>
		/// Копирует скачанные файлы обновлений.
		/// </summary>
		/// <param name="elemList"></param>
		private void CopyUpdateFilesInUserProgram(List<XmlFile> files)
		{
			foreach (var file in files)
			{
				string src = Path.Combine(tempDirPath, "update\\" + file.From);
				string dst = Path.Combine(pathRoot, file.To);
				string name = Path.Combine(pathRoot, file.Name);
				
				if (!File.Exists(dst) || File.Exists(dst) && GetCheckSum(src) != GetCheckSum(dst))
				{
					string action = "Копирование файла";
					if (File.Exists(dst)) action = "Замена файла";
					_printText($"{action} {name} ...\r\n", Color.Green); 

					FileBackUp fb = new FileBackUp()
					{
						Path = dst,
						PathBackUp = Path.Combine(backUpFileDirPath, name),
						IsFile = true
					};

					fb.AddToBackUp();
					listFileBackUp.Add(fb);

					CopyFile(src, dst);
				}

				if (!flagUpdate) break;
			}
		}

		/// <summary>
		/// Удаляет не актуальные файлы.
		/// </summary>
		/// <param name="elemList"></param>
		private void DeleteFilesInUserProgram(List<XmlDeletedFile> files)
		{
			foreach (var file in files)
			{
				string delFileName = Path.Combine(pathRoot, file.Name);
				string delFilePath = Path.Combine(pathRoot, file.Path);
				if (File.Exists(delFilePath))
				{
					FileBackUp fb = new FileBackUp()
					{
						Path = delFilePath,
						PathBackUp = Path.Combine(backUpFileDirPath, delFileName),
						IsFile = true
					};
					fb.AddToBackUp();
					listFileBackUp.Add(fb);

					_printText($"Удаление файла  {delFilePath} ...\r\n", Color.Green);

					File.Delete(delFilePath);
				}
			}
		}

		/// <summary>
		/// Удаляет не актуальные папки.
		/// </summary>
		/// <param name="elemList"></param>
		private void DeleteFoldersInUserProgram(List<XmlDeletedFolder> folders)
		{
			foreach (var folder in folders)
			{
				string delDir = Path.Combine(pathRoot, folder.Path);

				FileBackUp fb = new FileBackUp()
				{
					Path = delDir,
					PathBackUp = Path.Combine(backUpFileDirPath, delDir),
					IsFile = false
				};

				fb.AddToBackUp();
				listFileBackUp.Add(fb);

				_printText($"Удаление папки  {delDir} ...\r\n", Color.Green);

				Directory.Delete(delDir, true);

				if (!flagUpdate) break;
			}
		}

		/// <summary>
		/// Информация для пользователя, после установки обновления.
		/// </summary>
		private void PrintUpdateInfoForUser(XmlNodeList elemList)
		{
			foreach (XmlElement element in elemList)
			{
				if (DateTime.Parse(element.GetAttribute("Date")) > 
				    DateTime.Parse(_settings.MessageDate))
				{
					string messPath = Path.Combine(tempDirPath, "update\\Messages\\" + element.GetAttribute("Name"));
					_settings.MessageDate = element.GetAttribute("Date");

					string html = File.ReadAllText(messPath, Encoding.GetEncoding("windows-1251"));
					_printUpdateInfo(html);
				}
			}
		}

		/// <summary>
		/// Вычисляет контрольную сумму файла.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		string GetCheckSum(string path)
		{
			using (FileStream fs = File.OpenRead(path))
			{
				MD5 md5 = new MD5CryptoServiceProvider();
				byte[] fileData = new byte[fs.Length];
				fs.Read(fileData, 0, (int)fs.Length);
				byte[] checkSum = md5.ComputeHash(fileData);
				string result = BitConverter.ToString(checkSum).Replace("-", String.Empty);
				return result;
			}
		}
	}
}
