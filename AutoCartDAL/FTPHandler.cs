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
             string method = WebRequestMethods.Ftp.DownloadFile)
        {
            var ftAddress = ftpAddress + fileName;
            //FtpWebRequest request = (FtpWebRequest)WebRequest.Create(config.FTPAddress);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftAddress);
            // This example assumes the FTP site uses anonymous logon.  
            request.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
            // Copy the contents of the file to the request stream.  
            request.UsePassive = true;
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
                            if (name.Contains(".txt"))
                            {
                                string lastWord = name.Split(' ').Last();
                                files.Add(lastWord);
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
                        Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);
                    break;
                case WebRequestMethods.Ftp.DeleteFile:
                    request.Method = WebRequestMethods.Ftp.DeleteFile;
                    using (response = (FtpWebResponse)request.GetResponse())
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                    }
                    break;
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
    }
}
