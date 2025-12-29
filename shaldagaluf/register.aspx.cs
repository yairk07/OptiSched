using System;
using System.Data;
using System.Data.OleDb;
using System.Web.UI;

public partial class register : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        
        lblMessage.Text = "";

        if (!IsPostBack)
        {
            BindCities();
        }
    }

    protected void btnGoogleSignup_Click(object sender, EventArgs e)
    {
        try
        {
            string authUrl = GoogleOAuthService.GetAuthorizationUrl();
            Response.Redirect(authUrl);
        }
        catch (Exception ex)
        {
            lblMessage.Text = "שגיאה בהרשמה עם Google: " + ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    private void BindCities()
    {
        CityService cityService = new CityService();
        DataTable dt = cityService.GetAllCities();

        ddlOptions.DataSource = dt;
        ddlOptions.DataTextField = "cityname";
        ddlOptions.DataValueField = "id";
        ddlOptions.DataBind();
    }

    protected void btnRegister_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string firstName = txtFirstName.Text.Trim();
        string lastName = txtLastName.Text.Trim();
        string email = txtEmail.Text.Trim();
        string password = txtPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;
        string phone = txtPhone.Text.Trim();
        string id = txtID.Text.Trim();
        string genderStr = rblGender.SelectedValue;
        string cityStr = ddlOptions.SelectedValue;
        string yearofbirth = txtYearOfBirth.Text;

        // בדיקות בסיס
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(firstName) ||
            string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) ||
            string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(id) ||
            string.IsNullOrEmpty(genderStr) || string.IsNullOrEmpty(yearofbirth) ||
            string.IsNullOrEmpty(cityStr))
        {
            lblMessage.Text = "אנא מלא את כל השדות ובחר עיר.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (password != confirmPassword)
        {
            lblMessage.Text = "הסיסמה ואימות הסיסמה אינם תואמים.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            lblMessage.Text = "אנא הכנס כתובת אימייל תקינה";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        UsersService us = new UsersService();
        DataRow existingUser = us.GetUserByEmail(email);
        if (existingUser != null)
        {
            lblMessage.Text = "כתובת האימייל כבר קיימת במערכת. אנא השתמש באימייל אחר או התחבר לחשבון הקיים.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        // המרות מספריות מאובטחות
        if (!int.TryParse(genderStr, out int gender) ||
            !int.TryParse(cityStr, out int city) ||
            !int.TryParse(yearofbirth, out int yearOfBirth))
        {
            lblMessage.Text = "וודא שמין, עיר ושנת לידה הם מספרים תקינים.";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        // יצירת משתמש ושמירה
        User user = new User
        {
            Username = username,
            Firstname = firstName,
            Lastname = lastName,
            Email = email,
            Password = password,
            Gender = gender,
            YearOfBirth = yearOfBirth,
            UserId = id,
            PhoneNum = phone,
            City = city
        };

        user.insertintodb();

        try
        {
            EmailService.SendRegistrationEmail(email, firstName);
        }
        catch
        {
        }

        lblMessage.Text = "הרישום בוצע בהצלחה! נשלח לך אימייל אישור.";
        lblMessage.ForeColor = System.Drawing.Color.Green;
    }
}
