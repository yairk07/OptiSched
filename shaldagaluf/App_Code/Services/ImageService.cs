using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Web;

public class ImageService
{
    private const int MAX_IMAGE_SIZE = 5 * 1024 * 1024; // 5MB
    private const string UPLOAD_FOLDER = "~/App_Data/Uploads/Images/";
    private static readonly string[] ALLOWED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".gif" };

    /// <summary>
    /// Saves uploaded image to server and creates database record
    /// </summary>
    public int SaveImage(HttpPostedFile image, int eventId, int uploadedBy)
    {
        if (image == null || image.ContentLength == 0)
            throw new ArgumentException("תמונה לא תקינה");

        if (image.ContentLength > MAX_IMAGE_SIZE)
            throw new ArgumentException("גודל התמונה חורג מהמותר (5MB)");

        string fileExtension = Path.GetExtension(image.FileName).ToLower();
        if (Array.IndexOf(ALLOWED_EXTENSIONS, fileExtension) == -1)
            throw new ArgumentException("סוג קובץ לא מותר. מותר: JPG, JPEG, PNG, GIF");

        string connectionString = Connect.GetConnectionString();
        string uploadPath = HttpContext.Current.Server.MapPath(UPLOAD_FOLDER);
        
        // Create upload directory if it doesn't exist
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        // Generate unique filename
        string originalFileName = Path.GetFileName(image.FileName);
        string uniqueFileName = string.Format("{0}_{1}_{2}", eventId, DateTime.Now.Ticks, originalFileName);
        string imagePath = Path.Combine(uploadPath, uniqueFileName);

        // Save image to server
        image.SaveAs(imagePath);

        // Get relative path for database
        string relativePath = UPLOAD_FOLDER.Replace("~/", "") + uniqueFileName;

        // Save to database
        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            // Insert into Images table
            string sql = "INSERT INTO Images (image_name, image_path, uploaded_at, uploaded_by) VALUES (?, ?, ?, ?)";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", originalFileName);
                cmd.Parameters.AddWithValue("?", relativePath);
                cmd.Parameters.AddWithValue("?", DateTime.Now);
                cmd.Parameters.AddWithValue("?", uploadedBy);
                cmd.ExecuteNonQuery();
            }

            // Get the inserted image ID
            sql = "SELECT @@IDENTITY";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                int imageId = Convert.ToInt32(result);

                // Link image to event
                sql = "INSERT INTO EventImages (event_id, image_id) VALUES (?, ?)";
                using (OleDbCommand linkCmd = new OleDbCommand(sql, conn))
                {
                    linkCmd.Parameters.AddWithValue("?", eventId);
                    linkCmd.Parameters.AddWithValue("?", imageId);
                    linkCmd.ExecuteNonQuery();
                }

                LoggingService.Log("ImageService", string.Format("Image saved successfully - ImageId: {0}, EventId: {1}, ImageName: {2}", imageId, eventId, originalFileName));
                return imageId;
            }
        }
    }

    /// <summary>
    /// Gets all images for an event
    /// </summary>
    public DataTable GetImagesByEvent(int eventId)
    {
        string connectionString = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string sql = @"
                SELECT I.Id, I.image_name, I.image_path, I.uploaded_at, I.uploaded_by
                FROM Images I
                INNER JOIN EventImages EI ON I.Id = EI.image_id
                WHERE EI.event_id = ?
                ORDER BY I.uploaded_at DESC";

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
    /// Gets image by ID
    /// </summary>
    public DataRow GetImageById(int imageId)
    {
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            string sql = "SELECT * FROM Images WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", imageId);
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        DataTable dt = new DataTable();
                        dt.Columns.Add("Id", typeof(int));
                        dt.Columns.Add("image_name", typeof(string));
                        dt.Columns.Add("image_path", typeof(string));
                        dt.Columns.Add("uploaded_at", typeof(DateTime));
                        dt.Columns.Add("uploaded_by", typeof(int));

                        DataRow row = dt.NewRow();
                        row["Id"] = dr["Id"];
                        row["image_name"] = dr["image_name"] != DBNull.Value ? dr["image_name"].ToString() : "";
                        row["image_path"] = dr["image_path"] != DBNull.Value ? dr["image_path"].ToString() : "";
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
    /// Deletes image from server and database
    /// </summary>
    public bool DeleteImage(int imageId, int userId, string userRole)
    {
        string connectionString = Connect.GetConnectionString();

        using (OleDbConnection conn = new OleDbConnection(connectionString))
        {
            conn.Open();

            // Get image info first
            DataRow imageInfo = GetImageById(imageId);
            if (imageInfo == null)
                return false;

            // Check permissions - only owner or admin can delete
            int uploadedBy = Convert.ToInt32(imageInfo["uploaded_by"]);
            if (userRole != "owner" && uploadedBy != userId)
            {
                throw new UnauthorizedAccessException("אין לך הרשאה למחוק תמונה זו");
            }

            // Delete physical file
            string imagePath = imageInfo["image_path"].ToString();
            if (!string.IsNullOrEmpty(imagePath))
            {
                string fullPath = HttpContext.Current.Server.MapPath("~/" + imagePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Log("ImageService", "Error deleting physical image", ex);
                    }
                }
            }

            // Delete from EventImages junction table
            string sql = "DELETE FROM EventImages WHERE image_id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", imageId);
                cmd.ExecuteNonQuery();
            }

            // Delete from Images table
            sql = "DELETE FROM Images WHERE Id = ?";
            using (OleDbCommand cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", imageId);
                cmd.ExecuteNonQuery();
            }

            LoggingService.Log("ImageService", string.Format("Image deleted successfully - ImageId: {0}", imageId));
            return true;
        }
    }
}

