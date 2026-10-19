using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class exusers : System.Web.UI.Page
{
    private static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9_\.\-]{3,30}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new Regex(@"^0(5\d|[2-489]|7[2-9])-?\d{7}$", RegexOptions.Compiled);

    // טעינת הדף והפעלת לוגיקה ראשונית
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
        {
            pEditHint.Visible = IsOwner();
            BindUsers();
        }
    }

    // הצגת הודעה מעל טבלת המשתמשים
    private void ShowGridMessage(string text, bool ok)
    {
        lblGridMessage.Visible = true;
        lblGridMessage.Text = text;
        lblGridMessage.CssClass = ok ? "grid-message ok" : "grid-message err";
    }

    // טעינה וקישור של טבלת המשתמשים
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

        DataTable t = ds.Tables[0].Copy();
        EnsureGridColumnAliases(t);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var view = new DataView(t);
            string q = search.Replace("'", "''");
            view.RowFilter =
                string.Format("userName LIKE '%{0}%' OR email LIKE '%{0}%' OR firstName LIKE '%{0}%' OR lastName LIKE '%{0}%' OR phonenum LIKE '%{0}%' OR CityName LIKE '%{0}%'", q);

            gvUsers.DataSource = view;
        }
        else
        {
            gvUsers.DataSource = t;
        }

        gvUsers.DataBind();
    }

    // יצירת כינויי עמודות לתאימות הגריד
    private static void EnsureGridColumnAliases(DataTable t)
    {
        AddAlias(t, "userName", "UserName", "username");
        AddAlias(t, "firstName", "FirstName", "firstname");
        AddAlias(t, "lastName", "LastName", "lastname");
        AddAlias(t, "email", "Email");
        AddAlias(t, "phonenum", "PhoneNumber", "phone");
        AddAlias(t, "id", "Id");
    }

    // הוספת עמודת כינוי ל-DataTable
    private static void AddAlias(DataTable t, string preferred, params string[] alternates)
    {
        if (t.Columns.Contains(preferred))
            return;

        foreach (string alt in alternates)
        {
            if (!t.Columns.Contains(alt))
                continue;

            t.Columns.Add(preferred, t.Columns[alt].DataType);
            foreach (DataRow row in t.Rows)
                row[preferred] = row[alt];
            return;
        }
    }

    // חיפוש / ניקוי סינון ברשימה
    protected void btnSearch_Click(object sender, EventArgs e)
    {
        gvUsers.EditIndex = -1;
        BindUsers(txtSearchemail.Text);
    }

    // חיפוש / ניקוי סינון ברשימה
    protected void btnClear_Click(object sender, EventArgs e)
    {
        gvUsers.EditIndex = -1;
        txtSearchemail.Text = "";
        BindUsers();
    }

    // מעבר עמוד בגריד
    protected void gvUsers_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvUsers.EditIndex = -1;
        gvUsers.PageIndex = e.NewPageIndex;
        BindUsers(txtSearchemail.Text);
    }

    // מעבר לשורת עריכה בגריד
    protected void gvUsers_RowEditing(object sender, GridViewEditEventArgs e)
    {
        if (!IsOwner())
        {
            e.Cancel = true;
            ShowGridMessage("רק בעלים יכול לערוך משתמשים.", false);
            return;
        }

        gvUsers.EditIndex = e.NewEditIndex;
        BindUsers(txtSearchemail.Text);
    }

    // ביטול עריכת שורה בגריד
    protected void gvUsers_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        gvUsers.EditIndex = -1;
        lblGridMessage.Visible = false;
        BindUsers(txtSearchemail.Text);
    }

    // שמירת עדכון שורה בגריד
    protected void gvUsers_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (!IsOwner())
        {
            e.Cancel = true;
            ShowGridMessage("רק בעלים יכול לערוך משתמשים.", false);
            return;
        }

        int userId;
        if (!int.TryParse(Convert.ToString(gvUsers.DataKeys[e.RowIndex].Value), out userId))
        {
            ShowGridMessage("מזהה משתמש לא תקין.", false);
            return;
        }

        GridViewRow row = gvUsers.Rows[e.RowIndex];
        TextBox txtUserName = row.FindControl("txtEditUserName") as TextBox;
        TextBox txtFirstName = row.FindControl("txtEditFirstName") as TextBox;
        TextBox txtLastName = row.FindControl("txtEditLastName") as TextBox;
        TextBox txtEmail = row.FindControl("txtEditEmail") as TextBox;
        TextBox txtPhone = row.FindControl("txtEditPhone") as TextBox;

        string userName = txtUserName != null ? txtUserName.Text.Trim() : "";
        string firstName = txtFirstName != null ? txtFirstName.Text.Trim() : "";
        string lastName = txtLastName != null ? txtLastName.Text.Trim() : "";
        string email = txtEmail != null ? txtEmail.Text.Trim() : "";
        string phone = txtPhone != null ? txtPhone.Text.Trim().Replace(" ", "").Replace("-", "") : "";

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(firstName) ||
            string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
        {
            ShowGridMessage("יש למלא שם משתמש, שם פרטי, שם משפחה, אימייל וטלפון.", false);
            return;
        }

        if (!UsernameRegex.IsMatch(userName))
        {
            ShowGridMessage("שם משתמש חייב להכיל 3–30 תווים באנגלית, ספרות, נקודה, קו תחתון או מקף.", false);
            return;
        }

        if (firstName.Length < 2 || lastName.Length < 2)
        {
            ShowGridMessage("שם פרטי ושם משפחה חייבים להכיל לפחות 2 תווים.", false);
            return;
        }

        if (!EmailRegex.IsMatch(email))
        {
            ShowGridMessage("כתובת אימייל לא תקינה.", false);
            return;
        }

        if (!PhoneRegex.IsMatch(phone))
        {
            ShowGridMessage("מספר טלפון לא תקין. לדוגמה: 0501234567.", false);
            return;
        }

        UsersService us = new UsersService();
        if (us.UsernameExistsForOtherUser(userName, userId))
        {
            ShowGridMessage("שם המשתמש כבר קיים אצל משתמש אחר.", false);
            return;
        }

        DataRow existingEmail = us.GetUserByEmail(email);
        if (existingEmail != null)
        {
            string idCol = existingEmail.Table.Columns.Contains("id") ? "id" : "Id";
            int existingId;
            if (int.TryParse(Convert.ToString(existingEmail[idCol]), out existingId) && existingId != userId)
            {
                ShowGridMessage("כתובת האימייל כבר רשומה למשתמש אחר.", false);
                return;
            }
        }

        try
        {
            us.UpdateUserProfile(userId, userName, firstName, lastName, email, phone);
            gvUsers.EditIndex = -1;
            ShowGridMessage("הפרטים עודכנו בהצלחה.", true);
            BindUsers(txtSearchemail.Text);
        }
        catch (Exception ex)
        {
            LoggingService.Log("EXUSERS", "Error updating user from grid", ex);
            ShowGridMessage("שגיאה בעדכון המשתמש: " + ex.Message, false);
        }
    }

    // עיצוב שורה בעת קישור נתונים לגריד
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

    // החזרת אות ראשונה לאווטאר המשתמש
    protected string GetAvatarLetter(object container)
    {
        DataRowView drv = null;
        
        GridViewRow row = container as GridViewRow;
        if (row != null && row.DataItem is DataRowView)
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

    // החזרת שם עיר משורת הנתונים
    protected string GetCity(object container)
    {
        DataRowView drv = null;
        
        GridViewRow row = container as GridViewRow;
        if (row != null && row.DataItem is DataRowView)
        {
            drv = (DataRowView)row.DataItem;
        }
        else if (container is DataRowView)
        {
            drv = (DataRowView)container;
        }
        
        if (drv == null) return string.Empty;

        var table = drv.DataView != null ? drv.DataView.Table : null;
        if (table == null) return string.Empty;

        string[] names = { "CityName", "cityname", "citys.cityname", "c.cityname", "city" };

        foreach (var name in names)
        {
            if (table.Columns.Contains(name) && drv[name] != DBNull.Value && drv[name] != null)
                return Connect.FixEncoding(Convert.ToString(drv[name]));
        }
        return string.Empty;
    }

    // בדיקה אם המשתמש המחובר הוא בעלים (owner)
    protected bool IsOwner()
    {
        string role = Session["Role"] != null ? Session["Role"].ToString() : "";
        return role.ToLower() == "owner";
    }

    // מחיקת הפריט שנבחר
    protected void btnDeleteUser_Click(object sender, EventArgs e)
    {
        if (!IsOwner())
        {
            return;
        }

        System.Web.UI.WebControls.LinkButton btn = sender as System.Web.UI.WebControls.LinkButton;
        if (btn == null) return;

        string userIdStr = btn.CommandArgument;
        int userId;
        if (!int.TryParse(userIdStr, out userId))
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

    // איפוס סיסמת משתמש
    protected void btnResetPassword_Click(object sender, EventArgs e)
    {
        if (!IsOwner())
        {
            return;
        }

        System.Web.UI.WebControls.LinkButton btn = sender as System.Web.UI.WebControls.LinkButton;
        if (btn == null) return;

        string userIdStr = btn.CommandArgument;
        int userId;
        if (!int.TryParse(userIdStr, out userId))
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

