﻿using System;
using Eto;
using Eto.Forms;

namespace SeudoBuild.Client.Desktop.Mac
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new Application(Platforms.Mac).Run(new MainForm());
        }
    }
}
