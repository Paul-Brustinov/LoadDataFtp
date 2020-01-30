using LoadDataFtp.FTP_client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LoadDataFtp.FTP;

namespace LoadDataFtp
{
    public class DirectoryListParser
    {
        private List<FileStr> _myListArray;

        public FileStr[] FullListing
        {
            get
            {
                return _myListArray.ToArray();
            }
        }

        public FileStr[] FileList
        {
            get
            {
                List<FileStr> _fileList = new List<FileStr>();
                foreach (FileStr thisstruct in _myListArray)
                {
                    if (!thisstruct.IsDirectory)
                    {
                        _fileList.Add(thisstruct);
                    }
                }
                return _fileList.ToArray();
            }
        }

        public FileStr[] DirectoryList
        {
            get
            {
                List<FileStr> _dirList = new List<FileStr>();
                foreach (FileStr thisstruct in _myListArray)
                {
                    if (thisstruct.IsDirectory)
                    {
                        _dirList.Add(thisstruct);
                    }
                }
                return _dirList.ToArray();
            }
        }

        public DirectoryListParser(string responseString)
        {
            _myListArray = GetList(responseString);
        }

        private List<FileStr> GetList(string datastring)
        {
            List<FileStr> myListArray = new List<FileStr>();
            string[] dataRecords = datastring.Split('\n');
            //Получаем стиль записей на сервере
            FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (_directoryListStyle != FileListStyle.Unknown && s != "")
                {
                    FileStr f = new FileStr();
                    f.Name = "..";
                    switch (_directoryListStyle)
                    {
                        case FileListStyle.UnixStyle:
                            f = ParseFileStructFromUnixStyleRecord(s);
                            break;
                        case FileListStyle.WindowsStyle:
                            f = ParseFileStructFromWindowsStyleRecord(s);
                            break;
                    }
                    if (f.Name != "" && f.Name != "." && f.Name != "..")
                    {
                        myListArray.Add(f);
                    }
                }
            }
            return myListArray;
        }
        //Парсинг, если фтп сервера работает на Windows
        private FileStr ParseFileStructFromWindowsStyleRecord(string Record)
        {
            //Предположим стиль записи 02-03-04  07:46PM       <DIR>     Append
            FileStr f = new FileStr();
            string processstr = Record.Trim();
            //Получаем дату
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
            //Получаем время
            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            f.CreateTime = dateStr + " " + timeStr;
            //Это папка или нет
            if (processstr.Substring(0, 5) == "<DIR>")
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                processstr = strs[1];
                f.IsDirectory = false;
            }
            //Остальное содержмое строки представляет имя каталога/файла
            f.Name = processstr;
            return f;
        }
        //Получаем на какой ОС работает фтп-сервер - от этого будет зависеть дальнейший парсинг
        public FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {
                //Если соблюдено условие, то используется стиль Unix
                if (s.Length > 10
                    && Regex.IsMatch(s.Substring(0, 10), "(-|d)((-|r)(-|w)(-|x)){3}"))
                {
                    return FileListStyle.UnixStyle;
                }
                //Иначе стиль Windows
                else if (s.Length > 8
                    && Regex.IsMatch(s.Substring(0, 8), "[0-9]{2}-[0-9]{2}-[0-9]{2}"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }
        //Если сервер работает на nix-ах
        private FileStr ParseFileStructFromUnixStyleRecord(string record)
        {
            //Предположим. тчо запись имеет формат dr-xr-xr-x   1 owner    group    0 Nov 25  2002 bussys
            FileStr f = new FileStr();
            if (record[0] == '-' || record[0] == 'd')
            {// правильная запись файла
                string processstr = record.Trim();
                f.Flags = processstr.Substring(0, 9);
                f.IsDirectory = (f.Flags[0] == 'd');
                processstr = (processstr.Substring(11)).Trim();
                //отсекаем часть строки
                _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.Owner = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.CreateTime = getCreateTimeString(record);
                //Индекс начала имени файла
                int fileNameIndex = record.LastIndexOf(" ") + 1;
                //Само имя файла
                f.Name = record.Substring(fileNameIndex).Trim();
            }
            else
            {
                f.Name = "";
            }
            return f;
        }

        private string getCreateTimeString(string record)
        {
            //Получаем время
            string month = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)";
            string space = @"(\040)+";
            string day = "([0-9]|[1-3][0-9])";
            string year = "[1-2][0-9]{3}";
            string time = "[0-9]{1,2}:[0-9]{2}";
            Regex dateTimeRegex = new Regex(month + space + day + space + "(" + year + "|" + time + ")", RegexOptions.IgnoreCase);
            Match match = dateTimeRegex.Match(record);
            return match.Value;
        }

        private string _cutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }
    }

}
