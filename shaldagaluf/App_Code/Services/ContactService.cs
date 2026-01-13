using System;
using System.Data;
using System.Data.OleDb;

public class ContactService
{
    /// <summary>
    /// Saves contact message to database
    /// </summary>
    // Validates and saves contact message to database with Hebrew encoding fixes
    public int SaveContactMessage(string fullName, string email, string subject, string message)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("שם מלא הוא שדה חובה");
        
        // Basic email validation (must contain @)
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            throw new ArgumentException("אימייל לא תקין");

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("הודעה היא שדה חובה");

        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            // Insert contact message with Hebrew encoding fixes and normalized email
            string sql = "INSERT INTO ContactMessages (full_name, email, subject, message, created_at) VALUES (?, ?, ?, ?, ?)";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", Connect.FixEncoding(fullName));
                cmd.Parameters.AddWithValue("?", email.Trim().ToLower());
                cmd.Parameters.AddWithValue("?", Connect.FixEncoding(subject ?? ""));
                cmd.Parameters.AddWithValue("?", Connect.FixEncoding(message));
                cmd.Parameters.AddWithValue("?", DateTime.Now);
                cmd.ExecuteNonQuery();
            }

            // Get the inserted message ID using Access identity function
            sql = "SELECT @@IDENTITY";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                int messageId = Convert.ToInt32(result);
                LoggingService.Log("ContactService", string.Format("Contact message saved - MessageId: {0}, Email: {1}", messageId, email));
                return messageId;
            }
        }
    }

    /// <summary>
    /// Gets all contact messages (for admins)
    /// </summary>
    // Retrieves all contact messages ordered by creation date (newest first) for admin viewing
    public DataTable GetAllContactMessages()
    {
        string connectionString = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string sql = "SELECT * FROM ContactMessages ORDER BY created_at DESC";

            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
        }

        return dt;
    }

    /// <summary>
    /// Gets contact message by ID
    /// </summary>
    public DataRow GetContactMessageById(int messageId)
    {
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string sql = "SELECT * FROM ContactMessages WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", messageId);
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Id", typeof(int));
                        dt.Columns.Add("full_name", typeof(string));
                        dt.Columns.Add("email", typeof(string));
                        dt.Columns.Add("subject", typeof(string));
                        dt.Columns.Add("message", typeof(string));
                        dt.Columns.Add("created_at", typeof(DateTime));

                        DataRow row = dt.NewRow();
                        row["Id"] = dr["Id"];
                        row["full_name"] = dr["full_name"] != DBNull.Value ? dr["full_name"].ToString() : "";
                        row["email"] = dr["email"] != DBNull.Value ? dr["email"].ToString() : "";
                        row["subject"] = dr["subject"] != DBNull.Value ? dr["subject"].ToString() : "";
                        row["message"] = dr["message"] != DBNull.Value ? dr["message"].ToString() : "";
                        row["created_at"] = dr["created_at"] != DBNull.Value ? dr["created_at"] : DateTime.MinValue;
                        dt.Rows.Add(row);

                        return dt.Rows[0];
                    }
                }
            }
        }

        return null;
    }
}

