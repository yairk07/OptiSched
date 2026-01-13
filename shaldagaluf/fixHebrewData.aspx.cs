using System;
using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Web.UI.WebControls;

public partial class fixHebrewData : System.Web.UI.Page
{
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

        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        bool isOwner = string.Equals(role, "owner", StringComparison.OrdinalIgnoreCase);

        if (!isOwner)
        {
            pnlNotAuthorized.Visible = true;
            pnlContent.Visible = false;
            return;
        }

        pnlNotAuthorized.Visible = false;
        pnlContent.Visible = true;
    }

    protected void btnFix_Click(object sender, EventArgs e)
    {
        try
        {
            string tableName = ddlTable.SelectedValue;
            StringBuilder results = new StringBuilder();
            int fixedCount = 0;

            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                DataTable dt = new DataTable();
                string sql = "SELECT * FROM [" + tableName + "]";
                using (OleDbCommand cmd = new OleDbCommand(sql, con))
                {
                    using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }

                string[] columnsToFix = null;
                string[] oldColumnNames = null;

                if (tableName == "Users")
                {
                    columnsToFix = new string[] { "firstName", "lastName", "userName", "email" };
                    oldColumnNames = new string[] { "FirstName", "LastName", "UserName", "Email" };
                }
                else if (tableName == "CalendarEvents" || tableName == "SharedCalendarEvents")
                {
                    columnsToFix = new string[] { "Title", "EventTime", "Notes", "Category" };
                    oldColumnNames = new string[] { "title", "time", "notes", "category" };
                }
                else
                {
                    results.AppendLine("טבלה לא נתמכת: " + tableName);
                    lblResults.Text = results.ToString();
                    pnlResults.Visible = true;
                    return;
                }

                foreach (DataRow row in dt.Rows)
                {
                    bool rowUpdated = false;
                    int userId = Convert.ToInt32(row["Id"]);

                    for (int i = 0; i < columnsToFix.Length; i++)
                    {
                        string newCol = columnsToFix[i];
                        string oldCol = oldColumnNames[i];

                        object value = null;
                        if (dt.Columns.Contains(newCol) && row[newCol] != DBNull.Value)
                        {
                            value = row[newCol];
                        }
                        else if (dt.Columns.Contains(oldCol) && row[oldCol] != DBNull.Value)
                        {
                            value = row[oldCol];
                        }

                        if (value != null && value != DBNull.Value)
                        {
                            string originalValue = value.ToString();
                            string fixedValue = Connect.FixEncoding(originalValue);

                            if (originalValue != fixedValue && !string.IsNullOrEmpty(fixedValue))
                            {
                                string updateCol = dt.Columns.Contains(newCol) ? newCol : oldCol;
                                string updateSql = "UPDATE [" + tableName + "] SET [" + updateCol + "] = ? WHERE Id = ?";
                                
                                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, con))
                                {
                                    updateCmd.Parameters.AddWithValue("?", fixedValue);
                                    updateCmd.Parameters.AddWithValue("?", userId);
                                    updateCmd.ExecuteNonQuery();
                                }

                                results.AppendLine("תוקן משתמש ID " + userId + ", עמודה " + updateCol + ": " + originalValue.Substring(0, Math.Min(20, originalValue.Length)) + " -> " + fixedValue.Substring(0, Math.Min(20, fixedValue.Length)));
                                rowUpdated = true;
                            }
                        }
                    }

                    if (rowUpdated)
                        fixedCount++;
                }
            }

            results.Insert(0, "סה\"כ תוקנו " + fixedCount + " שורות.\n\n");
            lblResults.Text = results.ToString();
            pnlResults.Visible = true;
        }
        catch (Exception ex)
        {
            LoggingService.Log("fixHebrewData", "Error fixing Hebrew data", ex);
            lblResults.Text = "שגיאה: " + ex.Message;
            pnlResults.Visible = true;
        }
    }
}
