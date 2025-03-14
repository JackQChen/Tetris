using System.Collections.Concurrent;

namespace TetrisApp
{
    public class Logger : IDisposable
    {
        public static Logger Instance { get; set; }
        public static Logger AIInstance { get; set; }

        private StreamWriter writer;
        private CancellationTokenSource token;
        private BlockingCollection<string> queue = new BlockingCollection<string>();

        public Logger(string logFilePath)
        {
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);
            token = new CancellationTokenSource();
            writer = new StreamWriter(logFilePath, true) { AutoFlush = true };
            Task.Factory.StartNew(() =>
            {
                try
                {
                    while (true)
                    {
                        var log = queue.Take(token.Token);
                        writer.WriteLine(log);
                    }
                }
                catch
                {
                }
            }, token.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Log(string message) => queue.Add(message);

        public void Dispose()
        {
            token.Cancel();
            writer.Close();
            writer.Dispose();
        }
    }
}
