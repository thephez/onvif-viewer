using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace RTSP_Viewer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 form = new Form1();
            form.Size = new System.Drawing.Size(800, 500);
            Application.Run(form);
            //Application.Run(new Form1());
        }
    }
}
