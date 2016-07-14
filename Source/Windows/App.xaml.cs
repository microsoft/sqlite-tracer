// -----------------------------------------------------------------------
// <copyright file="CLASSNAME.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Usage message")]
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.Port = 3000;
            if (e != null && e.Args.Length > 0)
            {
                int port;
                if (int.TryParse(e.Args[0], out port))
                {
                    this.Port = port;
                }
                else
                {
                    Console.WriteLine("Usage: ");
                    Console.WriteLine("{0} [port]", Environment.GetCommandLineArgs()[0]);
                    Console.WriteLine("port defaults to {0}", this.Port);
                    App.Current.Shutdown(0);
                }
            }
        }

        public int Port { get; set; }
    }
}
