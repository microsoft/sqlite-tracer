// -----------------------------------------------------------------------
// <copyright file="Conductor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using ViewModels;

    public class Conductor : IConductor
    {
        private QueryWindow queryWindow;

        public void OpenQueryWindow()
        {
            if (this.queryWindow == null)
            {
                this.queryWindow = new QueryWindow()
                {
                    DataContext = Application.Current.MainWindow.DataContext,
                };
                this.queryWindow.Closed += (sender, e) => this.queryWindow = null;
                this.queryWindow.Show();
            }
            else
            {
                this.queryWindow.Focus();
            }
        }
    }
}
