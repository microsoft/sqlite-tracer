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
                var mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;
                this.queryWindow = new QueryWindow { DataContext = mainViewModel.LogViewModel };

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
