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
                    string role = Session["Role"]?.ToString() ?? "user";

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
                titleParam.Value = txtTitle.Text?.Trim() ?? "";
                cmd.Parameters.Add(titleParam);
                
                OleDbParameter dateParam = new OleDbParameter("?", OleDbType.Date);
                dateParam.Value = DateTime.Parse(txtDate.Text);
                cmd.Parameters.Add(dateParam);
                
                OleDbParameter timeParam = new OleDbParameter("?", OleDbType.WChar);
                timeParam.Value = txtTime.Text?.Trim() ?? "";
                cmd.Parameters.Add(timeParam);
                
                OleDbParameter notesParam = new OleDbParameter("?", OleDbType.WChar);
                notesParam.Value = txtNotes.Text?.Trim() ?? "";
                cmd.Parameters.Add(notesParam);
                
                OleDbParameter categoryParam = new OleDbParameter("?", OleDbType.WChar);
                categoryParam.Value = ddlCategory.SelectedValue?.Trim() ?? "";
                cmd.Parameters.Add(categoryParam);
                
                OleDbParameter idParam = new OleDbParameter("?", OleDbType.Integer);
                idParam.Value = eventId;
                cmd.Parameters.Add(idParam);

                cmd.ExecuteNonQuery();
            }

            Response.Redirect("allEvents.aspx");
        }
    }
}
