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
            BindPendingRequests();
        }
    }

    private string FixEncoding(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        try
        {
            if (IsValidUtf8(text))
            {
                return text;
            }
            
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
            System.Diagnostics.Debug.WriteLine(string.Format("BindCalendars: Loading calendars for userId: {0}", userId));
            
            DataTable dt = service.GetAllSharedCalendars(userId);
            
            System.Diagnostics.Debug.WriteLine(string.Format("BindCalendars: Loaded {0} calendars", dt.Rows.Count));
            
            if (dt.Rows.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine("BindCalendars: Columns:");
                foreach (System.Data.DataColumn col in dt.Columns)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("  - {0} ({1})", col.ColumnName, col.DataType.Name));
                }
            }
            
            foreach (DataRow row in dt.Rows)
            {
                if (dt.Columns.Contains("CalendarName") && row["CalendarName"] != DBNull.Value)
                {
                    string original = row["CalendarName"].ToString();
                    row["CalendarName"] = FixEncoding(original);
                    System.Diagnostics.Debug.WriteLine(string.Format("BindCalendars: Fixed CalendarName: '{0}' -> '{1}'", original, row["CalendarName"]));
                }
                if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value)
                {
                    string original = row["Description"].ToString();
                    row["Description"] = FixEncoding(original);
                }
                if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value)
                {
                    string original = row["CreatorName"].ToString();
                    row["CreatorName"] = FixEncoding(original);
                }
            }
            
            if (dt != null && dt.Rows.Count > 0)
            {
                dlCalendars.DataSource = dt;
                dlCalendars.DataBind();
                dlCalendars.Visible = true;
                lblNoCalendars.Visible = false;
                
                System.Diagnostics.Debug.WriteLine(string.Format("BindCalendars: Successfully bound {0} calendars to DataList", dt.Rows.Count));
            }
            else
            {
                dlCalendars.Visible = false;
                lblNoCalendars.Visible = true;
                System.Diagnostics.Debug.WriteLine("BindCalendars: No calendars found, showing empty message");
            }
            
            System.Diagnostics.Debug.WriteLine(string.Format("BindCalendars: DataList bound with {0} items", dt.Rows.Count));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("BindCalendars: Error: {0}\n{1}", ex.Message, ex.StackTrace));
            lblMessage.Text = "שגיאה בטעינת הטבלאות. אנא נסה שוב.";
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
                lblMessage.Text = "אנא הזן שם לטבלה.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            System.Diagnostics.Debug.WriteLine(string.Format("btnSaveCalendar: Creating calendar - Name: '{0}', Description: '{1}'", name, description));

            int userId = Convert.ToInt32(Session["userId"]);
            int calendarId = service.CreateSharedCalendar(name, description, userId);

            System.Diagnostics.Debug.WriteLine(string.Format("btnSaveCalendar: Created calendar with ID: {0}", calendarId));

            if (calendarId > 0)
            {
                lblMessage.Text = "הטבלה נוצרה בהצלחה!";
                lblMessage.ForeColor = System.Drawing.Color.Green;
                pnlCreateForm.Visible = false;
                btnCreateNew.Visible = true;
                txtCalendarName.Text = "";
                txtDescription.Text = "";
                BindCalendars();
            }
            else
            {
                lblMessage.Text = "שגיאה ביצירת הטבלה. אנא נסה שוב.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("btnSaveCalendar: Error: {0}\n{1}", ex.Message, ex.StackTrace));
            lblMessage.Text = string.Format("שגיאה ביצירת הטבלה: {0}", ex.Message);
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected string GetCalendarActionButton(object dataItem)
    {
        try
        {
            if (dataItem == null)
                return "";

            System.Data.DataRowView rowView = dataItem as System.Data.DataRowView;
            if (rowView == null)
                return "";

            System.Data.DataRow row = rowView.Row;
            int calendarId = Convert.ToInt32(row["CalendarId"]);
            int isAdmin = row["IsAdmin"] != DBNull.Value ? Convert.ToInt32(row["IsAdmin"]) : 0;
            int isMember = row["IsMember"] != DBNull.Value ? Convert.ToInt32(row["IsMember"]) : 0;
            int hasRequestedAccess = row["HasRequestedAccess"] != DBNull.Value ? Convert.ToInt32(row["HasRequestedAccess"]) : 0;
            string requestStatus = row["RequestStatus"] != DBNull.Value ? row["RequestStatus"].ToString() : "";

            if (isAdmin == 1 || isMember == 1)
            {
                return string.Format("<a href='sharedCalendarDetails.aspx?id={0}' class='btn-view'>צפה בטבלה</a>", calendarId);
            }
            else if (hasRequestedAccess == 1 && !string.IsNullOrEmpty(requestStatus))
            {
                if (requestStatus.ToLower() == "pending" || requestStatus.ToLower() == "ממתין")
                {
                    return "<span class='btn-requested'>בוצעה בקשה</span>";
                }
                else if (requestStatus.ToLower() == "approved" || requestStatus.ToLower() == "אושר")
                {
                    return string.Format("<a href='sharedCalendarDetails.aspx?id={0}' class='btn-view'>צפה בטבלה</a>", calendarId);
                }
                else
                {
                    return string.Format("<button type='button' class='btn-request' onclick='requestAccess({0})'>בקש גישה</button>", calendarId);
                }
            }
            else
            {
                return string.Format("<button type='button' class='btn-request' onclick='requestAccess({0})'>בקש גישה</button>", calendarId);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("sharedCalendars", "Error in GetCalendarActionButton", ex);
            return "";
        }
    }

    private void BindPendingRequests()
    {
        try
        {
            int userId = Convert.ToInt32(Session["userId"]);
            DataTable dt = service.GetPendingRequestsForUser(userId);

            if (dt != null && dt.Rows.Count > 0)
            {
                dlPendingRequests.DataSource = dt;
                dlPendingRequests.DataBind();
                pnlPendingRequests.Visible = true;
                lblNoRequests.Visible = false;
            }
            else
            {
                pnlPendingRequests.Visible = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("BindPendingRequests: Error: {0}\n{1}", ex.Message, ex.StackTrace));
            pnlPendingRequests.Visible = false;
        }
    }
}
