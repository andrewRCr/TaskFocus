using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskFocusDesktop.Utilities;

namespace TaskFocusDesktop.EventModels
{
    public class ViewSwitchedEvent
    {
        public ViewCatalog.ContentPanel SwitchedContentPanel { get; set; }

        public ViewCatalog.MainContentView NewMainContentView { get; set; }

        public ViewCatalog.SidePanelView NewSidePanelView { get; set; }

        public ViewSwitchedEvent(ViewCatalog.ContentPanel switchedContentPanel, ViewCatalog.MainContentView newMainContentView)
        {
            SwitchedContentPanel = switchedContentPanel;
            NewMainContentView = newMainContentView;
        }

        public ViewSwitchedEvent(ViewCatalog.ContentPanel switchedContentPanel, ViewCatalog.SidePanelView newSidePanelView)
        {
            SwitchedContentPanel = switchedContentPanel;
            NewSidePanelView = newSidePanelView;
        }
    }
}
