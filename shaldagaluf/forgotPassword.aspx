<%@ Page Title="שחזור סיסמה" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="forgotPassword.aspx.cs" Inherits="forgotPassword" ResponseEncoding="utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="forgot-password-shell">
        <div class="forgot-password-container">
            <div class="forgot-password-header">
                <h2 class="forgot-password-title" runat="server" id="h2Title"></h2>
                <p class="forgot-password-subtitle" runat="server" id="pSubtitle"></p>
            </div>

            <div class="forgot-password-form-container">
                <asp:Panel ID="pnlRequest" runat="server">
                    <div class="form-group">
                        <label class="form-label" runat="server" id="lblEmail"></label>
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-input" placeholder="your.email@example.com"></asp:TextBox>
                    </div>

                    <asp:Label ID="lblMessage" runat="server" CssClass="form-message"></asp:Label>

                    <div class="form-actions">
                        <asp:Button ID="btnSendReset" runat="server" OnClick="btnSendReset_Click" CssClass="btn-submit" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlCode" runat="server" Visible="false">
                    <div class="form-group">
                        <label class="form-label" runat="server" id="lblCode"></label>
                        <asp:TextBox ID="txtVerificationCode" runat="server" CssClass="form-input verification-code-input" placeholder="000000" MaxLength="6" style="text-align: center; font-size: 24px; letter-spacing: 8px;"></asp:TextBox>
                        <p style="font-size: 13px; color: var(--muted); margin-top: 8px; text-align: center;" runat="server" id="pCodeInfo"></p>
                    </div>

                    <asp:Label ID="lblCodeMessage" runat="server" CssClass="form-message"></asp:Label>

                    <div class="form-actions">
                        <asp:Button ID="btnVerifyCode" runat="server" OnClick="btnVerifyCode_Click" CssClass="btn-submit" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlReset" runat="server" Visible="false">
                    <div class="form-group">
                        <label class="form-label" runat="server" id="lblNewPassword"></label>
                        <asp:TextBox ID="txtNewPassword" runat="server" TextMode="Password" CssClass="form-input" placeholder="Enter new password"></asp:TextBox>
                    </div>

                    <div class="form-group">
                        <label class="form-label" runat="server" id="lblConfirmPassword"></label>
                        <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-input" placeholder="Confirm password"></asp:TextBox>
                    </div>

                    <asp:Label ID="lblResetMessage" runat="server" CssClass="form-message"></asp:Label>

                    <div class="form-actions">
                        <asp:Button ID="btnResetPassword" runat="server" OnClick="btnResetPassword_Click" CssClass="btn-submit" />
                    </div>
                </asp:Panel>

                <div class="forgot-password-footer">
                    <p runat="server" id="pFooter"></p>
                </div>
            </div>
        </div>
    </section>

    <style>
        .forgot-password-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .forgot-password-container {
            max-width: 500px;
            margin: 0 auto;
        }

        .forgot-password-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .forgot-password-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .forgot-password-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .forgot-password-form-container {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .form-group {
            margin-bottom: 24px;
        }

        .form-label {
            display: block;
            font-weight: 600;
            color: var(--heading);
            margin-bottom: 8px;
            font-size: 15px;
        }

        .required {
            color: var(--brand);
        }

        .form-input {
            width: 100%;
            padding: 12px 16px;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 15px;
            direction: rtl;
            background: var(--bg);
            color: var(--text);
            transition: border-color .2s ease, box-shadow .2s ease;
            box-sizing: border-box;
        }

        .form-input:focus {
            outline: none;
            border-color: var(--brand);
            box-shadow: 0 0 0 3px rgba(229, 9, 20, 0.1);
        }

        .form-message {
            display: block;
            padding: 12px;
            border-radius: 8px;
            margin-bottom: 24px;
            text-align: center;
            font-weight: 600;
            min-height: 24px;
        }

        .form-actions {
            margin-top: 32px;
            padding-top: 24px;
            border-top: 1px solid var(--border);
        }

        .btn-submit {
            width: 100%;
            padding: 14px 28px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 17px;
            cursor: pointer;
            transition: background .2s ease, transform .15s ease;
            box-shadow: 0 18px 35px rgba(229, 9, 20, 0.35);
        }

        .btn-submit:hover {
            background: var(--brand-dark);
            transform: translateY(-1px);
        }

        .btn-submit:active {
            transform: translateY(1px);
        }

        .forgot-password-footer {
            margin-top: 24px;
            text-align: center;
            padding-top: 24px;
            border-top: 1px solid var(--border);
        }

        .forgot-password-footer p {
            color: var(--text);
            opacity: 0.8;
            margin: 0;
        }

        .forgot-password-footer a {
            color: var(--brand);
            font-weight: 600;
            text-decoration: none;
        }

        .forgot-password-footer a:hover {
            text-decoration: underline;
        }

        .verification-code-input {
            font-family: 'Courier New', monospace;
            font-weight: 600;
        }
    </style>
</asp:Content>

