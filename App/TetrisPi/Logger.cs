namespace TetrisApp
{
    public class Logger
    {
        private static readonly string logFilePath;
        private static StreamWriter writer;

        static Logger()
        {
            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

            if (File.Exists(logFilePath))
                File.Delete(logFilePath);

            writer = new StreamWriter(logFilePath, true) { AutoFlush = true };
        }

        public static void Log(string message) => writer.WriteLine(message);

        public static void Close() => writer.Close();
    }

    public static class LoggerExtension
    {
        public static string Normize(this object obj)
        {
            return obj.ToString().Replace("\0", "");
        }
    }

}
