using SmartDocs.Views;

namespace SmartDocs;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages « poussées » au-dessus des onglets
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("documentdetail", typeof(DocumentDetailPage));
        Routing.RegisterRoute("versions", typeof(VersionsPage));
        Routing.RegisterRoute("users", typeof(UsersPage));
        Routing.RegisterRoute("trash", typeof(TrashPage));
        Routing.RegisterRoute("activity", typeof(ActivityPage));
        Routing.RegisterRoute("invitations", typeof(InvitationsPage));
    }
}
