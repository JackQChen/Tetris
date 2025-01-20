using System.Net;

namespace TetrisApp
{
    public class FTPHandler
    {
        private readonly string _ftpServerUrl = "";
        private readonly string _ftpUsername = "";
        private readonly string _ftpPassword = "";

        private readonly string _localFolder = "";

        public FTPHandler()
        {
        }

        private HashSet<string> GetFtpFileListAsync()
        {
            var fileList = new HashSet<string>();
            var request = (FtpWebRequest)WebRequest.Create(_ftpServerUrl);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

            using var response = (FtpWebResponse)request.GetResponse();
            using var streamReader = new StreamReader(response.GetResponseStream());

            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                fileList.Add(line);
            }

            return fileList;
        }

        private void UploadFile(string fileName)
        {
            var ftpRequest = (FtpWebRequest)WebRequest.Create($"{_ftpServerUrl}/{fileName}");
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
            ftpRequest.Credentials = new NetworkCredential(_ftpUsername, _ftpPassword);

            var path = Path.Combine(_localFolder, fileName);
            using var fileStream = File.OpenRead(path);
            using var requestStream = ftpRequest.GetRequestStream();

            fileStream.CopyTo(requestStream);
        }

        public bool SyncFiles()
        {
            try
            {
                var ftpFiles = GetFtpFileListAsync();
                var localFiles = Directory.GetFiles(_localFolder);
                foreach (var localFile in localFiles)
                {
                    var fileName = Path.GetFileName(localFile);
                    if (!ftpFiles.Contains(fileName))
                        UploadFile(fileName);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
