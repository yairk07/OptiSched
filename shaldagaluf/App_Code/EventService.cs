using System;
using System.Data;
using System.Data.OleDb;

public class EventService
{
    public DataTable GetAllEvents()
    {
        string conStr = Connect.GetConnectionString();
        DataTable dt = new DataTable();

        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();

            string sql = @"
SELECT
    C.Id            AS Id,
    C.Userid        AS UserId,
    U.username      AS UserName,
    C.title         AS Title,
    C.[date]        AS EventDate,
    C.[time]        AS EventTime,
    C.notes         AS Notes
FROM calnder AS C
LEFT JOIN users AS U
    ON C.Userid = U.id
ORDER BY C.[date] DESC, C.[time] DESC";

            OleDbDataAdapter da = new OleDbDataAdapter(sql, con);
            da.Fill(dt);
        }

        return dt;
    }
}
