using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Web;

public class FileService
{
    private const int MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    private const string UPLOAD_FOLDER = "~/App_Data/Uploads/Files/";

    /// <summary>
    /// Saves uploaded file to server and creates database record
    /// </summary>
    // Saves uploaded file to server, creates database record, and links it to an event
    public int SaveFile(HttpPostedFile file, int eventId, int uploadedBy)
    {
        // Validate file exists and has content
        if (file == null || file.ContentLength == 0)
            throw new ArgumentException("קובץ לא תקין");

        // Check file size limit (5MB)
        if (file.ContentLength > MAX_FILE_SIZE)
            throw new ArgumentException("גודל הקובץ חורג מהמותר (5MB)");

        string connectionString = Connect.GetConnectionString();
        string uploadPath = HttpContext.Current.Server.MapPath(UPLOAD_FOLDER);
        
        // Create upload directory if it doesn't exist
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Generate unique filename using event ID and timestamp to prevent conflicts
        string originalFileName = Path.GetFileName(file.FileName);
        string fileExtension = Path.GetExtension(originalFileName);
        string uniqueFileName = string.Format("{0}_{1}_{2}", eventId, DateTime.Now.Ticks, originalFileName);
        string filePath = Path.Combine(uploadPath, uniqueFileName);

        // Save file to server
        file.SaveAs(filePath);

        // Get relative path for database storage (without ~/)
        string relativePath = UPLOAD_FOLDER.Replace("~/", "") + uniqueFileName;

        // Save to database
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            // Insert into Files table
            string sql = "INSERT INTO Files (file_name, file_path, file_type, uploaded_at, uploaded_by) VALUES (?, ?, ?, ?, ?)";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", originalFileName);
                cmd.Parameters.AddWithValue("?", relativePath);
                cmd.Parameters.AddWithValue("?", fileExtension);
                cmd.Parameters.AddWithValue("?", DateTime.Now);
                cmd.Parameters.AddWithValue("?", uploadedBy);
                cmd.ExecuteNonQuery();
            }

            // Get the inserted file ID using Access identity function
            sql = "SELECT @@IDENTITY";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                int fileId = Convert.ToInt32(result);

                // Link file to event in junction table
                sql = "INSERT INTO EventFiles (event_id, file_id) VALUES (?, ?)";
                using (OleDbCommand linkCmd = new OleDbCommand(sql, conn))
                {
                    linkCmd.Parameters.AddWithValue("?", eventId);
                    linkCmd.Parameters.AddWithValue("?", fileId);
                    linkCmd.ExecuteNonQuery();
                }

                LoggingService.Log("FileService", string.Format("File saved successfully - FileId: {0}, EventId: {1}, FileName: {2}", fileId, eventId, originalFileName));
                return fileId;
            }
        }
    }

    /// <summary>
    /// Gets all files for an event
    /// </summary>
    // Retrieves all files linked to a specific event, ordered by upload date (newest first)
    public DataTable GetFilesByEvent(int eventId)
    {
        string connectionString = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            // Join Files and EventFiles tables to get files for this event
            string sql = @"
                SELECT F.Id, F.file_name, F.file_path, F.file_type, F.uploaded_at, F.uploaded_by
                FROM Files F
                INNER JOIN EventFiles EF ON F.Id = EF.file_id
                WHERE EF.event_id = ?
                ORDER BY F.uploaded_at DESC";

            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", eventId);
                using (OleDbDataAdapter adapter = new OleDbDataAdapter(cmd))
                {
                    adapter.Fill(dt);
                }
            }
        }

        return dt;
    }

    /// <summary>
    /// Gets file by ID
    /// </summary>
    public DataRow GetFileById(int fileId)
    {
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string sql = "SELECT * FROM Files WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", fileId);
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Id", typeof(int));
                        dt.Columns.Add("file_name", typeof(string));
                        dt.Columns.Add("file_path", typeof(string));
                        dt.Columns.Add("file_type", typeof(string));
                        dt.Columns.Add("uploaded_at", typeof(DateTime));
                        dt.Columns.Add("uploaded_by", typeof(int));

                        DataRow row = dt.NewRow();
                        row["Id"] = dr["Id"];
                        row["file_name"] = dr["file_name"] != DBNull.Value ? dr["file_name"].ToString() : "";
                        row["file_path"] = dr["file_path"] != DBNull.Value ? dr["file_path"].ToString() : "";
                        row["file_type"] = dr["file_type"] != DBNull.Value ? dr["file_type"].ToString() : "";
                        row["uploaded_at"] = dr["uploaded_at"] != DBNull.Value ? dr["uploaded_at"] : DateTime.MinValue;
                        row["uploaded_by"] = dr["uploaded_by"] != DBNull.Value ? dr["uploaded_by"] : 0;
                        dt.Rows.Add(row);

                        return dt.Rows[0];
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Deletes file from server and database
    /// </summary>
    // Deletes file from server and database after checking user permissions (owner or file uploader only)
    public bool DeleteFile(int fileId, int userId, string userRole)
    {
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            // Get file info first to check permissions
            DataRow fileInfo = GetFileById(fileId);
            if (fileInfo == null)
                return false;

            // Check permissions - only owner role or the user who uploaded the file can delete
            int uploadedBy = Convert.ToInt32(fileInfo["uploaded_by"]);
            if (userRole != "owner" && uploadedBy != userId)
            {
                throw new UnauthorizedAccessException("אין לך הרשאה למחוק קובץ זה");
            }

            // Delete physical file from server
            string filePath = fileInfo["file_path"].ToString();
            if (!string.IsNullOrEmpty(filePath))
            {
                string fullPath = HttpContext.Current.Server.MapPath("~/" + filePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        // Log file deletion errors but continue with database cleanup
                        LoggingService.Log("FileService", "Error deleting physical file", ex);
                    }
                }
            }

            // Delete from EventFiles junction table first (foreign key constraint)
            string sql = "DELETE FROM EventFiles WHERE file_id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", fileId);
                cmd.ExecuteNonQuery();
            }

            // Delete from Files table
            sql = "DELETE FROM Files WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", fileId);
                cmd.ExecuteNonQuery();
            }

            LoggingService.Log("FileService", string.Format("File deleted successfully - FileId: {0}", fileId));
            return true;
        }
    }
}

