using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class register : System.Web.UI.Page
{
    private static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9_\.\-]{3,30}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new Regex(@"^0(5\d|[2-489]|7[2-9])-?\d{7}$", RegexOptions.Compiled);
    private static readonly Regex IdRegex = new Regex(@"^\d{5,9}$", RegexOptions.Compiled);
    private const string DemoIndexSessionKey = "RegisterDemoSampleIndex";

    private class DemoSample
    {
        public string Username;
        public string FirstName;
        public string LastName;
        public string Email;
        public string Password;
        public string Phone;
        public string IdNumber;
        public string Gender;
        public int YearOfBirth;
        public int CityItemIndex; // 0-based among non-empty ddl items
    }

    // 20 valid samples: Israeli ID checksums, phones, usernames, emails; years 1990–2000
    private static readonly DemoSample[] DemoSamples = new DemoSample[]
    {
        new DemoSample { Username = "demo_avi01", FirstName = "אבי", LastName = "כהן", Email = "demo.avi01@example.com", Password = "Demo123!", Phone = "0501234567", IdNumber = "123456782", Gender = "1", YearOfBirth = 1995, CityItemIndex = 0 },
        new DemoSample { Username = "demo_noa02", FirstName = "נועה", LastName = "לוי", Email = "demo.noa02@example.com", Password = "Demo123!", Phone = "0522345678", IdNumber = "234567899", Gender = "2", YearOfBirth = 1998, CityItemIndex = 1 },
        new DemoSample { Username = "demo_dan03", FirstName = "דן", LastName = "מזרחי", Email = "demo.dan03@example.com", Password = "Demo123!", Phone = "0533456789", IdNumber = "345678908", Gender = "1", YearOfBirth = 1992, CityItemIndex = 2 },
        new DemoSample { Username = "demo_yael04", FirstName = "יעל", LastName = "שמש", Email = "demo.yael04@example.com", Password = "Demo123!", Phone = "0544567890", IdNumber = "456789015", Gender = "2", YearOfBirth = 1997, CityItemIndex = 3 },
        new DemoSample { Username = "demo_omri05", FirstName = "עומרי", LastName = "ברק", Email = "demo.omri05@example.com", Password = "Demo123!", Phone = "0555678901", IdNumber = "567890124", Gender = "1", YearOfBirth = 1994, CityItemIndex = 4 },
        new DemoSample { Username = "demo_maya06", FirstName = "מאיה", LastName = "גבע", Email = "demo.maya06@example.com", Password = "Demo123!", Phone = "0506789012", IdNumber = "678901232", Gender = "2", YearOfBirth = 1999, CityItemIndex = 5 },
        new DemoSample { Username = "demo_roi07", FirstName = "רועי", LastName = "אלון", Email = "demo.roi07@example.com", Password = "Demo123!", Phone = "0527890123", IdNumber = "789012341", Gender = "1", YearOfBirth = 1991, CityItemIndex = 6 },
        new DemoSample { Username = "demo_tamar08", FirstName = "תמר", LastName = "דוד", Email = "demo.tamar08@example.com", Password = "Demo123!", Phone = "0538901234", IdNumber = "890123458", Gender = "2", YearOfBirth = 1996, CityItemIndex = 7 },
        new DemoSample { Username = "demo_ido09", FirstName = "עידו", LastName = "פרץ", Email = "demo.ido09@example.com", Password = "Demo123!", Phone = "0549012345", IdNumber = "901234567", Gender = "1", YearOfBirth = 1993, CityItemIndex = 0 },
        new DemoSample { Username = "demo_lior10", FirstName = "ליאור", LastName = "אברהם", Email = "demo.lior10@example.com", Password = "Demo123!", Phone = "0550123456", IdNumber = "112233440", Gender = "3", YearOfBirth = 2000, CityItemIndex = 1 },
        new DemoSample { Username = "demo_gal11", FirstName = "גל", LastName = "רוזן", Email = "demo.gal11@example.com", Password = "Demo123!", Phone = "0501112233", IdNumber = "223344557", Gender = "1", YearOfBirth = 1990, CityItemIndex = 2 },
        new DemoSample { Username = "demo_shira12", FirstName = "שירה", LastName = "נחמיאס", Email = "demo.shira12@example.com", Password = "Demo123!", Phone = "0522223344", IdNumber = "334455664", Gender = "2", YearOfBirth = 1995, CityItemIndex = 3 },
        new DemoSample { Username = "demo_oren13", FirstName = "אורן", LastName = "סגל", Email = "demo.oren13@example.com", Password = "Demo123!", Phone = "0533334455", IdNumber = "445566771", Gender = "1", YearOfBirth = 1998, CityItemIndex = 4 },
        new DemoSample { Username = "demo_hila14", FirstName = "הילה", LastName = "ביטון", Email = "demo.hila14@example.com", Password = "Demo123!", Phone = "0544445566", IdNumber = "556677888", Gender = "2", YearOfBirth = 1994, CityItemIndex = 5 },
        new DemoSample { Username = "demo_amit15", FirstName = "עמית", LastName = "חדד", Email = "demo.amit15@example.com", Password = "Demo123!", Phone = "0555556677", IdNumber = "667788996", Gender = "1", YearOfBirth = 1997, CityItemIndex = 6 },
        new DemoSample { Username = "demo_ravit16", FirstName = "רווית", LastName = "מלכה", Email = "demo.ravit16@example.com", Password = "Demo123!", Phone = "0506667788", IdNumber = "778899005", Gender = "2", YearOfBirth = 1992, CityItemIndex = 7 },
        new DemoSample { Username = "demo_yonatan17", FirstName = "יונתן", LastName = "שפיר", Email = "demo.yonatan17@example.com", Password = "Demo123!", Phone = "0527778899", IdNumber = "889900114", Gender = "1", YearOfBirth = 1996, CityItemIndex = 0 },
        new DemoSample { Username = "demo_naama18", FirstName = "נעמה", LastName = "עמר", Email = "demo.naama18@example.com", Password = "Demo123!", Phone = "0538889900", IdNumber = "990011223", Gender = "2", YearOfBirth = 1999, CityItemIndex = 1 },
        new DemoSample { Username = "demo_bar19", FirstName = "בר", LastName = "יוסף", Email = "demo.bar19@example.com", Password = "Demo123!", Phone = "0549990011", IdNumber = "101112134", Gender = "3", YearOfBirth = 1991, CityItemIndex = 2 },
        new DemoSample { Username = "demo_eden20", FirstName = "עדן", LastName = "גולן", Email = "demo.eden20@example.com", Password = "Demo123!", Phone = "0550001122", IdNumber = "121314157", Gender = "2", YearOfBirth = 2000, CityItemIndex = 3 }
    };

    // טעינת הדף והפעלת לוגיקה ראשונית
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
        
        lblMessage.Text = "";

        int minYear = DateTime.Now.Year - 120;
        int maxYear = DateTime.Now.Year - 10;
        rvYear.MinimumValue = minYear.ToString();
        rvYear.MaximumValue = maxYear.ToString();
        rvYear.ErrorMessage = "שנת לידה חייבת להיות בין " + minYear + " ל-" + maxYear;

        if (!IsPostBack)
        {
            BindCities();
        }
    }

    // מילוי טופס במשתמש לדוגמה (מתחלף בין כ-20)
    protected void btnFillDemo_Click(object sender, EventArgs e)
    {
        if (ddlOptions.Items.Count <= 1)
            BindCities();

        int index = 0;
        if (Session[DemoIndexSessionKey] != null)
            int.TryParse(Convert.ToString(Session[DemoIndexSessionKey]), out index);

        DemoSample sample = DemoSamples[index % DemoSamples.Length];
        Session[DemoIndexSessionKey] = (index + 1) % DemoSamples.Length;

        txtUsername.Text = sample.Username;
        txtFirstName.Text = sample.FirstName;
        txtLastName.Text = sample.LastName;
        txtEmail.Text = sample.Email;
        txtPhone.Text = sample.Phone;
        txtID.Text = sample.IdNumber;
        txtYearOfBirth.Text = sample.YearOfBirth.ToString();

        // Password TextBoxes clear value on render unless Attributes["value"] is set
        txtPassword.Attributes["value"] = sample.Password;
        txtConfirmPassword.Attributes["value"] = sample.Password;

        if (rblGender.Items.FindByValue(sample.Gender) != null)
            rblGender.SelectedValue = sample.Gender;

        SelectDemoCity(sample.CityItemIndex);

        lblMessage.Text = "מולא משתמש לדוגמה " + ((index % DemoSamples.Length) + 1) + " מתוך " + DemoSamples.Length
            + " (" + sample.Username + "). לחץ שוב לדוגמה הבאה.";
        lblMessage.ForeColor = System.Drawing.Color.Green;
        lblMessage.CssClass = "form-message ok";
    }

    // פונקציה: SelectDemoCity
    private void SelectDemoCity(int preferredIndex)
    {
        System.Collections.Generic.List<ListItem> cities = new System.Collections.Generic.List<ListItem>();
        foreach (ListItem item in ddlOptions.Items)
        {
            if (!string.IsNullOrEmpty(item.Value))
                cities.Add(item);
        }

        if (cities.Count == 0)
            return;

        ListItem chosen = cities[preferredIndex % cities.Count];
        ddlOptions.ClearSelection();
        ListItem match = ddlOptions.Items.FindByValue(chosen.Value);
        if (match != null)
            match.Selected = true;

        string cityNameJs = (chosen.Text ?? "").Replace("\\", "\\\\").Replace("'", "\\'");
        string cityIdJs = (chosen.Value ?? "").Replace("\\", "\\\\").Replace("'", "\\'");
        string script = "setTimeout(function(){"
            + "if(typeof loadAllCities==='function'){loadAllCities();}"
            + "var search=document.getElementById('citySearchInput');"
            + "var ddl=document.getElementById('" + ddlOptions.ClientID + "');"
            + "if(ddl){ddl.value='" + cityIdJs + "';}"
            + "if(search){search.value='" + cityNameJs + "';}"
            + "if(typeof selectCity==='function'){selectCity('" + cityIdJs + "','" + cityNameJs + "');}"
            + "},0);";
        ClientScript.RegisterStartupScript(GetType(), "FillDemoCity", script, true);
    }

    // הצגת הודעת שגיאה למשתמש
    private void ShowError(string message)
    {
        lblMessage.Text = message;
        lblMessage.ForeColor = System.Drawing.Color.Red;
        lblMessage.CssClass = "form-message err";
    }

    // טיפול בלחיצת הרשמה
    protected void btnGoogleRegister_Click(object sender, EventArgs e)
    {
        try
        {
            if (!GoogleOAuthService.IsConfigured())
            {
                lblMessage.Text = "Google OAuth לא מוגדר. כדי להפעיל הרשמה עם Google:<br/>"
                    + "1. היכנס ל-Google Cloud Console<br/>"
                    + "2. צור OAuth 2.0 Client ID<br/>"
                    + "3. עדכן ב-Web.config את GoogleOAuth:ClientId ו-GoogleOAuth:ClientSecret<br/>"
                    + "4. הוסף Authorized redirect URI: <b>" + GoogleOAuthService.GetConfiguredRedirectUri() + "</b>";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                return;
            }

            string redirectUrl = GoogleOAuthService.GetAuthorizationUrl();
            Response.Redirect(redirectUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            LoggingService.Log("GOOGLE_REGISTER_ERROR", "Error in Google registration: " + ex.Message, ex);
            lblMessage.Text = "שגיאה בהרשמה עם Google: " + ex.Message;
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    // טעינת רשימת ערים ל-DropDown
    private void BindCities()
    {
        CityService cityService = new CityService();
        DataTable dt = cityService.GetAllCities();

        ddlOptions.Items.Clear();
        ddlOptions.Items.Add(new ListItem("בחר עיר", ""));
        ddlOptions.AppendDataBoundItems = true;
        ddlOptions.DataSource = dt;
        ddlOptions.DataTextField = "cityname";
        ddlOptions.DataValueField = "id";
        ddlOptions.DataBind();
        ddlOptions.ClearSelection();
        ddlOptions.SelectedIndex = 0;
    }

    // טיפול בלחיצת הרשמה
    protected void btnRegister_Click(object sender, EventArgs e)
    {
        Page.Validate("Register");
        if (!Page.IsValid)
        {
            ShowError("יש לתקן את השגיאות בטופס לפני ההרשמה.");
            return;
        }

        string username = txtUsername.Text.Trim();
        string firstName = txtFirstName.Text.Trim();
        string lastName = txtLastName.Text.Trim();
        string email = txtEmail.Text.Trim();
        string password = txtPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;
        string phone = txtPhone.Text.Trim().Replace(" ", "").Replace("-", "");
        string id = txtID.Text.Trim().Replace(" ", "").Replace("-", "");
        string genderStr = rblGender.SelectedValue;
        string cityStr = ddlOptions.SelectedValue;
        string yearofbirth = txtYearOfBirth.Text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(firstName) ||
            string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) ||
            string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(id) ||
            string.IsNullOrEmpty(genderStr) || string.IsNullOrEmpty(yearofbirth) ||
            string.IsNullOrEmpty(cityStr))
        {
            ShowError("אנא מלא את כל השדות ובחר עיר.");
            return;
        }

        if (!UsernameRegex.IsMatch(username))
        {
            ShowError("שם משתמש חייב להכיל 3–30 תווים באנגלית, ספרות, נקודה, קו תחתון או מקף.");
            return;
        }

        if (firstName.Length < 2 || lastName.Length < 2)
        {
            ShowError("שם פרטי ושם משפחה חייבים להכיל לפחות 2 תווים.");
            return;
        }

        if (!EmailRegex.IsMatch(email))
        {
            ShowError("אנא הכנס כתובת אימייל תקינה.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("הסיסמה חייבת להכיל לפחות 6 תווים.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("הסיסמה ואימות הסיסמה אינם תואמים.");
            return;
        }

        if (!PhoneRegex.IsMatch(phone))
        {
            ShowError("מספר טלפון לא תקין. לדוגמה: 0501234567 או 03-1234567.");
            return;
        }

        if (!IdRegex.IsMatch(id) || !IsValidIsraeliId(id))
        {
            ShowError("תעודת זהות לא תקינה. יש להזין מספר בן 5–9 ספרות עם ספרת ביקורת נכונה.");
            return;
        }

        int gender, city, yearOfBirth;
        if (!int.TryParse(genderStr, out gender) ||
            !int.TryParse(cityStr, out city) ||
            !int.TryParse(yearofbirth, out yearOfBirth))
        {
            ShowError("וודא שמין, עיר ושנת לידה הם מספרים תקינים.");
            return;
        }

        int minYear = DateTime.Now.Year - 120;
        int maxYear = DateTime.Now.Year - 10;
        if (yearOfBirth < minYear || yearOfBirth > maxYear)
        {
            ShowError("שנת לידה לא תקינה. יש להזין שנה בין " + minYear + " ל-" + maxYear + ".");
            return;
        }

        UsersService usersService = new UsersService();
        if (usersService.UsernameExists(username))
        {
            ShowError("שם המשתמש כבר קיים במערכת. אנא בחר שם משתמש אחר.");
            return;
        }

        DataRow existingEmail = usersService.GetUserByEmail(email);
        if (existingEmail != null)
        {
            ShowError("כתובת האימייל כבר רשומה במערכת.");
            return;
        }

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

        lblMessage.Text = "הרישום בוצע בהצלחה!";
        lblMessage.ForeColor = System.Drawing.Color.Green;
        lblMessage.CssClass = "form-message ok";
    }

    // בדיקת תקינות תעודת זהות ישראלית
    private static bool IsValidIsraeliId(string id)
    {
        if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\d{5,9}$"))
            return false;

        id = id.PadLeft(9, '0');
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            int digit = id[i] - '0';
            int step = digit * ((i % 2) + 1);
            if (step > 9)
                step -= 9;
            sum += step;
        }
        return sum % 10 == 0;
    }
}

