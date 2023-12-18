using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ApplicationUpdater
{
    public class ZipExtractor
    {
        public void ExtractFromArchive(string archiver, string archiveName, string outputFolder)
        {
            try
            {
                // Предварительные проверки
                if (!File.Exists(archiver))
                    throw new Exception("Архиватор 7z по пути \"" + archiver + "\" не найден");
                if (!File.Exists(archiveName))
                    throw new Exception("Файл архива \"" + archiveName + "\" не найден");
                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);
                // Формируем параметры вызова 7z
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = archiver;
                // Распаковать (для полных путей - x) e
                startInfo.Arguments = " x";
                // На все отвечать yes
                startInfo.Arguments += " -y";
                // Файл, который нужно распаковать
                startInfo.Arguments += " " + "\"" + archiveName + "\"";
                // Папка распаковки
                startInfo.Arguments += " -o" + "\"" + outputFolder + "\"";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                int sevenZipExitCode = 0;
                using (Process sevenZip = Process.Start(startInfo))
                {
                    sevenZip.WaitForExit();
                    sevenZipExitCode = sevenZip.ExitCode;
                }
                // Если с первого раза не получилось,
                //пробуем еще раз через 1 секунду
                if (sevenZipExitCode != 0 && sevenZipExitCode != 1)
                {
                    using (Process sevenZip = Process.Start(startInfo))
                    {
                        Thread.Sleep(1000);
                        sevenZip.WaitForExit();
                        switch (sevenZip.ExitCode)
                        {
                            case 0: return; // Без ошибок и предупреждений
                            case 1: return; // Есть некритичные предупреждения
                            case 2: throw new Exception("Фатальная ошибка распаковки");
                            case 7: throw new Exception("Ошибка в командной строке");
                            case 8:
                                throw new Exception("Недостаточно памяти для выполнения операции");
                            case 225:
                                throw new Exception("Пользователь отменил выполнение операции");
                            default: throw new Exception("Архиватор 7z вернул недокументированный код ошибки: " + sevenZip.ExitCode.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("7Zip: " + e.Message);
            }
        }
    }
}
