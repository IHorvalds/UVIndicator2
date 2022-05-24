namespace UVIndicator2
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //create a controller and Pass an instance of your application main form
            var controller = new UVIndicator2.ApplicationController(new WeatherForm());

            //Run application
            controller.Run(Environment.GetCommandLineArgs());
        }
    }
}