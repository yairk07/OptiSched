<%@ WebHandler Language="C#" Class="downloadFile" %>

using System;
using System.Web;
using System.IO;
using System.Data;
using System.Data.OleDb;

public class downloadFile : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        try
        {
            int fileId = 0;
            if (!int.TryParse(context.Request.QueryString["id"], out fileId))
            {
                context.Response.StatusCode = 400;
                context.Response.Write("Invalid file ID");
                return;
            }

            // Check if user is logged in
            if (context.Session["username"] == null)
            {
                context.Response.StatusCode = 401;
                context.Response.Write("Unauthorized");
                return;
            }

            FileService fileService = new FileService();
            DataRow fileInfo = fileService.GetFileById(fileId);
            
            if (fileInfo == null)
            {
                context.Response.StatusCode = 404;
                context.Response.Write("File not found");
                return;
            }

            // Check permissions - get event ID from EventFiles
            string connectionString = Connect.GetConnectionString();
            int eventId = 0;
            int eventUserId = 0;
            
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT EF.event_id, CE.UserId 
                    FROM EventFiles EF
                    INNER JOIN CalendarEvents CE ON EF.event_id = CE.Id
                    WHERE EF.file_id = ?";
                
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", fileId);
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
                            context.Response.Write("File not associated with any event");
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

            // Get file path
            string filePath = fileInfo["file_path"].ToString();
            string fileName = fileInfo["file_name"].ToString();
            
            string fullPath = context.Server.MapPath("~/" + filePath);
            
            if (!File.Exists(fullPath))
            {
                context.Response.StatusCode = 404;
                context.Response.Write("File not found on server");
                return;
            }

            // Send file
            context.Response.ContentType = "application/octet-stream";
            context.Response.AddHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", fileName));
            context.Response.TransmitFile(fullPath);
        }
        catch (Exception ex)
        {
            LoggingService.Log("downloadFile", "Error downloading file", ex);
            context.Response.StatusCode = 500;
            context.Response.Write("Error downloading file");
        }
    }

    public bool IsReusable
    {
        get { return false; }
    }
}

