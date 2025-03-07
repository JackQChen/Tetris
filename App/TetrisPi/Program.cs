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
            var form = new Processor();
            form.Init();
            new ManualResetEvent(false).WaitOne();
        }
    }
}
