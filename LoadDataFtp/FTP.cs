using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;








namespace LoadDataFtp
{
    public class FTP
    {
        private FtpWebResponse ftpResponse;
        private string host;
        private string path;
        private string userName;
        private string password;
        bool useSSL;

        public FTP(string _host, string _path, string _userName, string _password, bool _useSSL)
        {
            host = _host;
            path = _path;
            if (path == null || path == "")
            {
                path = "/";
            }
            userName = _userName;
            password = _password;
            useSSL = _useSSL;
        }
        
        public FtpWebRequest GetRequest(string uri)
        {
            FtpWebRequest ftpRequest;

            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + uri);
            ftpRequest.EnableSsl = useSSL;
            ftpRequest.Credentials = new NetworkCredential(userName, password);
            return ftpRequest;
        }


        //Реализеум команду LIST для получения подробного списока файлов на FTP-сервере
        public FileStr[] ListDirectory(string path)
        {
            FtpWebRequest ftpRequest = GetRequest(host + path);
            //команда фтп LIST
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            //Получаем входящий поток
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            //переменная для хранения всей полученной информации
            string content = "";

            StreamReader sr = new StreamReader(ftpResponse.GetResponseStream(), Encoding.ASCII);
            content = sr.ReadToEnd();
            sr.Close();
            ftpResponse.Close();

            DirectoryListParser parser = new DirectoryListParser(content);
            return parser.FullListing;
        }

        //метод протокола FTP RETR для загрузки файла с FTP-сервера
        public void DownloadFile(string fileName, string pathTo)
        {
            FtpWebRequest ftpRequest = GetRequest(host + path + fileName);

            //команда фтп RETR
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

            //Файлы будут копироваться в кталог программы
            FileStream downloadedFile = new FileStream(pathTo + fileName, FileMode.Create, FileAccess.ReadWrite);

            try { 
                ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            }
            catch(Exception e)
            {
                throw e;
            }
            //Получаем входящий поток
            Stream responseStream = ftpResponse.GetResponseStream();

            //Буфер для считываемых данных
            byte[] buffer = new byte[1024];
            int size = 0;

            while ((size = responseStream.Read(buffer, 0, 1024)) > 0)
            {
                downloadedFile.Write(buffer, 0, size);

            }
            ftpResponse.Close();
            downloadedFile.Close();
            responseStream.Close();
        }

        public void RemoveFile(string fileName)
        {
            FtpWebRequest ftpRequest = GetRequest(host + path + fileName);
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();
            Console.WriteLine("Delete status: {0}", response.StatusDescription);
            response.Close();
        }



        public struct FileStr
        {
            public string Flags;
            public string Owner;
            public bool IsDirectory;
            public string CreateTime;
            public string Name;
        }

    }
}
