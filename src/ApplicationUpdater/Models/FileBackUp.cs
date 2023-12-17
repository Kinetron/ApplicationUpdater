using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Updater.Models
{
    public class FileBackUp
    {
        enum State { DELETED_REPLACED, ADDED }
        public string PathBackUp { get; set; }
        public string Path { get; set; }
        State state { get; set; }
        public bool IsFile { get; set; }

        public string AddToBackUp()
        {
            string Mess = "";
            try
            {
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(this.PathBackUp)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(this.PathBackUp));

                if (File.Exists(this.Path) || Directory.Exists(this.Path))
                    this.state = State.DELETED_REPLACED;
                else
                    this.state = State.ADDED;

                if (this.IsFile)
                {
                    if (File.Exists(this.PathBackUp))
                        File.Delete(this.PathBackUp);
                    File.Copy(this.Path, this.PathBackUp);
                }
                else
                    if (this.state == State.DELETED_REPLACED)
                    {
                        if (Directory.Exists(this.PathBackUp))
                            Directory.Delete(this.PathBackUp);
                        DirectoryCopy(this.Path, this.PathBackUp);
                    }
            }
            catch(Exception e)
            {
                Mess = e.Message;
            }
            return Mess;      
        }

        public string[] RestoreFromBackUp()
        {
            string[] Mess = {"", ""};
            try
            {

                if (this.IsFile && this.state == State.DELETED_REPLACED)
                {
                    if (File.Exists(this.Path))
                        File.Delete(this.Path);
                    File.Copy(this.PathBackUp, this.Path);
                    Mess[1] = "Файл " + this.Path  + " восстановлен...";
                }
                else
                if (this.IsFile && this.state == State.ADDED)
                {
                    File.Delete(this.Path);
                    Mess[1] = "Файл " + this.Path + " удален...";
                }
                else
                if (!this.IsFile && this.state == State.DELETED_REPLACED)
                {
                    if (Directory.Exists(this.Path))
                        Directory.Delete(this.Path, true);
                    DirectoryCopy(this.PathBackUp, this.Path);
                    Mess[1] = "Папка " + this.Path + " восстановлена...";
                }
                else
                if (!this.IsFile && this.state == State.ADDED)
                {
                    Directory.Delete(this.Path, true);
                    Mess[1] = "Папка " + this.Path + " удалена...";
                }
            }
            catch(Exception e)
            {
                Mess[0] = e.Message;
            }
            return Mess;
        }

        void DirectoryCopy(string from, string to)
        {
            DirectoryInfo dir_inf = new DirectoryInfo(from);
            foreach (DirectoryInfo dir in dir_inf.GetDirectories())
            {
                if (Directory.Exists(to + "\\" + dir.Name) != true)
                {
                    Directory.CreateDirectory(to + "\\" + dir.Name);
                }
                DirectoryCopy(dir.FullName, to + "\\" + dir.Name);

            }
            foreach (string file in Directory.GetFiles(from))
            {
                //Определяем (отделяем) имя файла с расширением - без пути (но с слешем "\").
                string filik = file.Substring(file.LastIndexOf('\\'), file.Length - file.LastIndexOf('\\'));
                File.Copy(file, to + "\\" + filik, true);
            }
        }

    }
}
