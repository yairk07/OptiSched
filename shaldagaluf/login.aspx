<%@ Page Title="Login" Language="C#" MasterPageFile="~/danimaster.master" CodeFile="login.aspx.cs"AutoEventWireup="true"Inherits="login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="auth-section">
        <div class="auth-card">
            <div class="auth-info">
                <span class="hero-eyebrow">Secure Access</span>
                <h2>ברוך הבא למרכז השליטה</h2>
                <p>
                    התחבר כדי לצפות ביומן היחידה, לעדכן משימות ולהגיב בזמן אמת.
                    המערכת מותאמת לכל מכשיר ומאובטחת ברמה צבאית.
                </p>

                <div class="auth-highlights">
                    <div class="auth-highlight">
                        <span>01</span>
                        עדכוני אירועים חיים
                    </div>
                    <div class="auth-highlight">
                        <span>02</span>
                        ניהול משימות אדום-שחור
                    </div>
                    <div class="auth-highlight">
                        <span>03</span>
                        גישה בהתאם להרשאות
                    </div>
                </div>
            </div>

            <div class="auth-form">
                <h3>התחברות למערכת</h3>
                <p class="auth-support">הזן את פרטי המשתמש שסופקו לך על ידי מנהל המערכת</p>

                <div class="form-field">
                    <label for="txtUserName">שם משתמש</label>
                    <asp:TextBox ID="txtUserName" runat="server" CssClass="textbox" placeholder="לדוגמה: yair.k" />
                </div>

                <div class="form-field">
                    <label for="txtPassword">סיסמה</label>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="textbox" placeholder="••••••••" />
                </div>

                <asp:Button ID="btnLogin" runat="server" Text="כניסה למערכת" OnClick="btnLogin_Click" CssClass="button" />

                <asp:Label ID="lblError" runat="server" CssClass="auth-error" />

                <div class="auth-support">
                    לא רשומים עדיין? <a href="register.aspx">צרו משתמש חדש</a>
                </div>
            </div>
        </div>
    </section>
</asp:Content>

