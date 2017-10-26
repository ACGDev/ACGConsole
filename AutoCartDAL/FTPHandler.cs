using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoCarOperations.Model;

namespace AutoCarOperations
{
    public class FTPHandler
    {
        public static void DownloadOrUploadOrDeleteFile(string ftpAddress, string ftpUserName, string ftpPassword, string filePath, string fileName,
              string method = WebRequestMethods.Ftp.DownloadFile, int? noOfDays = null)
        {
            var ftAddress = ftpAddress + fileName;
            //FtpWebRequest request = (FtpWebRequest)WebRequest.Create(config.FTPAddress);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftAddress);
            // This example assumes the FTP site uses anonymous logon.  
            request.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
            // Copy the contents of the file to the request stream.  
            request.UsePassive = true;
            List<string> dateFormatList = null;
            if (noOfDays.HasValue)
            {
                dateFormatList = new List<string>();
                for (int i = 0; i <= noOfDays; i++)
                {
                    var fetchDate = DateTime.Now.AddDays(-i);
                    var dateFormat1 = fetchDate.ToString("M-d-yyyy");  // date does not have leading 0
                    var dateFormat2 = fetchDate.ToString("yyyy-MM-dd");
                    dateFormatList.Add(dateFormat1);
                    dateFormatList.Add(dateFormat2);
                }
            }
            FtpWebResponse response;
            switch (method)
            {
                case WebRequestMethods.Ftp.ListDirectory:
                    request.Method = WebRequestMethods.Ftp.ListDirectory;
                    List<string> files = new List<string>();
                    using (response = (FtpWebResponse)request.GetResponse())
                    using (Stream listStream = response.GetResponseStream())
                    using (StreamReader listReader = new StreamReader(listStream))
                    {
                        while (!listReader.EndOfStream)
                        {
                            var name = listReader.ReadLine();
                            if (name.ToLower().Contains(".txt") || name.ToLower().Contains(".csv"))
                            {
                                //string lastWord = name.Split(' ').Last();
	
                                if (dateFormatList != null && !dateFormatList.Any(I => name.Contains(I)))
                                {
                                     continue;
                                }
                                files.Add(name);
                            }
                        }
                    }
                    foreach (var file in files)
                    {
                        DownloadOrUploadOrDeleteFile(ftpAddress, ftpUserName, ftpPassword, filePath, file);
                    }
                    break;
                case WebRequestMethods.Ftp.UploadFile:
                    request.Method = WebRequestMethods.Ftp.UploadFile;
                    StreamReader sourceStream = new StreamReader(filePath + "\\" + fileName);
                    byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
                    sourceStream.Close();
                    request.ContentLength = fileContents.Length;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(fileContents, 0, fileContents.Length);
                    requestStream.Close();
                    using (response = (FtpWebResponse)request.GetResponse())
                        Console.WriteLine("  FTP file upload status {0}", response.StatusDescription);
                    break;
                case WebRequestMethods.Ftp.DeleteFile:
                    try
                    {
                        request.Method = WebRequestMethods.Ftp.DeleteFile;
                        using (response = (FtpWebResponse)request.GetResponse())
                        using (Stream responseStream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        // TODO: need reason for failure
                        Console.WriteLine("  FTP method {0}, Error: {1} File: {2}", method, ex.Message, fileName);
                        break;
                    }
                    
                default:
                    request.Method = WebRequestMethods.Ftp.DownloadFile;
                    using (response = (FtpWebResponse)request.GetResponse())
                    using (Stream responseStream = response.GetResponseStream())
                    using (Stream fileStream = File.Create(filePath + "/"+  fileName))
                    {
                        responseStream.CopyTo(fileStream);
                    }
                    //using (StreamReader reader = new StreamReader(responseStream))
                    //{
                    //    downloadedFileContents.Add(reader.ReadToEnd());
                    //}
                    break;
            }
        }


        public static void DownloadLastTwoDaysFiles(string ftpAddress, string user, string password, string filePath, string folder)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpAddress +"/"+ folder);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            request.Credentials = new NetworkCredential(user, password);

            List<string> tmpFileList = new List<string>();
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                while (!reader.EndOfStream)
                {
                    tmpFileList.Add(reader.ReadLine());
                }
            }

            Uri ftp = new Uri(ftpAddress);
            foreach (var f in tmpFileList)
            {
                FtpWebRequest req = (FtpWebRequest)WebRequest.Create(new Uri(ftp, f));
                req.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                req.Credentials = new NetworkCredential(user, password);

                using (FtpWebResponse resp = (FtpWebResponse)req.GetResponse())
                {
                    if (resp.LastModified.Date == DateTime.Today.Date || resp.LastModified.Date == DateTime.Today.AddDays(1).Date)
                    {
                        DownloadOrUploadOrDeleteFile(ftpAddress, user, password, filePath, f, WebRequestMethods.Ftp.DownloadFile);
                    }
                }
            }
        }

    }
}
