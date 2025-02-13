using TaskFocusDesktop.Utilities;

namespace TaskFocusDesktop.EventModels
{
    public class RequestViewSwitchEvent
    {   
        public ViewCatalog.ContentPanel RequestedContentPanel { get; set; }

        public ViewCatalog.MainContentView RequestedMainContentView { get; set; }

        public ViewCatalog.SidePanelView RequestedSidePanelView { get; set; }

        public RequestViewSwitchEvent(ViewCatalog.ContentPanel requestedContentPanel, ViewCatalog.MainContentView requestedMainContentView)
        {
            RequestedContentPanel = requestedContentPanel;
            RequestedMainContentView = requestedMainContentView;
        }

        public RequestViewSwitchEvent(ViewCatalog.ContentPanel requestedContentPanel, ViewCatalog.SidePanelView requestedSidePanelView)
        {
            RequestedContentPanel = requestedContentPanel;
            RequestedSidePanelView = requestedSidePanelView;
        }
    }
}
