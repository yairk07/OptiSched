<%@ Page Title="התחברות עם קוד" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="login-with-code.aspx.cs" Inherits="login_with_code" Culture="he-IL" UICulture="he-IL" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
    <meta charset="utf-8" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="auth-section">
        <div class="auth-card">
            <div class="auth-info">
                <span class="hero-eyebrow">Passwordless Login</span>
                <h2 runat="server" id="h2Title"></h2>
                <p runat="server" id="pDescription"></p>
            </div>

            <div class="auth-form">
                <h3 runat="server" id="h3Title"></h3>
                <p class="auth-support" runat="server" id="pSupport"></p>

                <asp:Panel ID="pnlRequestCode" runat="server">
                    <div class="form-field">
                        <label for="txtEmail" runat="server" id="lblEmail"></label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="textbox" placeholder="your.email@example.com" />
                    </div>

                    <asp:Button ID="btnSendCode" runat="server" OnClick="btnSendCode_Click" CssClass="button" />

                    <asp:Label ID="lblMessage" runat="server" CssClass="auth-error" />
                </asp:Panel>

                <asp:Panel ID="pnlVerifyCode" runat="server" Visible="false">
                    <div class="form-field">
                        <label for="txtCode" runat="server" id="lblCode"></label>
                        <asp:TextBox ID="txtCode" runat="server" CssClass="textbox verification-code-input" placeholder="000000" MaxLength="6" style="text-align: center; font-size: 24px; letter-spacing: 8px;" />
                        <p style="font-size: 13px; color: var(--muted); margin-top: 8px; text-align: center;" runat="server" id="pCodeInfo"></p>
                    </div>

                    <asp:Button ID="btnVerifyCode" runat="server" OnClick="btnVerifyCode_Click" CssClass="button" />

                    <asp:Label ID="lblCodeMessage" runat="server" CssClass="auth-error" />

                    <div style="text-align: center; margin-top: 20px;">
                        <asp:LinkButton ID="lnkResendCode" runat="server" OnClick="lnkResendCode_Click" CssClass="auth-link"></asp:LinkButton>
                    </div>
                </asp:Panel>

                <div class="auth-support" style="margin-top: 24px;">
                    <a href="login.aspx" style="display: block; margin-bottom: 12px; color: var(--brand); text-decoration: none;" runat="server" id="lnkBack"></a>
                    <span runat="server" id="spanNotRegistered"></span> <a href="register.aspx" runat="server" id="lnkRegister"></a>
                </div>
            </div>
        </div>
    </section>

    <style>
        .verification-code-input {
            font-family: monospace;
        }

        .auth-link {
            color: var(--brand);
            text-decoration: none;
            font-weight: 600;
        }

        .auth-link:hover {
            text-decoration: underline;
        }

        .required {
            color: var(--brand);
        }
    </style>
</asp:Content>
