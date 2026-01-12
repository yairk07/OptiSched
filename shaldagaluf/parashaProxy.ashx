<%@ WebHandler Language="C#" Class="ParashaProxy" %>

using System;
using System.Web;
using System.Web.Script.Serialization;

public class ParashaProxy : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        DateTime date = DateTime.Today;
        string dateParam = context.Request["date"];
        DateTime parsed;
        if (!string.IsNullOrEmpty(dateParam) && DateTime.TryParse(dateParam, out parsed))
            date = parsed;

        try
        {
            var info = home.FetchHebrewInfo(date);
            var serializer = new JavaScriptSerializer();
            var payload = new
            {
                date = date.ToString("yyyy-MM-dd"),
                hebrewDate = info != null ? (info.HebrewDate ?? string.Empty) : string.Empty,
                parasha = info != null ? (info.Parasha ?? string.Empty) : string.Empty,
                holiday = info != null ? (info.Holiday ?? string.Empty) : string.Empty,
                converterUrl = info != null ? (info.ConverterUrl ?? string.Empty) : string.Empty,
                eventsUrl = info != null ? (info.EventsUrl ?? string.Empty) : string.Empty
            };

            context.Response.Write(serializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            context.Response.Write("{\"error\":\"" + HttpUtility.JavaScriptStringEncode(ex.Message) + "\"}");
        }
    }

    public bool IsReusable
    {
        get { return false; }
    }
}

