using System;
using System.Data;
using System.Linq;
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
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        // Handle request access from query string
        string requestAccessId = Request.QueryString["requestAccess"];
        if (!string.IsNullOrEmpty(requestAccessId))
        {
            try
            {
                int calendarId = Convert.ToInt32(requestAccessId);
                string userIdStr = Session["userId"]?.ToString();
                int userId = 0;
                
                if (string.IsNullOrEmpty(userIdStr))
                {
                    // Try to get userId from username
                    string username = Session["username"]?.ToString();
                    if (!string.IsNullOrEmpty(username))
                    {
                        UsersService us = new UsersService();
                        DataRow user = us.GetUserByEmail(username);
                        if (user == null)
                        {
                            // Try to find by username
                            var allUsers = us.getallusers();
                            if (allUsers != null && allUsers.Tables.Count > 0)
                            {
                                var userRow = allUsers.Tables[0].AsEnumerable()
                                    .FirstOrDefault(r => 
                                        (r["UserName"]?.ToString() ?? "").Equals(username, StringComparison.OrdinalIgnoreCase) ||
                                        (r["userName"]?.ToString() ?? "").Equals(username, StringComparison.OrdinalIgnoreCase));
                                if (userRow != null)
                                {
                                    string idCol = userRow.Table.Columns.Contains("Id") ? "Id" : "id";
                                    userIdStr = userRow[idCol]?.ToString();
                                    if (!string.IsNullOrEmpty(userIdStr))
                                    {
                                        userId = Convert.ToInt32(userIdStr);
                                        Session["userId"] = userIdStr;
                                    }
                                }
                            }
                        }
                        else
                        {
                            string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";
                            userIdStr = user[idCol]?.ToString();
                            if (!string.IsNullOrEmpty(userIdStr))
                            {
                                userId = Convert.ToInt32(userIdStr);
                                Session["userId"] = userIdStr;
                            }
                        }
                    }
                }
                else
                {
                    userId = Convert.ToInt32(userIdStr);
                }
                
                if (userId <= 0)
                {
                    lblMessage.Text = "שגיאה: לא ניתן לזהות את המשתמש. אנא התחבר מחדש.";
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    BindCalendars();
                    return;
                }
                
                bool success = service.RequestAccess(calendarId, userId);
                if (success)
                {
                    // Store success message in session to show after redirect
                    Session["RequestAccessMessage"] = "בקשת הגישה נשלחה בהצלחה!";
                    Session["RequestAccessMessageType"] = "success";
                }
                else
                {
                    Session["RequestAccessMessage"] = "שגיאה בשליחת בקשת הגישה. אנא נסה שוב.";
                    Session["RequestAccessMessageType"] = "error";
                }
            }
            catch (Exception ex)
            {
                Session["RequestAccessMessage"] = "שגיאה בשליחת בקשת הגישה: " + ex.Message;
                Session["RequestAccessMessageType"] = "error";
            }
            
            // Remove query string and reload page
            Response.Redirect("sharedCalendars.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }
        
        // Check for message from previous request
        if (Session["RequestAccessMessage"] != null)
        {
            lblMessage.Text = Session["RequestAccessMessage"].ToString();
            string messageType = Session["RequestAccessMessageType"]?.ToString() ?? "error";
            if (messageType == "success")
            {
                lblMessage.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
            Session.Remove("RequestAccessMessage");
            Session.Remove("RequestAccessMessageType");
        }

        string eventTarget = Request["__EVENTTARGET"];
        string eventArgument = Request["__EVENTARGUMENT"];

        if (!string.IsNullOrEmpty(eventTarget) && eventTarget == "RequestAccess" && !string.IsNullOrEmpty(eventArgument))
        {
            int calendarId = Convert.ToInt32(eventArgument);
            int userId = Convert.ToInt32(Session["userId"]);
            bool success = service.RequestAccess(calendarId, userId);
            if (success)
            {
                lblMessage.Text = "בקשת הגישה נשלחה בהצלחה!";
                lblMessage.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                lblMessage.Text = "שגיאה בשליחת בקשת הגישה. אנא נסה שוב.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
            BindCalendars();
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

    protected void RequestAccess_Click(object sender, System.Web.UI.WebControls.CommandEventArgs e)
    {
        try
        {
            int calendarId = Convert.ToInt32(e.CommandArgument);
            int userId = Convert.ToInt32(Session["userId"]);

            bool success = service.RequestAccess(calendarId, userId);

            if (success)
            {
                lblMessage.Text = "בקשת הגישה נשלחה בהצלחה!";
                lblMessage.ForeColor = System.Drawing.Color.Green;
                BindCalendars();
            }
            else
            {
                lblMessage.Text = "שגיאה בשליחת בקשת הגישה. אנא נסה שוב.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblMessage.Text = $"שגיאה בשליחת בקשת הגישה: {ex.Message}";
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void dlCalendars_ItemDataBound(object sender, System.Web.UI.WebControls.DataListItemEventArgs e)
    {
    }

    protected string GetCalendarActionButton(object dataItem)
    {
        try
        {
            if (dataItem == null) return "";
            
            System.Data.DataRowView row = (System.Data.DataRowView)dataItem;
            int calendarId = Convert.ToInt32(row["CalendarId"]);
            
            int isMember = 0;
            int isAdmin = 0;
            string requestStatus = "";
            
            try { isMember = Convert.ToInt32(row["IsMember"] ?? 0); } catch { }
            try { isAdmin = Convert.ToInt32(row["IsAdmin"] ?? 0); } catch { }
            try { requestStatus = row["RequestStatus"]?.ToString() ?? ""; } catch { }

            // If user is admin or member, show view button
            if (isMember == 1 || isAdmin == 1)
            {
                return string.Format("<a href='sharedCalendarDetails.aspx?id={0}' class='btn-view'>צפה בטבלה</a>", calendarId);
            }
            // If user has a pending request, show status only (latest request)
            else if (!string.IsNullOrEmpty(requestStatus) && requestStatus == "Pending")
            {
                return string.Format("<span class='btn-requested'>בקשה ממתינה לאישור</span>");
            }
            // If request was approved or rejected, show status (latest request only)
            else if (!string.IsNullOrEmpty(requestStatus))
            {
                string statusText = "";
                if (requestStatus == "Approved")
                    statusText = "בקשה אושרה";
                else if (requestStatus == "Rejected")
                    statusText = "בקשה נדחתה";
                else
                    statusText = requestStatus;
                
                // Show only the latest status - user can still send new requests but we only show the latest one
                return string.Format("<span class='btn-requested'>{0}</span>", statusText);
            }
            // Otherwise, show request access button
            else
            {
                return string.Format("<button type='button' onclick='requestAccess({0})' class='btn-request'>בקש גישה</button>", calendarId);
            }
        }
        catch (Exception ex)
        {
            // In case of error, show request button as fallback
            try
            {
                System.Data.DataRowView row = (System.Data.DataRowView)dataItem;
                int calendarId = Convert.ToInt32(row["CalendarId"]);
                return string.Format("<button type='button' onclick='requestAccess({0})' class='btn-request'>בקש גישה</button>", calendarId);
            }
            catch
            {
                return "";
            }
        }
    }
}
