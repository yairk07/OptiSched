using System;
using System.Web.UI;

public partial class createDatabaseTables : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            messageDiv.InnerHtml = "<div class='info'>Click the button below to create all missing database tables.</div>";
        }
    }

    protected void btnCreateTables_Click(object sender, EventArgs e)
    {
        try
        {
            DatabaseSchemaUpdater.CreateMissingTables();
            messageDiv.InnerHtml = "<div class='success'><strong>Success!</strong> All missing tables have been created successfully.</div>";
        }
        catch (Exception ex)
        {
            messageDiv.InnerHtml = string.Format(
                "<div class='error'><strong>Error:</strong> {0}<br/><br/>Stack Trace:<br/>{1}</div>",
                ex.Message,
                ex.StackTrace.Replace("\n", "<br/>")
            );
        }
    }
}

