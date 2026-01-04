using System;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class exusers : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        if (Session["username"] == null)
        {
            Response.Redirect("login.aspx");
            return;
        }

        if (!IsPostBack)
            BindUsers();
    }

    private void BindUsers(string search = "")
    {
        var us = new UsersService();
        var ds = us.getallusers();

        if (ds == null || ds.Tables.Count == 0)
        {
            gvUsers.DataSource = null;
            gvUsers.DataBind();
            return;
        }

        DataTable t = ds.Tables[0];

        if (!string.IsNullOrWhiteSpace(search))
        {
            var view = new DataView(t);
            string q = search.Replace("'", "''");
            view.RowFilter =
                $"userName LIKE '%{q}%' OR email LIKE '%{q}%' OR firstName LIKE '%{q}%' OR lastName LIKE '%{q}%' OR phonenum LIKE '%{q}%' OR CityName LIKE '%{q}%'";

            gvUsers.DataSource = view;
        }
        else
        {
            gvUsers.DataSource = t;
        }

        gvUsers.DataBind();
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        BindUsers(txtSearchemail.Text);
    }

    protected void btnClear_Click(object sender, EventArgs e)
    {
        txtSearchemail.Text = "";
        BindUsers();
    }

    protected void gvUsers_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvUsers.PageIndex = e.NewPageIndex;
        BindUsers(txtSearchemail.Text);
    }

    protected void gvUsers_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            DataRowView drv = e.Row.DataItem as DataRowView;
            if (drv != null)
            {
                foreach (TableCell cell in e.Row.Cells)
                {
                    foreach (Control ctrl in cell.Controls)
                    {
                        if (ctrl is LiteralControl)
                        {
                            LiteralControl lit = ctrl as LiteralControl;
                            if (!string.IsNullOrWhiteSpace(lit.Text))
                            {
                                lit.Text = Connect.FixEncoding(lit.Text);
                            }
                        }
                        else if (ctrl is System.Web.UI.WebControls.Label)
                        {
                            System.Web.UI.WebControls.Label lbl = ctrl as System.Web.UI.WebControls.Label;
                            if (!string.IsNullOrWhiteSpace(lbl.Text))
                            {
                                lbl.Text = Connect.FixEncoding(lbl.Text);
                            }
                        }
                    }
                }
            }
        }
    }

    protected string GetAvatarLetter(object container)
    {
        DataRowView drv = null;
        
        if (container is GridViewRow row && row.DataItem is DataRowView)
        {
            drv = (DataRowView)row.DataItem;
        }
        else if (container is DataRowView)
        {
            drv = (DataRowView)container;
        }
        
        if (drv == null) return "?";

        string userNameCol = drv.DataView.Table.Columns.Contains("userName") ? "userName" : (drv.DataView.Table.Columns.Contains("UserName") ? "UserName" : "userName");
        string firstNameCol = drv.DataView.Table.Columns.Contains("firstName") ? "firstName" : (drv.DataView.Table.Columns.Contains("FirstName") ? "FirstName" : "firstName");
        
        string userName = drv.DataView.Table.Columns.Contains(userNameCol) && drv[userNameCol] != DBNull.Value && drv[userNameCol] != null 
            ? Connect.FixEncoding(Convert.ToString(drv[userNameCol])) : "";
        if (string.IsNullOrWhiteSpace(userName))
        {
            string firstName = drv.DataView.Table.Columns.Contains(firstNameCol) && drv[firstNameCol] != DBNull.Value && drv[firstNameCol] != null 
                ? Connect.FixEncoding(Convert.ToString(drv[firstNameCol])) : "";
            if (!string.IsNullOrWhiteSpace(firstName))
                return firstName.Substring(0, 1).ToUpper();
            return "?";
        }

        return userName.Substring(0, 1).ToUpper();
    }

    protected string GetCity(object container)
    {
        DataRowView drv = null;
        
        if (container is GridViewRow row && row.DataItem is DataRowView)
        {
            drv = (DataRowView)row.DataItem;
        }
        else if (container is DataRowView)
        {
            drv = (DataRowView)container;
        }
        
        if (drv == null) return string.Empty;

        var table = drv.DataView?.Table;
        if (table == null) return string.Empty;

        string[] names = { "CityName", "cityname", "citys.cityname", "c.cityname", "city" };

        foreach (var name in names)
        {
            if (table.Columns.Contains(name) && drv[name] != DBNull.Value && drv[name] != null)
                return Connect.FixEncoding(Convert.ToString(drv[name]));
        }
        return string.Empty;
    }

    protected bool IsOwner()
    {
        string role = Session["Role"]?.ToString() ?? "";
        return role.ToLower() == "owner";
    }

    protected void btnDeleteUser_Click(object sender, EventArgs e)
    {
        if (!IsOwner())
        {
            return;
        }

        System.Web.UI.WebControls.LinkButton btn = sender as System.Web.UI.WebControls.LinkButton;
        if (btn == null) return;

        string userIdStr = btn.CommandArgument;
        if (!int.TryParse(userIdStr, out int userId))
        {
            return;
        }

        UsersService us = new UsersService();
        bool deleted = us.DeleteUser(userId);

        if (deleted)
        {
            BindUsers(txtSearchemail.Text);
        }
    }

    protected void btnResetPassword_Click(object sender, EventArgs e)
    {
        if (!IsOwner())
        {
            return;
        }

        System.Web.UI.WebControls.LinkButton btn = sender as System.Web.UI.WebControls.LinkButton;
        if (btn == null) return;

        string userIdStr = btn.CommandArgument;
        if (!int.TryParse(userIdStr, out int userId))
        {
            return;
        }

        try
        {
            UsersService us = new UsersService();
            string defaultPassword = "123456";
            us.UpdatePassword(userId, defaultPassword);
            
            BindUsers(txtSearchemail.Text);
        }
        catch (Exception ex)
        {
        }
    }
}
