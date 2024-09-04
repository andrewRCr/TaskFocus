using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
