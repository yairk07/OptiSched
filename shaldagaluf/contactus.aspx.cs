using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class contactus : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.ContentType = "text/html; charset=utf-8";
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        Response.HeaderEncoding = System.Text.Encoding.UTF8;
    }

    protected void btnSubmit_Click(object sender, EventArgs e)
    {
        try
        {
            string firstName = txtFirstName.Text.Trim();
            string lastName = txtLastName.Text.Trim();
            string fullName = firstName + " " + lastName;
            string email = txtEmail.Text.Trim();
            string subject = txtSubject.Text.Trim();
            string message = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                lblMessage.Text = "אנא הזן שם פרטי או שם משפחה";
                lblMessage.CssClass = "form-message";
                lblMessage.Style["color"] = "#f44336";
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                lblMessage.Text = "אנא הזן אימייל תקין";
                lblMessage.CssClass = "form-message";
                lblMessage.Style["color"] = "#f44336";
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                lblMessage.Text = "אנא הזן תוכן הודעה";
                lblMessage.CssClass = "form-message";
                lblMessage.Style["color"] = "#f44336";
                return;
            }

            ContactService contactService = new ContactService();
            contactService.SaveContactMessage(fullName, email, subject, message);

            lblMessage.Text = "הודעתך נשלחה בהצלחה! נחזור אליך בהקדם.";
            lblMessage.CssClass = "form-message";
            lblMessage.Style["color"] = "#4caf50";

            // Clear form
            txtFirstName.Text = "";
            txtLastName.Text = "";
            txtEmail.Text = "";
            txtSubject.Text = "";
            txtMessage.Text = "";
            if (DropDownList1 != null)
                DropDownList1.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            LoggingService.Log("contactus", "Error saving contact message", ex);
            lblMessage.Text = "אירעה שגיאה בשליחת ההודעה. אנא נסה שוב מאוחר יותר.";
            lblMessage.CssClass = "form-message";
            lblMessage.Style["color"] = "#f44336";
        }
    }
}