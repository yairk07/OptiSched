using System;
using System.Data;
using System.Data.OleDb;
using System.Text;

public partial class eventDetails : System.Web.UI.Page
{
    private int eventId;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!int.TryParse(Request.QueryString["id"], out eventId))
        {
            ShowNotFound();
            return;
        }

        if (!IsPostBack)
        {
            LoadEvent();
        }
    }

    private void LoadEvent()
    {
        try
        {
            string conStr = Connect.GetConnectionString();
            bool found = false;

            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                string sql = "SELECT * FROM CalendarEvents WHERE Id = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("?", eventId);
                    using (OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            found = true;
                            LoadEventData(dr, con);
                            return;
                        }
                    }
                }

                if (!found)
                {
                    sql = "SELECT * FROM SharedCalendarEvents WHERE Id = ?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("?", eventId);
                        using (OleDbDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                found = true;
                                LoadEventData(dr, con);
                                return;
                            }
                        }
                    }
                }
            }

            if (!found)
            {
                ShowNotFound();
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("EVENTDETAILS", "Error loading event", ex);
            ShowNotFound();
        }
    }

    private void LoadEventData(OleDbDataReader dr, OleDbConnection con)
    {
        int rowUserId = dr["UserId"] != DBNull.Value ? Convert.ToInt32(dr["UserId"]) : 0;
        int currentUserId = Session["userId"] != null ? Convert.ToInt32(Session["userId"]) : 0;
        string role = Session["Role"] != null ? Session["Role"].ToString() : "user";

        if (role != "owner" && rowUserId != currentUserId)
        {
            ShowNotFound();
            return;
        }

        pnlContent.Visible = true;
        pnlNotFound.Visible = false;

        if (dr["Title"] != DBNull.Value && dr["Title"] != null)
        {
            lblTitle.Text = dr["Title"].ToString();
        }
        else
        {
            lblTitle.Text = "(No Title)";
        }

        if (dr["EventTime"] != DBNull.Value && dr["EventTime"] != null)
        {
            lblTime.Text = dr["EventTime"].ToString();
        }
        else
        {
            lblTime.Text = "-";
        }

        if (dr["Category"] != DBNull.Value && dr["Category"] != null)
        {
            lblCategory.Text = dr["Category"].ToString();
        }
        else
        {
            lblCategory.Text = "Other";
        }

        if (dr["Notes"] != DBNull.Value && dr["Notes"] != null)
        {
            lblNotes.Text = dr["Notes"].ToString();
        }
        else
        {
            lblNotes.Text = "-";
        }

        if (dr["EventDate"] != DBNull.Value && dr["EventDate"] != null)
        {
            lblDate.Text = Convert.ToDateTime(dr["EventDate"]).ToString("dd/MM/yyyy");
        }
        else
        {
            lblDate.Text = "-";
        }

        string userName = "-";
        if (rowUserId > 0)
        {
            try
            {
                string userNameCol = "UserName";
                if (!ColumnExists(con, "Users", "UserName"))
                {
                    if (ColumnExists(con, "Users", "userName"))
                        userNameCol = "userName";
                    else if (ColumnExists(con, "Users", "username"))
                        userNameCol = "username";
                }

                string userSql = "SELECT " + userNameCol + " FROM Users WHERE Id = ?";
                using (OleDbCommand userCmd = new OleDbCommand(userSql, con))
                {
                    userCmd.Parameters.AddWithValue("?", rowUserId);
                    object userNameObj = userCmd.ExecuteScalar();
                    if (userNameObj != null && userNameObj != DBNull.Value)
                    {
                        userName = userNameObj.ToString();
                    }
                }
            }
            catch { }
        }
        lblUserName.Text = userName;

        lnkEdit.NavigateUrl = "editEvent.aspx?id=" + eventId;

        LoadEventFiles();
        LoadEventImages();
    }

    private bool ColumnExists(OleDbConnection conn, string tableName, string columnName)
    {
        try
        {
            string[] variations = { columnName, columnName.ToLower(), columnName.ToUpper(), 
                                   char.ToUpper(columnName[0]) + columnName.Substring(1).ToLower() };
            
            foreach (string variant in variations)
            {
                try
                {
                    using (OleDbCommand cmd = new OleDbCommand("SELECT TOP 1 [" + variant + "] FROM [" + tableName + "]", conn))
                    {
                        cmd.ExecuteScalar();
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }


    private void LoadEventFiles()
    {
        try
        {
            FileService fs = new FileService();
            DataTable dt = fs.GetFilesByEvent(eventId);
            pnlFiles.Visible = dt != null && dt.Rows.Count > 0;
            rptFiles.DataSource = dt;
            rptFiles.DataBind();
        }
        catch { pnlFiles.Visible = false; }
    }

    private void LoadEventImages()
    {
        try
        {
            ImageService ims = new ImageService();
            DataTable dt = ims.GetImagesByEvent(eventId);
            pnlImages.Visible = dt != null && dt.Rows.Count > 0;
            rptImages.DataSource = dt;
            rptImages.DataBind();
        }
        catch { pnlImages.Visible = false; }
    }

    private void ShowNotFound()
    {
        pnlContent.Visible = false;
        pnlNotFound.Visible = true;
    }
}
