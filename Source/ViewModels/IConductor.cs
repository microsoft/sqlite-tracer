namespace SQLiteLogViewer.ViewModels
{
    public interface IConductor
    {
        void OpenQueryWindow();

        void OpenFilterWindow();

        string OpenSaveFileDialog();

        string OpenOpenFileDialog();

        bool? ConfirmSave();
    }
}