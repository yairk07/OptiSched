using System;
using System.Web.UI;

public partial class runSchemaUpdate : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Write("<html><head><meta charset='utf-8'><title>Creating Database Tables</title>");
        Response.Write("<style>body{font-family:Arial;max-width:800px;margin:50px auto;padding:20px;direction:ltr;}");
        Response.Write(".success{color:green;padding:10px;background:#e8f5e9;border:1px solid #4caf50;border-radius:4px;margin:10px 0;}");
        Response.Write(".error{color:red;padding:10px;background:#ffebee;border:1px solid #f44336;border-radius:4px;margin:10px 0;}");
        Response.Write("</style></head><body>");
        Response.Write("<h1>Creating Database Tables</h1>");
        
        try
        {
            DatabaseSchemaUpdater.CreateMissingTables();
            Response.Write("<div class='success'><strong>Success!</strong> All missing tables have been created successfully.</div>");
            Response.Write("<div class='success'>Tables created:<br/>");
            Response.Write("1. Files<br/>");
            Response.Write("2. EventFiles<br/>");
            Response.Write("3. Images<br/>");
            Response.Write("4. EventImages<br/>");
            Response.Write("5. ContactMessages<br/>");
            Response.Write("6. EventTypes<br/>");
            Response.Write("7. PermissionTypes<br/>");
            Response.Write("8. CalendarPermissions<br/>");
            Response.Write("9. CalendarJoinRequests<br/>");
            Response.Write("</div>");
        }
        catch (Exception ex)
        {
            Response.Write(string.Format(
                "<div class='error'><strong>Error:</strong> {0}<br/><br/>Stack Trace:<br/>{1}</div>",
                ex.Message,
                ex.StackTrace.Replace("\n", "<br/>")
            ));
        }
        
        Response.Write("</body></html>");
        Response.End();
    }
}

