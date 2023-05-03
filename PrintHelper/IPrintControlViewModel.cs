namespace PrintHelper
{
    public interface IPrintControlViewModel : IViewModel
    {
        bool CanScale { get; set; }
        void ShowPrintPreview();
    }
}