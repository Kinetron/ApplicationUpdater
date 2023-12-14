using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Services
{
	public class UpdateService
	{
		/// <summary>
		/// Папка в которую скачиваются обновления.
		/// </summary>
		private const string tempDirPath = "TempUpdate";

		public string LastError { get; private set; }

		/// <summary>
		/// Читает настройки.
		/// </summary>
		public void ReadSettings()
		{

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
		public void GetUpdateInfo()
		{

		}
	}
}
