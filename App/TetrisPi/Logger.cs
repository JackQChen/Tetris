using System.Collections.Concurrent;

namespace TetrisApp
{
    public class Logger
    {
        private static StreamWriter writer;

        private static BlockingCollection<string> queue = new BlockingCollection<string>();

        static Logger()
        {
            Open();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var log = queue.Take();
                    writer.WriteLine(log);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public static void Open()
        {
            var logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
            Open(logFilePath);
        }

        public static void Open(string logFilePath)
        {
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);

            writer = new StreamWriter(logFilePath, true) { AutoFlush = true };
        }

        public static void Log(string message) => queue.Add(message);

        public static void Close() => writer.Close();
    }

}
