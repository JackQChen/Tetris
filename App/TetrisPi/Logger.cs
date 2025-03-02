namespace TetrisApp
{
    public class Logger
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        private static StreamWriter writer;

        static Logger()
        {
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);

            writer = new StreamWriter(logFilePath, true) { AutoFlush = true };
        }

        public static void Log(string message)
        {
            message = message.Trim('\0');
            if (string.IsNullOrWhiteSpace(message))
                return;
            writer.WriteLine(message);
        }

        public static void Close() => writer.Close();
    }

}
