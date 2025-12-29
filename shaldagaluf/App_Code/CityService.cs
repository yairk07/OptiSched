using System;
using System.Data;
using System.Data.OleDb;

public class CityService
{
    public DataTable GetAllCities()
    {
        DataTable dt = new DataTable();
        string conStr = Connect.GetConnectionString();

        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            string sql = "SELECT id, cityname FROM Citys WHERE id Is Not Null ORDER BY cityname";
            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
        }

        return dt;
    }

    public DataTable SearchCities(string searchTerm)
    {
        DataTable dt = new DataTable();
        string conStr = Connect.GetConnectionString();

        using (OleDbConnection con = new OleDbConnection(conStr))
        {
            con.Open();
            string sql = "SELECT id, cityname FROM Citys WHERE id Is Not Null AND cityname LIKE ? ORDER BY cityname";
            using (OleDbCommand cmd = new OleDbCommand(sql, con))
            {
                OleDbParameter searchParam = new OleDbParameter("?", OleDbType.WChar);
                searchParam.Value = "%" + (searchTerm ?? "").Trim() + "%";
                cmd.Parameters.Add(searchParam);
                using (OleDbDataAdapter da = new OleDbDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
        }

        return dt;
    }
}



