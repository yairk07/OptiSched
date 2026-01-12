using System;
using System.Data;
using System.Data.OleDb;

public partial class editEvent : System.Web.UI.Page
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
            Response.Redirect("allEvents.aspx");
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

            // DSD Schema: Use CalendarEvents table with UserId, EventDate, EventTime columns
            string sql = "SELECT * FROM CalendarEvents WHERE Id = ?";

            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = eventId;
                cmd.Parameters.Add(idParam);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        Response.Redirect("allEvents.aspx");
                        return;
                    }

                    // DSD Schema: Use UserId column
                    int rowUserId = Convert.ToInt32(dr["UserId"]);
                    int currentUserId = Convert.ToInt32(Session["userId"]);
                    string role = Session["Role"] != null ? Session["Role"].ToString() : "user";

                    if (role != "owner" && rowUserId != currentUserId)
                    {
                        Response.Write("אין לך הרשאה לערוך את האירוע הזה.");
                        Response.End();
                        return;
                    }

                    // DSD Schema: Use Title, EventDate, EventTime, Notes, Category columns
                    txtTitle.Text = dr["Title"].ToString();
                    txtDate.Text = Convert.ToDateTime(dr["EventDate"]).ToString("yyyy-MM-dd");
                    txtTime.Text = dr["EventTime"].ToString();
                    txtNotes.Text = dr["Notes"].ToString();
                    
                    if (dr["Category"] != DBNull.Value && dr["Category"] != null)
                    {
                        string category = dr["Category"].ToString();
                        if (ddlCategory.Items.FindByValue(category) != null)
                        {
                            ddlCategory.SelectedValue = category;
                        }
                    }
                }
            }
        }

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
            LoggingService.Log("editEvent", "Error loading files", ex);
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
            LoggingService.Log("editEvent", "Error loading images", ex);
            pnlImages.Visible = false;
        }
    }

    protected void rptFiles_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
    {
        if (e.CommandName == "DeleteFile")
        {
            int fileId = Convert.ToInt32(e.CommandArgument);
            int userId = Convert.ToInt32(Session["userId"]);
            string role = Session["Role"] != null ? Session["Role"].ToString() : "user";
            
            try
            {
                FileService fileService = new FileService();
                fileService.DeleteFile(fileId, userId, role);
                LoadEventFiles();
            }
            catch (Exception ex)
            {
                LoggingService.Log("editEvent", "Error deleting file", ex);
            }
        }
    }

    protected void rptImages_ItemCommand(object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e)
    {
        if (e.CommandName == "DeleteImage")
        {
            int imageId = Convert.ToInt32(e.CommandArgument);
            int userId = Convert.ToInt32(Session["userId"]);
            string role = Session["Role"] != null ? Session["Role"].ToString() : "user";
            
            try
            {
                ImageService imageService = new ImageService();
                imageService.DeleteImage(imageId, userId, role);
                LoadEventImages();
            }
            catch (Exception ex)
            {
                LoggingService.Log("editEvent", "Error deleting image", ex);
            }
        }
    }

    protected void btnSave_Click(object sender, EventArgs e)
    {
        string conStr = Connect.GetConnectionString();

        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();

            // DSD Schema: Use CalendarEvents table with Title, EventDate, EventTime, Notes, Category columns
            string sql = @"
                UPDATE CalendarEvents
                SET Title = ?,
                    EventDate = ?,
                    EventTime = ?,
                    Notes = ?,
                    Category = ?
                WHERE Id = ?";

            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                OleDbParameter titleParam = new OleDbParameter("?", OleDbType.WChar);
                titleParam.Value = txtTitle.Text != null ? txtTitle.Text.Trim() : "";
                cmd.Parameters.Add(titleParam);
                
                OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                dateParam.Value = DateTime.Parse(txtDate.Text);
                cmd.Parameters.Add(dateParam);
                
                OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
                timeParam.Value = txtTime.Text != null ? txtTime.Text.Trim() : "";
                cmd.Parameters.Add(timeParam);
                
                OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
                notesParam.Value = txtNotes.Text != null ? txtNotes.Text.Trim() : "";
                cmd.Parameters.Add(notesParam);
                
                OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
                categoryParam.Value = ddlCategory.SelectedValue != null ? ddlCategory.SelectedValue.Trim() : "";
                cmd.Parameters.Add(categoryParam);
                
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = eventId;
                cmd.Parameters.Add(idParam);

                cmd.ExecuteNonQuery();
            }

            // Handle file upload
            if (fileUpload.HasFile)
            {
                try
                {
                    int userId = Convert.ToInt32(Session["userId"]);
                    FileService fileService = new FileService();
                    fileService.SaveFile(fileUpload.PostedFile, eventId, userId);
                }
                catch (Exception ex)
                {
                    LoggingService.Log("editEvent", "Error uploading file", ex);
                }
            }

            // Handle image upload
            if (imageUpload.HasFile)
            {
                try
                {
                    int userId = Convert.ToInt32(Session["userId"]);
                    ImageService imageService = new ImageService();
                    imageService.SaveImage(imageUpload.PostedFile, eventId, userId);
                }
                catch (Exception ex)
                {
                    LoggingService.Log("editEvent", "Error uploading image", ex);
                }
            }

            Response.Redirect("allEvents.aspx");
        }
    }
}
