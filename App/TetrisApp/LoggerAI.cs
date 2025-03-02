namespace TetrisApp
{
    public class LoggerAI
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logAI.txt");
        private static StreamWriter writer;

        static LoggerAI()
        {
            if (File.Exists(logFilePath))
                File.Delete(logFilePath);

            writer = new StreamWriter(logFilePath, true) { AutoFlush = true };
        }

        public static void Log(string message) => writer.WriteLine(message);

        public static void Close() => writer.Close();
    }

}
