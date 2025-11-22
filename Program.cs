using System.Windows;

namespace TarkovBuddy
{
    /// <summary>
    /// Program entry point.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new App();
            app.Run();
        }
    }
}