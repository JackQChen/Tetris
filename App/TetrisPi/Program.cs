namespace TetrisApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var form = new MainForm();
            form.OnLoad(EventArgs.Empty);
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                    break;
            }
        }
    }
}
