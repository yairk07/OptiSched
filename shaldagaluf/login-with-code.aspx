<%@ Page Title="התחברות עם קוד" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="login-with-code.aspx.cs" Inherits="login_with_code" Culture="he-IL" UICulture="he-IL" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
    <meta charset="utf-8" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="auth-section">
        <div class="auth-card">
            <div class="auth-info">
                <span class="hero-eyebrow">Passwordless Login</span>
                <h2>התחברות ללא סיסמה</h2>
                <p>קבל קוד התחברות באימייל שלך</p>
            </div>

            <div class="auth-form">
                <h3>התחברות עם קוד</h3>
                <p class="auth-support">הזן את כתובת האימייל שלך ונשלח לך קוד התחברות</p>

                <asp:Panel ID="pnlRequestCode" runat="server" DefaultButton="btnSendCode">
                    <div class="form-field">
                        <label for="txtEmail">כתובת אימייל</label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="textbox" placeholder="your.email@example.com" />
                    </div>

                    <asp:Button ID="btnSendCode" runat="server" Text="שלח קוד" OnClick="btnSendCode_Click" CssClass="button" />

                    <asp:Label ID="lblMessage" runat="server" CssClass="auth-error" />
                </asp:Panel>

                <asp:Panel ID="pnlVerifyCode" runat="server" Visible="false" DefaultButton="btnVerifyCode">
                    <div class="form-field">
                        <label for="txtCode">קוד אימות</label>
                        <asp:TextBox ID="txtCode" runat="server" CssClass="textbox verification-code-input" placeholder="000000" MaxLength="6" style="text-align: center; font-size: 24px; letter-spacing: 8px;" />
                        <p style="font-size: 13px; color: var(--muted); margin-top: 8px; text-align: center;">הזן את הקוד בן 6 הספרות שנשלח לאימייל שלך</p>
                    </div>

                    <asp:Button ID="btnVerifyCode" runat="server" Text="אמת קוד" OnClick="btnVerifyCode_Click" CssClass="button" />

                    <asp:Label ID="lblCodeMessage" runat="server" CssClass="auth-error" />

                    <div style="text-align: center; margin-top: 20px;">
                        <asp:LinkButton ID="lnkResendCode" runat="server" OnClick="lnkResendCode_Click" CssClass="auth-link" Text="שלח קוד חדש"></asp:LinkButton>
                    </div>
                </asp:Panel>

                <div class="auth-support" style="margin-top: 24px;">
                    <a href="login.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;">חזור להתחברות רגילה</a>
                    עדיין לא רשום? <a href="register.aspx">הירשם עכשיו</a>
                </div>
            </div>
        </div>
    </section>

    <style>
        .verification-code-input { font-family: monospace; }
        .auth-link { color: var(--brand); text-decoration: none; font-weight: 600; }
        .auth-link:hover { text-decoration: underline; }
    </style>
</asp:Content>