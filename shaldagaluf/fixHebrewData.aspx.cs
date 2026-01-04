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

        string role = Session["Role"]?.ToString() ?? "";
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
            int fixedCount = 0;
            StringBuilder results = new StringBuilder();

            string conStr = Connect.GetConnectionString();
            using (OleDbConnection con = new OleDbConnection(conStr))
            {
                con.Open();

                if (tableName == "Users")
                {
                    fixedCount = FixUsersHebrew(con, results);
                }
                else if (tableName == "CalendarEvents")
                {
                    fixedCount = FixCalendarEventsHebrew(con, results);
                }
                else if (tableName == "SharedCalendarEvents")
                {
                    fixedCount = FixSharedCalendarEventsHebrew(con, results);
                }
                else
                {
                    ShowMessage("טבלה לא ידועה: " + tableName, true);
                    return;
                }
            }

            results.AppendLine("סה\"כ שורות שתוקנו: " + fixedCount);
            lblResults.Text = results.ToString();
            pnlResults.Visible = true;
            ShowMessage("תיקון הושלם בהצלחה! " + fixedCount + " שורות תוקנו.", false);
        }
        catch (Exception ex)
        {
            ShowMessage("שגיאה בתיקון: " + ex.Message, true);
        }
    }

    private int FixUsersHebrew(OleDbConnection con, StringBuilder results)
    {
        int fixedCount = 0;
        results.AppendLine("תיקון טבלת Users:");
        results.AppendLine("");

        string[] columnsToFix = { "FirstName", "LastName", "UserName", "Email" };
        string[] oldColumnNames = { "firstName", "lastName", "userName", "email" };

        string sql = "SELECT Id, FirstName, LastName, UserName, Email, firstName, lastName, userName, email FROM Users";
        
        using (OleDbCommand cmd = new OleDbCommand(sql, con))
        {
            using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);

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
                                string updateSql = "UPDATE Users SET [" + updateCol + "] = ? WHERE Id = ?";
                                
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
        }

        return fixedCount;
    }

    private int FixCalendarEventsHebrew(OleDbConnection con, StringBuilder results)
    {
        int fixedCount = 0;
        results.AppendLine("תיקון טבלת CalendarEvents:");
        results.AppendLine("");

        string sql = "SELECT Id, Title, Notes, Category FROM CalendarEvents";
        
        using (OleDbCommand cmd = new OleDbCommand(sql, con))
        {
            using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    bool rowUpdated = false;
                    int eventId = Convert.ToInt32(row["Id"]);

                    string[] columnsToFix = { "Title", "Notes", "Category" };
                    foreach (string col in columnsToFix)
                    {
                        if (dt.Columns.Contains(col) && row[col] != DBNull.Value)
                        {
                            string originalValue = row[col].ToString();
                            string fixedValue = Connect.FixEncoding(originalValue);

                            if (originalValue != fixedValue && !string.IsNullOrEmpty(fixedValue))
                            {
                                string updateSql = "UPDATE CalendarEvents SET [" + col + "] = ? WHERE Id = ?";
                                
                                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, con))
                                {
                                    updateCmd.Parameters.AddWithValue("?", fixedValue);
                                    updateCmd.Parameters.AddWithValue("?", eventId);
                                    updateCmd.ExecuteNonQuery();
                                }

                                results.AppendLine("תוקן אירוע ID " + eventId + ", עמודה " + col);
                                rowUpdated = true;
                            }
                        }
                    }

                    if (rowUpdated)
                        fixedCount++;
                }
            }
        }

        return fixedCount;
    }

    private int FixSharedCalendarEventsHebrew(OleDbConnection con, StringBuilder results)
    {
        int fixedCount = 0;
        results.AppendLine("תיקון טבלת SharedCalendarEvents:");
        results.AppendLine("");

        string sql = "SELECT Id, Title, Notes, Category FROM SharedCalendarEvents";
        
        using (OleDbCommand cmd = new OleDbCommand(sql, con))
        {
            using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    bool rowUpdated = false;
                    int eventId = Convert.ToInt32(row["Id"]);

                    string[] columnsToFix = { "Title", "Notes", "Category" };
                    foreach (string col in columnsToFix)
                    {
                        if (dt.Columns.Contains(col) && row[col] != DBNull.Value)
                        {
                            string originalValue = row[col].ToString();
                            string fixedValue = Connect.FixEncoding(originalValue);

                            if (originalValue != fixedValue && !string.IsNullOrEmpty(fixedValue))
                            {
                                string updateSql = "UPDATE SharedCalendarEvents SET [" + col + "] = ? WHERE Id = ?";
                                
                                using (OleDbCommand updateCmd = new OleDbCommand(updateSql, con))
                                {
                                    updateCmd.Parameters.AddWithValue("?", fixedValue);
                                    updateCmd.Parameters.AddWithValue("?", eventId);
                                    updateCmd.ExecuteNonQuery();
                                }

                                results.AppendLine("תוקן אירוע משותף ID " + eventId + ", עמודה " + col);
                                rowUpdated = true;
                            }
                        }
                    }

                    if (rowUpdated)
                        fixedCount++;
                }
            }
        }

        return fixedCount;
    }

    private void ShowMessage(string message, bool isError)
    {
        lblMessage.Text = message;
        lblMessage.Visible = true;
        lblMessage.CssClass = isError ? "message-label error-message" : "message-label success-message";
    }
}



