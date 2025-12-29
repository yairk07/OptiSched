using System;
using System.Web;
using System.Web.UI;
using System.Web.Script.Serialization;

public partial class danimaster : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "C",
                location = "danimaster.master.cs:Page_Load:entry",
                message = "Master page Page_Load entry - checking encoding",
                data = new {
                    contentType = Response.ContentType,
                    charset = Response.Charset,
                    contentEncoding = Response.ContentEncoding?.EncodingName,
                    headerEncoding = Response.HeaderEncoding?.EncodingName,
                    pagePath = Request.Url.AbsolutePath
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch (Exception ex) {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
        }
        // #endregion agent log

        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        // #region agent log
        try {
            var logData = new {
                sessionId = "debug-session",
                runId = "run1",
                hypothesisId = "C",
                location = "danimaster.master.cs:Page_Load:after_encoding",
                message = "Master page after setting encoding",
                data = new {
                    contentType = Response.ContentType,
                    charset = Response.Charset,
                    contentEncoding = Response.ContentEncoding?.EncodingName,
                    headerEncoding = Response.HeaderEncoding?.EncodingName
                },
                timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds
            };
            var serializer = new JavaScriptSerializer();
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                serializer.Serialize(logData) + "\n");
        } catch (Exception ex) {
            System.IO.File.AppendAllText(@"c:\Users\yairk\source\repos\OptiSched1\.cursor\debug.log", 
                "{\"error\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}\n");
        }
        // #endregion agent log
        
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

    protected void lnkUserName_Click(object sender, EventArgs e)
    {
        Session.Clear();
        Response.Redirect("login.aspx");
    }
}
