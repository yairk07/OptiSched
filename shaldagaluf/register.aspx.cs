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
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        lblMessage.Text = "";

        if (!IsPostBack)
        {
            BindCities();
        }
    }

    protected void btnGoogleRegister_Click(object sender, EventArgs e)
    {
        try
        {
            LoggingService.Log("GOOGLE_REGISTER_CLICK", "Google register button clicked");
            
            string clientId = System.Configuration.ConfigurationManager.AppSettings["GoogleOAuth:ClientId"];
            int clientIdLength = clientId != null ? clientId.Length : 0;
            LoggingService.Log("GOOGLE_REGISTER_CHECK_CLIENT_ID", string.Format("Checking ClientId - IsEmpty: {0}, Length: {1}", string.IsNullOrWhiteSpace(clientId), clientIdLength));
            
            if (string.IsNullOrWhiteSpace(clientId))
            {
                LoggingService.Log("GOOGLE_REGISTER_NO_CLIENT_ID", "ClientId is empty - cannot proceed with Google registration");
                lblMessage.Text = "Google OAuth לא מוגדר. כדי להפעיל הרשמה עם Google:<br/>1. היכנס ל-Google Cloud Console (https://console.cloud.google.com/)<br/>2. צור OAuth 2.0 Client ID<br/>3. הוסף את הערכים ב-Web.config תחת GoogleOAuth:ClientId ו-GoogleOAuth:ClientSecret<br/>4. הוסף Authorized redirect URI: " + Request.Url.Scheme + "://" + Request.Url.Authority + "/google-oauth-callback.aspx";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }
            
            string redirectUrl = GoogleOAuthService.GetAuthorizationUrl();
            int redirectUrlLength = redirectUrl != null ? redirectUrl.Length : 0;
            LoggingService.Log("GOOGLE_REGISTER_REDIRECT", string.Format("Redirecting to Google OAuth - URL length: {0}", redirectUrlLength));
            Response.Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_REGISTER_ERROR", string.Format("Error in Google registration: {0}, StackTrace: {1}", ex.Message, ex.StackTrace), ex);
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

        int gender, city, yearOfBirth;
        if (!int.TryParse(genderStr, out gender) ||
            !int.TryParse(cityStr, out city) ||
            !int.TryParse(yearofbirth, out yearOfBirth))
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
            City = city              // <-- תמיד ה-id מטבלת Citys
        };

        user.insertintodb();

        lblMessage.Text = "הרישום בוצע בהצלחה!";
        lblMessage.ForeColor = System.Drawing.Color.Green;
    }
}
