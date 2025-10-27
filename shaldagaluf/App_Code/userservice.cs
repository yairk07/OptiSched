using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;
/// <summary>
/// Summary description for UsersService
/// </summary>
public class UsersService
{
    static OleDbConnection myConnection;
    public UsersService()
    {
        string connectionString = Connect.GetConnectionString();
        myConnection = new OleDbConnection(connectionString);
    }

    public void insertIntoDB(string userName, string firstName, string lastName, string email, string password,
                int gender, int yearOfBirth, string userId, string phonenum, int city)
    {
        try
        {
            myConnection.Open();
            string sSql = "INSERT INTO Users (userName, firstName, lastName, email, [password], gender, yearOfBirth, userId, phonenum, city) " +
                          "VALUES('" + userName + "', '" + firstName + "', '" + lastName + "', '" + email + "', '" + password + "', " + gender + ", " + yearOfBirth + ", '" + userId + "', '" + phonenum + "', " + city + ")";
            OleDbCommand myCmd = new OleDbCommand(sSql, myConnection);
            myCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            myConnection.Close();
        } 
    }


    public DataSet getallusers()
    {
        DataSet data = new DataSet();
        try
        {
            myConnection.Open();
            string sSql = "SELECT * FROM Users ,citys where users.city=citys.id;";
            OleDbCommand myCmd = new OleDbCommand(sSql, myConnection);
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = myCmd;
            adapter.Fill(data);
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            myConnection.Close();
        }
        return data;
    }

    public bool IsExist(string userName, string password)
    {
        DataSet dataset = new DataSet();
        try
        {
            myConnection.Open();
            string sSql = "SELECT * FROM Users WHERE userName = '" + userName + "' AND [password] = '" + password + "'";
            OleDbCommand myCmd = new OleDbCommand(sSql, myConnection);
            OleDbDataAdapter adapter = new OleDbDataAdapter();
            adapter.SelectCommand = myCmd;
            adapter.Fill(dataset, "Users");
        }
        catch (Exception ex)
        {
            throw ex;
        }
        finally
        {
            myConnection.Close();
        }

        return dataset.Tables[0].Rows.Count > 0;
    }
}
