using System;
using Eto;
using Eto.Forms;

namespace SeudoBuild.Client.Desktop.XamMac2
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Platforms.XamMac2).Run(new MainForm());
        }
    }
}
