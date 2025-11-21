<%@ WebHandler Language="C#" Class="ZmanimProxy" %>

using System;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;

public class ZmanimProxy : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        try
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers[HttpRequestHeader.Accept] = "application/json";
                client.Encoding = System.Text.Encoding.UTF8;

                string body = string.Empty;
                if (string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) && context.Request.ContentLength > 0)
                {
                    using (var reader = new System.IO.StreamReader(context.Request.InputStream))
                        body = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    double latitude = ParseDouble(context.Request["latitude"], 31.7683);
                    double longitude = ParseDouble(context.Request["longitude"], -35.2137);
                    string date = string.IsNullOrWhiteSpace(context.Request["date"])
                        ? DateTime.Today.ToString("yyyy-MM-dd")
                        : context.Request["date"];

                    var serializer = new JavaScriptSerializer();
                    body = serializer.Serialize(new
                    {
                        latitude,
                        longitude,
                        date
                    });
                }

                var response = client.UploadString("https://times.rabaz.co.il/api/zmanim", "POST", body);
                context.Response.Write(response);
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.Write("{\"error\":\"" + HttpUtility.JavaScriptStringEncode(ex.Message) + "\"}");
        }
    }

    public bool IsReusable => false;

    private static double ParseDouble(string value, double fallback)
    {
        if (double.TryParse(value, out double result))
            return result;
        return fallback;
    }
}

