namespace Orvina.UI
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var model = new Model();
            var view = new View(new Form1());
            var controller = new Controller(view, model);
            Application.Run(view.MainForm);
        }
    }
}