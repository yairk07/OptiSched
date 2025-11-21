using System;
using System.Web;
using System.Web.UI;

public partial class danimaster : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string pageName = System.IO.Path.GetFileNameWithoutExtension(Request.Url.AbsolutePath);
        Body.Attributes["class"] = "page-" + pageName.ToLower();

        bool isLoggedIn = Session["username"] != null;
        string role = Convert.ToString(Session["Role"]);
        bool isOwner = isLoggedIn && string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);

        if (linkRegister != null)
            linkRegister.Visible = !isLoggedIn;

        if (linkLogin != null)
            linkLogin.Visible = !isLoggedIn;

        if (linkTasks != null)
            linkTasks.Visible = isLoggedIn;

        if (linkContent != null)
            linkContent.Visible = isLoggedIn;

        if (linkTerms != null)
            linkTerms.Visible = true;

        if (linkUsers != null)
            linkUsers.Visible = isOwner;

        if (linkAllEvents != null)
            linkAllEvents.Visible = isOwner;

        if (linkEditEvent != null)
            linkEditEvent.Visible = isOwner;

        if (lnkUserName != null)
            lnkUserName.Visible = isLoggedIn;
    }

    protected void lnkUserName_Click(object sender, EventArgs e)
    {
        Session.Clear();
        Response.Redirect("login.aspx");
    }
}
