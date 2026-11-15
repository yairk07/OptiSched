using System;
using System.Web.UI;

public partial class accessibility : System.Web.UI.Page
{
    // עמוד הצהרת נגישות — תוכן סטטי בעברית
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
    }
}
