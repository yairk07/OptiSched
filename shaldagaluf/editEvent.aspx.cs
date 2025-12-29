using System;
using System.Data;
using System.Data.OleDb;

public partial class editEvent : System.Web.UI.Page
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

            string sql = "SELECT * FROM calnder WHERE Id = ?";

            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("?", eventId);

                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (!dr.Read())
                    {
                        Response.Redirect("allEvents.aspx");
                        return;
                    }

                    int rowUserId = Convert.ToInt32(dr["Userid"]);
                    int currentUserId = Convert.ToInt32(Session["userId"]);
                    string role = Session["Role"]?.ToString() ?? "user";

                    if (role != "owner" && rowUserId != currentUserId)
                    {
                        Response.Write("אין לך הרשאה לערוך את האירוע הזה.");
                        Response.End();
                        return;
                    }

                    txtTitle.Text = dr["title"].ToString();
                    txtDate.Text = Convert.ToDateTime(dr["date"]).ToString("yyyy-MM-dd");
                    txtTime.Text = dr["time"].ToString();
                    txtNotes.Text = dr["notes"].ToString();
                    
                    if (dr["category"] != DBNull.Value && dr["category"] != null)
                    {
                        string category = dr["category"].ToString();
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

            string sql = @"
                UPDATE calnder
                SET title = ?,
                    [date] = ?,
                    [time] = ?,
                    notes = ?,
                    category = ?
                WHERE Id = ?";

            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("?", txtTitle.Text);
                cmd.Parameters.AddWithValue("?", DateTime.Parse(txtDate.Text));
                cmd.Parameters.AddWithValue("?", txtTime.Text);
                cmd.Parameters.AddWithValue("?", txtNotes.Text);
                cmd.Parameters.AddWithValue("?", ddlCategory.SelectedValue);
                cmd.Parameters.AddWithValue("?", eventId);

                cmd.ExecuteNonQuery();
            }

            Response.Redirect("allEvents.aspx");
        }
    }
}
