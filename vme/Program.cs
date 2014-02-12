using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace vme
{
    static class Program
    {
        /// <summary>
        /// The Main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());

        }
    }
}
