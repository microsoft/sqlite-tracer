// -----------------------------------------------------------------------
// <copyright file="Conductor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SQLiteLogViewer
{
    using Microsoft.Win32;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using ViewModels;

    public class Conductor : IConductor
    {
        private QueryWindow queryWindow;
        private FilterWindow filterWindow;

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

        public void OpenFilterWindow()
        {
            if (this.filterWindow == null)
            {
                var mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;
                this.filterWindow = new FilterWindow { DataContext = mainViewModel.LogViewModel };

                this.filterWindow.Closed += (sender, e) => this.filterWindow = null;
                this.filterWindow.Show();
            }
            else
            {
                this.filterWindow.Focus();
            }
        }

        public string OpenSaveFileDialog()
        {
            var dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        public string OpenOpenFileDialog()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Quit confirmation")]
        public bool? ConfirmSave()
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them?",
                "SQLite Log Viewer",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            switch (result)
            {
            case MessageBoxResult.Yes:
                return true;
            case MessageBoxResult.No:
                return false;

            case MessageBoxResult.Cancel:
            default:
                return null;
            }
        }
    }
}
