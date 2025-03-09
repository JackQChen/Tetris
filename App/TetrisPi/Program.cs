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
            //var processor = new Processor();
            //processor.Init();
            new ManualResetEvent(false).WaitOne();
        }
    }
}
