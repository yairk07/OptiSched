using System;
using System.Data;
using System.Data.OleDb;

public partial class eventDetails : System.Web.UI.Page
{
    private int eventId;

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

        if (!int.TryParse(Request.QueryString["id"], out eventId))
        {
            ShowNotFound();
            return;
        }

        if (!IsPostBack)
            LoadEvent();
    }

    private void LoadEvent()
    {
        string conStr = Connect.GetConnectionString();

        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();

            // Try to get event from CalendarEvents first
            string sql = "SELECT * FROM CalendarEvents WHERE Id = ?";
            bool found = false;

            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = eventId;
                cmd.Parameters.Add(idParam);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        found = true;
                        LoadEventData(dr, con);
                    }
                }
            }

            // If not found in CalendarEvents, try SharedCalendarEvents
            if (!found)
            {
                sql = "SELECT * FROM SharedCalendarEvents WHERE Id = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                    idParam.Value = eventId;
                    cmd.Parameters.Add(idParam);

                    using (OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            found = true;
                            LoadEventData(dr, con);
                        }
                    }
                }
            }

            if (!found)
            {
                ShowNotFound();
            }
        }
    }

    private void LoadEventData(OleDbDataReader dr, OleDbConnection con)
    {
        int rowUserId = 0;
        if (dr["UserId"] != DBNull.Value && dr["UserId"] != null)
        {
            rowUserId = Convert.ToInt32(dr["UserId"]);
        }

        int currentUserId = 0;
        if (Session["userId"] != null)
        {
            currentUserId = Convert.ToInt32(Session["userId"]);
        }

        string role = Session["Role"] != null ? Session["Role"].ToString() : "user";

        // Check permissions - owner can see all, others can only see their own events
        if (role != "owner" && rowUserId != currentUserId)
        {
            ShowNotFound();
            return;
        }

        pnlContent.Visible = true;
        pnlNotFound.Visible = false;

        // Load event data
        if (dr["Title"] != DBNull.Value && dr["Title"] != null)
            lblTitle.Text = Connect.FixEncoding(dr["Title"].ToString());
        else
            lblTitle.Text = "(ללא כותרת)";

        if (dr["EventDate"] != DBNull.Value && dr["EventDate"] != null)
        {
            DateTime eventDate = Convert.ToDateTime(dr["EventDate"]);
            lblDate.Text = eventDate.ToString("dd/MM/yyyy");
        }
        else
            lblDate.Text = "-";

        if (dr["EventTime"] != DBNull.Value && dr["EventTime"] != null)
            lblTime.Text = Connect.FixEncoding(dr["EventTime"].ToString());
        else
            lblTime.Text = "-";

        if (dr["Category"] != DBNull.Value && dr["Category"] != null)
            lblCategory.Text = Connect.FixEncoding(dr["Category"].ToString());
        else
            lblCategory.Text = "אחר";

        if (dr["Notes"] != DBNull.Value && dr["Notes"] != null)
            lblNotes.Text = Connect.FixEncoding(dr["Notes"].ToString());
        else
            lblNotes.Text = "-";

        if (dr["EventType"] != DBNull.Value && dr["EventType"] != null)
            lblEventType.Text = Connect.FixEncoding(dr["EventType"].ToString());
        else
            lblEventType.Text = "אישי";

        // Load user name
        string userName = "";
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
                    OleDbParameter userIdParam = new OleDbParameter("?", OleDbType.Integer);
                    userIdParam.Value = rowUserId;
                    userCmd.Parameters.Add(userIdParam);

                    object userNameObj = userCmd.ExecuteScalar();
                    if (userNameObj != null && userNameObj != DBNull.Value)
                    {
                        userName = Connect.FixEncoding(userNameObj.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Log("EVENTDETAILS", "Error loading user name", ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(userName))
            lblUserName.Text = userName;
        else
            lblUserName.Text = "-";

        // Set edit link
        lnkEdit.NavigateUrl = "editEvent.aspx?id=" + eventId.ToString();

        // Load files and images
        LoadEventFiles();
        LoadEventImages();
    }

    private void LoadEventFiles()
    {
        try
        {
            FileService fileService = new FileService();
            DataTable files = fileService.GetFilesByEvent(eventId);
            
            if (files != null && files.Rows.Count > 0)
            {
                rptFiles.DataSource = files;
                rptFiles.DataBind();
                pnlFiles.Visible = true;
            }
            else
            {
                pnlFiles.Visible = false;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("eventDetails", "Error loading files", ex);
            pnlFiles.Visible = false;
        }
    }

    private void LoadEventImages()
    {
        try
        {
            ImageService imageService = new ImageService();
            DataTable images = imageService.GetImagesByEvent(eventId);
            
            if (images != null && images.Rows.Count > 0)
            {
                rptImages.DataSource = images;
                rptImages.DataBind();
                pnlImages.Visible = true;
            }
            else
            {
                pnlImages.Visible = false;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Log("eventDetails", "Error loading images", ex);
            pnlImages.Visible = false;
        }
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

    private void ShowNotFound()
    {
        pnlContent.Visible = false;
        pnlNotFound.Visible = true;
    }
}

