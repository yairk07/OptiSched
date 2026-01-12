<%@ WebHandler Language="C#" Class="showImage" %>

using System;
using System.Web;
using System.IO;
using System.Data;
using System.Data.OleDb;

public class showImage : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        try
        {
            int imageId = 0;
            if (!int.TryParse(context.Request.QueryString["id"], out imageId))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid image ID");
                return;
            }

            // Check if user is logged in
            if (context.Session["username"] == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Write("Unauthorized");
                return;
            }

            ImageService imageService = new ImageService();
            DataRow imageInfo = imageService.GetImageById(imageId);
            
            if (imageInfo == null)
            {
                context.Response.StatusCode = 404;
                context.Response.Write("Image not found");
                return;
            }

            // Check permissions - get event ID from EventImages
            string connectionString = Connect.GetConnectionString();
            int eventId = 0;
            int eventUserId = 0;
            
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT EI.event_id, CE.UserId 
                    FROM EventImages EI
                    INNER JOIN CalendarEvents CE ON EI.event_id = CE.Id
                    WHERE EI.image_id = ?";
                
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", imageId);
                    using (OleDbDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            eventId = Convert.ToInt32(dr["event_id"]);
                            eventUserId = Convert.ToInt32(dr["UserId"]);
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            context.Response.Write("Image not associated with any event");
                            return;
                        }
                    }
                }
            }

            // Check permissions
            int currentUserId = Convert.ToInt32(context.Session["userId"]);
            string role = context.Session["Role"] != null ? context.Session["Role"].ToString() : "user";
            
            if (role != "owner" && eventUserId != currentUserId)
            {
                context.Response.StatusCode = 403;
                context.Response.Write("Access denied");
                return;
            }

            // Get image path
            string imagePath = imageInfo["image_path"].ToString();
            string fullPath = context.Server.MapPath("~/" + imagePath);
            
            if (!File.Exists(fullPath))
            {
                context.Response.StatusCode = 404;
                context.Response.Write("Image not found on server");
                return;
            }

            // Determine content type
            string extension = Path.GetExtension(fullPath).ToLower();
            string contentType = "image/jpeg";
            if (extension == ".png")
                contentType = "image/png";
            else if (extension == ".gif")
                contentType = "image/gif";

            // Send image
            context.Response.ContentType = contentType;
            context.Response.TransmitFile(fullPath);
        }
        catch (Exception ex)
        {
            LoggingService.Log("showImage", "Error showing image", ex);
            context.Response.StatusCode = 500;
            context.Response.Write("Error showing image");
        }
    }

    public bool IsReusable
    {
        get { return false; }
    }
}

