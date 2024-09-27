
namespace TaskFocusDesktop.Utilities
{
    public class ViewCatalog
    {
        public enum ContentPanel
        {
            MainContent,
            SidePanel,
            TopPanel
        }

        public enum MainContentView
        {
            Home,
            Inbox,
            Today,
            Projects,
            Contexts,
            Completed,
            Settings
        }

        public enum SidePanelView
        {
            NavMenu,
            ProjectSubNavMenu,
            ContextSubNavMenu
        }

        public enum DialogView
        {
            AddNewProjectDialog,
            AddNewContextDialog,
            AddNewTaskDialog,
            RenameProjectDialog,
            DeleteProjectDialog,
            RenameContextDialog,
            DeleteContextDialog,
            UpdateEmailDialog,
            ChangePasswordDialog
        }
    }
}
