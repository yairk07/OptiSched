using System;
using System.Web.UI;

public partial class migrate_database : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
    }

    protected void btnMigrate_Click(object sender, EventArgs e)
    {
        try
        {
            DatabaseMigration.MigrateToDSD();
            lblResult.Text = "<div style='color: green; padding: 20px; border: 2px solid green;'>" +
                            "<h2>Migration Completed Successfully!</h2>" +
                            "<p>The database has been migrated to DSD-compliant schema.</p>" +
                            "<p><strong>Changes made:</strong></p>" +
                            "<ul>" +
                            "<li>Users table: Renamed columns (password→PasswordHash, userId→UserIdNumber, phonenum→PhoneNumber, city→CityId), added CreatedDate</li>" +
                            "<li>CalendarEvents table: Created from calnder table with new column names (UserId, EventDate, EventTime, CreatedDate)</li>" +
                            "<li>SharedCalendarEvents: Updated to use EventDate and EventTime columns</li>" +
                            "<li>AuthCodes table: Created unified table for OTP and password reset codes</li>" +
                            "<li>All tables: Updated to use INTEGER types where specified in DSD</li>" +
                            "</ul>" +
                            "<p><strong>Next steps:</strong></p>" +
                            "<ul>" +
                            "<li>Test the application thoroughly</li>" +
                            "<li>Verify all features work correctly</li>" +
                            "<li>Delete this migration page after verification</li>" +
                            "</ul>" +
                            "</div>";
        }
        catch (Exception ex)
        {
            lblResult.Text = "<div style='color: red; padding: 20px; border: 2px solid red;'>" +
                            "<h2>Migration Failed!</h2>" +
                            "<p>Error: " + Server.HtmlEncode(ex.Message) + "</p>" +
                            "<p>Stack trace: " + Server.HtmlEncode(ex.StackTrace) + "</p>" +
                            "</div>";
        }
    }
}

