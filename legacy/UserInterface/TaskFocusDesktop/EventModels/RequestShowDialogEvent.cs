using TaskFocusDesktop.Utilities;

namespace TaskFocusDesktop.EventModels
{
    public class RequestShowDialogEvent
    {
        public ViewCatalog.DialogView RequestedDialogView { get; set; }

        public RequestShowDialogEvent(ViewCatalog.DialogView requestedDialogView)
        {
                RequestedDialogView = requestedDialogView;
        }
    }
}
