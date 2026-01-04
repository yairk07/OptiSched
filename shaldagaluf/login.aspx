<%@ Page Title="Login" Language="C#" MasterPageFile="~/danimaster.master" CodeFile="login.aspx.cs"AutoEventWireup="true"Inherits="login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="auth-section">
        <div class="auth-card">
            <div class="auth-info">
                <span class="hero-eyebrow">Productivity Hub</span>
                <h2>ברוך הבא למרכז הניהול</h2>
                <p>
                    התחבר כדי לצפות בלוחות הזמנים, לעדכן משימות ולהגיב בזמן אמת.
                    המערכת מותאמת לכל מכשיר ומאפשרת מקסום יעילות יומיומי.
                </p>

                <div class="auth-highlights">
                    <div class="auth-highlight">
                        <span>01</span>
                        עדכוני אירועים בזמן אמת
                    </div>
                    <div class="auth-highlight">
                        <span>02</span>
                        תכנון משימות חכם
                    </div>
                    <div class="auth-highlight">
                        <span>03</span>
                        התאמה לצוותים שונים
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
                    <a href="forgotPassword.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;">שכחת סיסמה?</a>
                    לא רשומים עדיין? <a href="register.aspx">צרו משתמש חדש</a>
                </div>
            </div>
        </div>
    </section>
</asp:Content>

