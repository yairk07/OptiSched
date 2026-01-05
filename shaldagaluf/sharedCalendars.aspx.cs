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
        
        LoggingService.Log("SHARED_CALENDARS_PAGE_LOAD", "Page_Load started");
        
        if (Session["username"] == null)
        {
            LoggingService.Log("SHARED_CALENDARS_NO_SESSION", "User not logged in, redirecting to login");
            Response.Redirect("login.aspx");
            return;
        }
        
        string username = Session["username"]?.ToString() ?? "";
        string userIdStr = Session["userId"]?.ToString() ?? "";
        LoggingService.Log("SHARED_CALENDARS_SESSION", string.Format("User session - Username: {0}, UserId: {1}", username, userIdStr));

        // Handle request access from query string
        string requestAccessId = Request.QueryString["requestAccess"];
        if (!string.IsNullOrEmpty(requestAccessId))
        {
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_QS", string.Format("Request access from query string - CalendarId: {0}", requestAccessId));
            try
            {
                int calendarId = Convert.ToInt32(requestAccessId);
                string localUserIdStr = Session["userId"]?.ToString();
                int userId = 0;
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_USERID", string.Format("Getting userId - Session userId: {0}", localUserIdStr ?? "NULL"));
                
                if (string.IsNullOrEmpty(localUserIdStr))
                {
                    // Try to get userId from username
                    string localUsername = Session["username"]?.ToString();
                    if (!string.IsNullOrEmpty(localUsername))
                    {
                        UsersService us = new UsersService();
                        DataRow user = us.GetUserByEmail(localUsername);
                        if (user == null)
                        {
                            // Try to find by username
                            var allUsers = us.getallusers();
                            if (allUsers != null && allUsers.Tables.Count > 0)
                            {
                                var userRow = allUsers.Tables[0].AsEnumerable()
                                    .FirstOrDefault(r => 
                                        (r["UserName"]?.ToString() ?? "").Equals(localUsername, StringComparison.OrdinalIgnoreCase) ||
                                        (r["userName"]?.ToString() ?? "").Equals(localUsername, StringComparison.OrdinalIgnoreCase));
                                if (userRow != null)
                                {
                                    string idCol = userRow.Table.Columns.Contains("Id") ? "Id" : "id";
                                    localUserIdStr = userRow[idCol]?.ToString();
                                    if (!string.IsNullOrEmpty(localUserIdStr))
                                    {
                                        userId = Convert.ToInt32(localUserIdStr);
                                        Session["userId"] = localUserIdStr;
                                    }
                                }
                            }
                        }
                        else
                        {
                            string idCol = user.Table.Columns.Contains("Id") ? "Id" : "id";
                            localUserIdStr = user[idCol]?.ToString();
                            if (!string.IsNullOrEmpty(localUserIdStr))
                            {
                                userId = Convert.ToInt32(localUserIdStr);
                                Session["userId"] = localUserIdStr;
                            }
                        }
                    }
                }
                else
                {
                    userId = Convert.ToInt32(localUserIdStr);
                }
                
                if (userId <= 0)
                {
                    LoggingService.Log("SHARED_CALENDARS_INVALID_USERID", "UserId is invalid or zero");
                    lblMessage.Text = "שגיאה: לא ניתן לזהות את המשתמש. אנא התחבר מחדש.";
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                    BindCalendars();
                    return;
                }
                
                LoggingService.Log("SHARED_CALENDARS_CALLING_REQUEST_ACCESS", string.Format("Calling RequestAccess - CalendarId: {0}, UserId: {1}", calendarId, userId));
                bool success = service.RequestAccess(calendarId, userId);
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_RESULT", string.Format("RequestAccess result - CalendarId: {0}, UserId: {1}, Success: {2}", calendarId, userId, success));
                
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
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_ERROR", string.Format("Error in request access - CalendarId: {0}, Error: {1}", requestAccessId, ex.Message), ex);
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
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_EVENT", string.Format("Request access from event - CalendarId: {0}", eventArgument));
            try
            {
                int calendarId = Convert.ToInt32(eventArgument);
                int userId = Convert.ToInt32(Session["userId"]);
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_EVENT_CALL", string.Format("Calling RequestAccess - CalendarId: {0}, UserId: {1}", calendarId, userId));
                bool success = service.RequestAccess(calendarId, userId);
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_EVENT_RESULT", string.Format("RequestAccess result - Success: {0}", success));
                
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
            }
            catch (Exception ex)
            {
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_EVENT_ERROR", string.Format("Error in request access event - CalendarId: {0}, Error: {1}", eventArgument, ex.Message), ex);
                lblMessage.Text = "שגיאה בשליחת בקשת הגישה: " + ex.Message;
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
        LoggingService.Log("SHARED_CALENDARS_BIND_START", "BindCalendars started");
        try
        {
            string userIdStr = Session["userId"]?.ToString() ?? "";
            LoggingService.Log("SHARED_CALENDARS_BIND_USERID", string.Format("Getting userId from session - UserId: {0}", userIdStr));
            
            if (string.IsNullOrEmpty(userIdStr))
            {
                LoggingService.Log("SHARED_CALENDARS_BIND_NO_USERID", "UserId is null or empty in session");
                lblMessage.Text = "שגיאה: לא ניתן לזהות את המשתמש. אנא התחבר מחדש.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }
            
            int userId = Convert.ToInt32(userIdStr);
            LoggingService.Log("SHARED_CALENDARS_BIND_CALL_SERVICE", string.Format("Calling GetAllSharedCalendars - UserId: {0}", userId));
            
            DataTable dt = service.GetAllSharedCalendars(userId);
            
            LoggingService.Log("SHARED_CALENDARS_BIND_RESULT", string.Format("GetAllSharedCalendars returned - Rows count: {0}, Columns count: {1}", 
                dt?.Rows.Count ?? 0, dt?.Columns.Count ?? 0));
            
            if (dt != null)
            {
                LoggingService.Log("SHARED_CALENDARS_BIND_COLUMNS", string.Format("DataTable columns: {0}", string.Join(", ", dt.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName))));
                
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        if (dt.Columns.Contains("CalendarName") && row["CalendarName"] != DBNull.Value)
                            row["CalendarName"] = FixEncoding(row["CalendarName"].ToString());
                        if (dt.Columns.Contains("Description") && row["Description"] != DBNull.Value)
                            row["Description"] = FixEncoding(row["Description"].ToString());
                        if (dt.Columns.Contains("CreatorName") && row["CreatorName"] != DBNull.Value)
                            row["CreatorName"] = FixEncoding(row["CreatorName"].ToString());
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("SHARED_CALENDARS_BIND_ENCODING_ERROR", string.Format("Error fixing encoding for row - Error: {0}", ex.Message), ex);
                    }
                }
            }
            
            if (dt != null && dt.Rows.Count > 0)
            {
                LoggingService.Log("SHARED_CALENDARS_BIND_BINDING", string.Format("Binding {0} calendars to DataList", dt.Rows.Count));
                dlCalendars.DataSource = dt;
                dlCalendars.DataBind();
                dlCalendars.Visible = true;
                lblNoCalendars.Visible = false;
                LoggingService.Log("SHARED_CALENDARS_BIND_SUCCESS", "Calendars bound successfully");
            }
            else
            {
                LoggingService.Log("SHARED_CALENDARS_BIND_NO_DATA", "No calendars found");
                dlCalendars.Visible = false;
                lblNoCalendars.Visible = true;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SHARED_CALENDARS_BIND_ERROR", string.Format("Error in BindCalendars - Error: {0}", ex.Message), ex);
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
        LoggingService.Log("SHARED_CALENDARS_CREATE_START", "btnSaveCalendar_Click started");
        try
        {
            string name = txtCalendarName.Text.Trim();
            string description = txtDescription.Text.Trim();
            LoggingService.Log("SHARED_CALENDARS_CREATE_INPUT", string.Format("Create calendar input - Name: {0}, Description length: {1}", name, description?.Length ?? 0));

            if (string.IsNullOrEmpty(name))
            {
                LoggingService.Log("SHARED_CALENDARS_CREATE_EMPTY_NAME", "Calendar name is empty");
                lblMessage.Text = "אנא הזן שם ללוח.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            string userIdStr = Session["userId"]?.ToString() ?? "";
            LoggingService.Log("SHARED_CALENDARS_CREATE_USERID", string.Format("Getting userId - Session userId: {0}", userIdStr));
            
            if (string.IsNullOrEmpty(userIdStr))
            {
                LoggingService.Log("SHARED_CALENDARS_CREATE_NO_USERID", "UserId is null or empty");
                lblMessage.Text = "שגיאה: לא ניתן לזהות את המשתמש. אנא התחבר מחדש.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            int userId = Convert.ToInt32(userIdStr);
            LoggingService.Log("SHARED_CALENDARS_CREATE_CALL_SERVICE", string.Format("Calling CreateSharedCalendar - Name: {0}, UserId: {1}", name, userId));
            
            int calendarId = service.CreateSharedCalendar(name, description, userId);
            
            LoggingService.Log("SHARED_CALENDARS_CREATE_RESULT", string.Format("CreateSharedCalendar returned - CalendarId: {0}", calendarId));

            if (calendarId > 0)
            {
                LoggingService.Log("SHARED_CALENDARS_CREATE_SUCCESS", string.Format("Calendar created successfully - CalendarId: {0}", calendarId));
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
                LoggingService.Log("SHARED_CALENDARS_CREATE_FAILED", "CreateSharedCalendar returned 0 or negative");
                lblMessage.Text = "שגיאה ביצירת הלוח. אנא נסה.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SHARED_CALENDARS_CREATE_ERROR", string.Format("Error in btnSaveCalendar_Click - Error: {0}", ex.Message), ex);
            lblMessage.Text = string.Format("שגיאה ביצירת הלוח: {0}", ex.Message);
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void RequestAccess_Click(object sender, System.Web.UI.WebControls.CommandEventArgs e)
    {
        LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK", "RequestAccess_Click started");
        try
        {
            string commandArg = e.CommandArgument?.ToString() ?? "";
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK_ARG", string.Format("CommandArgument: {0}", commandArg));
            
            int calendarId = Convert.ToInt32(commandArg);
            string userIdStr = Session["userId"]?.ToString() ?? "";
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK_USERID", string.Format("Getting userId - Session userId: {0}", userIdStr));
            
            if (string.IsNullOrEmpty(userIdStr))
            {
                LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK_NO_USERID", "UserId is null or empty");
                lblMessage.Text = "שגיאה: לא ניתן לזהות את המשתמש. אנא התחבר מחדש.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            int userId = Convert.ToInt32(userIdStr);
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK_CALL", string.Format("Calling RequestAccess - CalendarId: {0}, UserId: {1}", calendarId, userId));

            bool success = service.RequestAccess(calendarId, userId);
            
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK_RESULT", string.Format("RequestAccess result - CalendarId: {0}, UserId: {1}, Success: {2}", calendarId, userId, success));

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
            LoggingService.Log("SHARED_CALENDARS_REQUEST_ACCESS_CLICK_ERROR", string.Format("Error in RequestAccess_Click - CommandArgument: {0}, Error: {1}", e.CommandArgument?.ToString() ?? "NULL", ex.Message), ex);
            lblMessage.Text = string.Format("שגיאה בשליחת בקשת הגישה: {0}", ex.Message);
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
            if (dataItem == null)
            {
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_NULL", "dataItem is null");
                return "";
            }
            
            System.Data.DataRowView row = (System.Data.DataRowView)dataItem;
            int calendarId = 0;
            try
            {
                calendarId = Convert.ToInt32(row["CalendarId"]);
            }
            catch (Exception ex)
            {
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_CALENDARID_ERROR", string.Format("Error getting CalendarId - Error: {0}", ex.Message), ex);
                return "";
            }
            
            LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_START", string.Format("Getting action button for CalendarId: {0}", calendarId));
            
            int isMember = 0;
            int isAdmin = 0;
            string requestStatus = "";
            
            try { isMember = Convert.ToInt32(row["IsMember"] ?? 0); } catch (Exception ex) { LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_ISMEMBER_ERROR", string.Format("Error getting IsMember - Error: {0}", ex.Message)); }
            try { isAdmin = Convert.ToInt32(row["IsAdmin"] ?? 0); } catch (Exception ex) { LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_ISADMIN_ERROR", string.Format("Error getting IsAdmin - Error: {0}", ex.Message)); }
            try { requestStatus = row["RequestStatus"]?.ToString() ?? ""; } catch (Exception ex) { LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_STATUS_ERROR", string.Format("Error getting RequestStatus - Error: {0}", ex.Message)); }
            
            LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_VALUES", string.Format("CalendarId: {0}, IsMember: {1}, IsAdmin: {2}, RequestStatus: {3}", calendarId, isMember, isAdmin, requestStatus ?? "NULL"));

            // If user is admin or member, show view button
            if (isMember == 1 || isAdmin == 1)
            {
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_VIEW", string.Format("Showing view button for CalendarId: {0}", calendarId));
                return string.Format("<a href='sharedCalendarDetails.aspx?id={0}' class='btn-view'>צפה בטבלה</a>", calendarId);
            }
            // If user has a pending request, show status only (latest request)
            else if (!string.IsNullOrEmpty(requestStatus) && requestStatus == "Pending")
            {
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_PENDING", string.Format("Showing pending status for CalendarId: {0}", calendarId));
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
                
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_STATUS", string.Format("Showing status for CalendarId: {0}, Status: {1}", calendarId, statusText));
                // Show only the latest status - user can still send new requests but we only show the latest one
                return string.Format("<span class='btn-requested'>{0}</span>", statusText);
            }
            // Otherwise, show request access button
            else
            {
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_REQUEST", string.Format("Showing request button for CalendarId: {0}", calendarId));
                return string.Format("<button type='button' onclick='requestAccess({0})' class='btn-request'>בקש גישה</button>", calendarId);
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_ERROR", string.Format("Error in GetCalendarActionButton - Error: {0}", ex.Message), ex);
            // In case of error, show request button as fallback
            try
            {
                System.Data.DataRowView row = (System.Data.DataRowView)dataItem;
                int calendarId = Convert.ToInt32(row["CalendarId"]);
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_FALLBACK", string.Format("Using fallback request button for CalendarId: {0}", calendarId));
                return string.Format("<button type='button' onclick='requestAccess({0})' class='btn-request'>בקש גישה</button>", calendarId);
            }
            catch (Exception fallbackEx)
            {
                LoggingService.Log("SHARED_CALENDARS_ACTION_BUTTON_FALLBACK_ERROR", string.Format("Error in fallback - Error: {0}", fallbackEx.Message), fallbackEx);
                return "";
            }
        }
    }
}
