using System;
using System.Data;
using System.Web.UI;

public partial class calendarAccessRequests : System.Web.UI.Page
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

        // Allow both owners and calendar managers to access this page
        string role = Convert.ToString(Session["Role"]);
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);

        if (!isOwner)
        {
            // Check if user is a manager of any calendar
            int userId = Convert.ToInt32(Session["userId"]);
            bool isCalendarManager = IsCalendarManager(userId);
            
            if (!isCalendarManager)
            {
                lblMessage.Text = "אין לך הרשאה לגשת לדף זה. רק מנהלי טבלאות ובעל האתר יכולים לאשר בקשות גישה.";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                rptRequests.Visible = false;
                return;
            }
        }

        if (!IsPostBack)
        {
            BindRequests();
        }
    }

    private bool IsCalendarManager(int userId)
    {
        try
        {
            string conStr = Connect.GetConnectionString();
            using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
            {
                con.Open();
                string sql = "SELECT COUNT(*) FROM SharedCalendars WHERE CreatedBy = ?";
                using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", userId);
                    object result = cmd.ExecuteScalar();
                    return result != null && Convert.ToInt32(result) > 0;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    private void BindRequests()
    {
        try
        {
            int userId = Convert.ToInt32(Session["userId"]);
            string role = Convert.ToString(Session["Role"]);
            bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);

            DataTable dt = new DataTable();
            string conStr = Connect.GetConnectionString();
            
            using (System.Data.OleDb.OleDbConnection con = new System.Data.OleDb.OleDbConnection(conStr))
            {
                con.Open();
                
                // Get all pending requests - we'll filter to show only latest per user-calendar in code
                string sql = @"
SELECT 
    JR.Id AS RequestId,
    JR.CalendarId,
    JR.UserId,
    JR.Status,
    JR.RequestDate,
    JR.Message,
    U.UserName AS RequesterName,
    U.Email AS RequesterEmail,
    SC.Name AS CalendarName,
    SC.CreatedBy
FROM ((JoinRequests JR
INNER JOIN Users U ON CLng(JR.UserId) = CLng(U.Id))
INNER JOIN SharedCalendars SC ON JR.CalendarId = SC.Id)
WHERE JR.Status = 'Pending'";

                // If user is owner, show all requests
                // Otherwise, show only requests for calendars they manage
                if (!isOwner)
                {
                    sql += " AND SC.CreatedBy = ?";
                }
                
                sql += " ORDER BY JR.RequestDate DESC";

                using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, con))
                {
                    if (!isOwner)
                    {
                        cmd.Parameters.AddWithValue("?", userId);
                    }
                    
                    using (System.Data.OleDb.OleDbDataAdapter da = new System.Data.OleDb.OleDbDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            // Filter to show only the latest request per user per calendar
            if (dt != null && dt.Rows.Count > 0)
            {
                DataTable filteredDt = dt.Clone();
                System.Collections.Generic.Dictionary<string, DataRow> latestRequests = new System.Collections.Generic.Dictionary<string, DataRow>();
                
                foreach (DataRow row in dt.Rows)
                {
                    int calendarId = Convert.ToInt32(row["CalendarId"]);
                    int requestUserId = Convert.ToInt32(row["UserId"]);
                    string key = calendarId + "_" + requestUserId;
                    DateTime requestDate = Convert.ToDateTime(row["RequestDate"]);
                    
                    if (!latestRequests.ContainsKey(key))
                    {
                        latestRequests[key] = row;
                    }
                    else
                    {
                        DateTime existingDate = Convert.ToDateTime(latestRequests[key]["RequestDate"]);
                        if (requestDate > existingDate)
                        {
                            latestRequests[key] = row;
                        }
                    }
                }
                
                foreach (var kvp in latestRequests)
                {
                    filteredDt.ImportRow(kvp.Value);
                }
                
                // Sort by request date descending
                System.Data.DataView dv = filteredDt.DefaultView;
                dv.Sort = "RequestDate DESC";
                filteredDt = dv.ToTable();
                
                if (filteredDt.Rows.Count > 0)
                {
                    rptRequests.DataSource = filteredDt;
                    rptRequests.DataBind();
                    rptRequests.Visible = true;
                    lblNoRequests.Visible = false;
                }
                else
                {
                    rptRequests.Visible = false;
                    lblNoRequests.Visible = true;
                }
            }
            else
            {
                rptRequests.Visible = false;
                lblNoRequests.Visible = true;
            }
        }
        catch (Exception ex)
        {
            lblMessage.Text = "שגיאה בטעינת הבקשות: " + ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    protected void rptRequests_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
    {
        try
        {
            int requestId = Convert.ToInt32(e.CommandArgument);
            int approverUserId = Convert.ToInt32(Session["userId"]);

            if (e.CommandName == "Approve")
            {
                // Get permission from dropdown
                System.Web.UI.WebControls.DropDownList ddlPermission = (System.Web.UI.WebControls.DropDownList)e.Item.FindControl("ddlPermission");
                string permission = "Read";
                if (ddlPermission != null)
                {
                    permission = ddlPermission.SelectedValue;
                }

                bool success = service.ApproveRequest(requestId, approverUserId, permission);
                if (success)
                {
                    lblMessage.Text = "הבקשה אושרה בהצלחה!";
                    lblMessage.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblMessage.Text = "שגיאה באישור הבקשה.";
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                }
            }
            else if (e.CommandName == "Reject")
            {
                bool success = service.RejectRequest(requestId, approverUserId);
                if (success)
                {
                    lblMessage.Text = "הבקשה נדחתה.";
                    lblMessage.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblMessage.Text = "שגיאה בדחיית הבקשה.";
                    lblMessage.ForeColor = System.Drawing.Color.Red;
                }
            }

            BindRequests();
        }
        catch (Exception ex)
        {
            lblMessage.Text = "שגיאה בעיבוד הבקשה: " + ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }
}


