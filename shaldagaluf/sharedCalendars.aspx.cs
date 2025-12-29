using System;
using System.Data;
using System.Web.UI;
using System.Text;

public partial class sharedCalendars : System.Web.UI.Page
{
    SharedCalendarService service = new SharedCalendarService();

    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!IsPostBack)
        {
            BindCalendars();
        }
    }

    private string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        try
        {
            if (IsValidUtf8(text))
                return text;
            
            byte[] bytes = Encoding.GetEncoding("Windows-1255").GetBytes(text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return text;
        }
    }
    
    private bool IsValidUtf8(string text)
    {
        try
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
            string decoded = Encoding.UTF8.GetString(utf8Bytes);
            return decoded == text;
        }
        catch
        {
            return false;
        }
    }

    private void BindCalendars()
    {
        try
        {
            int userId = Convert.ToInt32(Session["userId"]);
            DataTable dt = service.GetAllSharedCalendars(userId);
            
            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains("CalendarName") && row["CalendarName"] != DBNull.Value)
                    row["CalendarName"] = FixEncoding(row["CalendarName"].ToString());
                if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value)
                    row["Description"] = FixEncoding(row["Description"].ToString());
                if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value)
                    row["CreatorName"] = FixEncoding(row["CreatorName"].ToString());
            }
            
            if (dt != null && dt.Rows.Count > 0)
            {
                dlCalendars.DataSource = dt;
                dlCalendars.DataBind();
                dlCalendars.Visible = true;
                lblNoCalendars.Visible = false;
            }
            else
            {
                dlCalendars.Visible = false;
                lblNoCalendars.Visible = true;
            }
        }
        catch
        {
            lblMessage.Text = "שגיאה בטעינת הלוחות. אנא נסה.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void btnCreateNew_Click(object sender, EventArgs e)
    {
        pnlCreateForm.Visible = true;
        btnCreateNew.Visible = false;
    }

    protected void btnCancelCreate_Click(object sender, EventArgs e)
    {
        pnlCreateForm.Visible = false;
        btnCreateNew.Visible = true;
        txtCalendarName.Text = "";
        txtDescription.Text = "";
    }

    protected void btnSaveCalendar_Click(object sender, EventArgs e)
    {
        try
        {
            string name = txtCalendarName.Text.Trim();
            string description = txtDescription.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                lblMessage.Text = "אנא הזן שם ללוח.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            int userId = Convert.ToInt32(Session["userId"]);
            int calendarId = service.CreateSharedCalendar(name, description, userId);

            if (calendarId > 0)
            {
                lblMessage.Text = "הלוח נוצר בהצלחה!";
                lblMessage.ForeColor = System.Drawing.Color.Green;
                pnlCreateForm.Visible = false;
                btnCreateNew.Visible = true;
                txtCalendarName.Text = "";
                txtDescription.Text = "";
                BindCalendars();
            }
            else
            {
                lblMessage.Text = "שגיאה ביצירת הלוח. אנא נסה.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblMessage.Text = $"שגיאה ביצירת הלוח: {ex.Message}";
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }
}
