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

                <div style="margin: 24px 0; text-align: center; position: relative;">
                    <div style="position: relative; text-align: center;">
                        <span style="background: var(--surface); padding: 0 16px; color: var(--text); font-size: 14px; position: relative; z-index: 1;">או</span>
                        <div style="position: absolute; top: 50%; left: 0; right: 0; height: 1px; background: var(--border); z-index: 0;"></div>
                    </div>
                </div>

                <div class="form-field">
                    <asp:Button ID="btnGoogleLogin" runat="server" Text="התחבר עם Google" OnClick="btnGoogleLogin_Click" 
                        CssClass="google-button" />
                </div>

                <div class="auth-support">
                    <a href="forgotPassword.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;">שכחת סיסמה?</a>
                    <a href="login-with-code.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;">התחברות ללא סיסמה</a>
                    לא רשומים עדיין? <a href="register.aspx">צרו משתמש חדש</a>
                </div>
            </div>
        </div>
    </section>
</asp:Content>

