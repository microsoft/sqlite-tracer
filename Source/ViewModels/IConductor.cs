namespace SQLiteLogViewer.ViewModels
{
    public interface IConductor
    {
        void OpenQueryWindow();

        string OpenSaveFileDialog();

        string OpenOpenFileDialog();

        bool? ConfirmSave();
    }
}