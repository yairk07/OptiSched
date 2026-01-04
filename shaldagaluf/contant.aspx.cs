using System;
using System.Data;

public partial class Default3 : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        if (!IsPostBack)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("image");
            dt.Columns.Add("text");
            dt.Columns.Add("url");

            dt.Rows.Add("pics/chat.png",
                        "",
                        "https://chatgpt.com/");

            dt.Rows.Add("pics/scouts.png",
                        "המסע בצופים",
                        "https://prod-hamasa.zofim.org.il/he/login/");

            dt.Rows.Add("pics/math.jpg",
                        "עזרה בלימודים ",
                        "https://tiktek.com/il/heb-index.htm");

            dt.Rows.Add("pics/github-logo-git-hub-icon-with-text-on-white-and-black-background-free-vector.jpg",
                        "GitHub",
                        "https://github.com/yairk07/OptiSched");

            dlCards.DataSource = dt;
            dlCards.DataBind();
        }
    }
}
