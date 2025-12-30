using System;
using System.Web;
using System.Web.UI;

public partial class danimaster : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        string pageName = System.IO.Path.GetFileNameWithoutExtension(Request.Url.AbsolutePath);
        Body.Attributes["class"] = "page-" + pageName.ToLower();

        bool isLoggedIn = Session["username"] != null;
        string role = Convert.ToString(Session["Role"]);
        bool isOwner = isLoggedIn && string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);

        SetHebrewText();

        if (linkRegister != null)
            linkRegister.Visible = !isLoggedIn;

        if (linkLogin != null)
            linkLogin.Visible = !isLoggedIn;

        if (linkTasks != null)
            linkTasks.Visible = isLoggedIn;

        if (linkSharedCalendars != null)
            linkSharedCalendars.Visible = isLoggedIn;

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

    private void SetHebrewText()
    {
        if (linkHome != null) linkHome.InnerText = "\u05d1\u05d9\u05ea";
        if (linkRegister != null) linkRegister.InnerText = "\u05d4\u05e8\u05e9\u05de\u05d4";
        if (linkTasks != null) linkTasks.InnerText = "\u05d8\u05d5\u05e4\u05e1 \u05de\u05e9\u05d9\u05de\u05d5\u05ea";
        if (linkSharedCalendars != null) linkSharedCalendars.InnerText = "\u05d8\u05d1\u05dc\u05d0\u05d5\u05ea \u05de\u05e9\u05d5\u05ea\u05e4\u05d5\u05ea";
        if (linkContact != null) linkContact.InnerText = "\u05e6\u05d5\u05e8 \u05e7\u05e9\u05e8";
        if (linkContent != null) linkContent.InnerText = "\u05ea\u05d5\u05db\u05df";
        if (linkLogin != null) linkLogin.InnerText = "\u05db\u05e0\u05d9\u05e1\u05d4";
        if (linkTerms != null) linkTerms.InnerText = "\u05ea\u05e0\u05d0\u05d9 \u05e9\u05d9\u05e8\u05d5\u05ea";
        if (linkUsers != null) linkUsers.InnerText = "\u05de\u05e9\u05ea\u05de\u05e9\u05d9\u05dd";
        if (linkAllEvents != null) linkAllEvents.InnerText = "\u05db\u05dc \u05d4\u05d0\u05d9\u05e8\u05d5\u05e2\u05d9\u05dd";
        if (linkEditEvent != null) linkEditEvent.InnerText = "\u05e2\u05e8\u05d9\u05db\u05ea \u05d0\u05d9\u05e8\u05d5\u05e2";
        if (lnkUserName != null) lnkUserName.Text = "\u05d4\u05ea\u05e0\u05ea\u05e7";
    }

    protected void lnkUserName_Click(object sender, EventArgs e)
    {
        Session.Clear();
        Response.Redirect("login.aspx");
    }
}
