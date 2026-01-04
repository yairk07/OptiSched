<%@ Page Title="עריכת משתמש" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="editUser.aspx.cs" Inherits="editUser" ResponseEncoding="utf-8" 
    ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <section class="edit-user-shell">
        <div class="edit-user-container">
            <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="exusers.aspx" CssClass="back-link">
                <i class="fas fa-arrow-right"></i> חזרה לרשימת המשתמשים
            </asp:HyperLink>

            <div class="edit-user-card">
                <div class="card-header">
                    <h2 class="card-title"><i class="fas fa-user-edit"></i> עריכת משתמש</h2>
                </div>

                <asp:Panel ID="pnlNotFound" runat="server" Visible="false">
                    <div class="error-message">
                        <i class="fas fa-exclamation-circle"></i>
                        <p>המשתמש לא נמצא</p>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlContent" runat="server" Visible="false">
                    <div class="form-container">
                        <asp:Label ID="lblMessage" runat="server" CssClass="message-label" Visible="false"></asp:Label>

                        <div class="form-row">
                            <div class="form-group">
                                <label for="txtUserName" class="form-label">
                                    <i class="fas fa-user"></i> שם משתמש
                                </label>
                                <asp:TextBox ID="txtUserName" runat="server" CssClass="form-input" />
                            </div>

                            <div class="form-group">
                                <label for="txtFirstName" class="form-label">
                                    <i class="fas fa-user"></i> שם פרטי
                                </label>
                                <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-input" />
                            </div>
                        </div>

                        <div class="form-row">
                            <div class="form-group">
                                <label for="txtLastName" class="form-label">
                                    <i class="fas fa-user"></i> שם משפחה
                                </label>
                                <asp:TextBox ID="txtLastName" runat="server" CssClass="form-input" />
                            </div>

                            <div class="form-group">
                                <label for="txtEmail" class="form-label">
                                    <i class="fas fa-envelope"></i> אימייל
                                </label>
                                <asp:TextBox ID="txtEmail" runat="server" CssClass="form-input" TextMode="Email" />
                            </div>
                        </div>

                        <div class="form-row">
                            <div class="form-group">
                                <label for="txtPhone" class="form-label">
                                    <i class="fas fa-phone"></i> טלפון
                                </label>
                                <asp:TextBox ID="txtPhone" runat="server" CssClass="form-input" />
                            </div>

                            <div class="form-group">
                                <label for="ddlCity" class="form-label">
                                    <i class="fas fa-map-marker-alt"></i> עיר
                                </label>
                                <asp:DropDownList ID="ddlCity" runat="server" CssClass="form-input" />
                            </div>
                        </div>

                        <asp:Panel ID="pnlRole" runat="server">
                            <div class="form-row">
                                <div class="form-group">
                                    <label for="ddlRole" class="form-label">
                                        <i class="fas fa-shield-alt"></i> רמת גישה
                                    </label>
                                    <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-input">
                                        <asp:ListItem Value="user" Text="משתמש" />
                                        <asp:ListItem Value="admin" Text="אדמין" />
                                        <asp:ListItem Value="owner" Text="בעל אתר" />
                                    </asp:DropDownList>
                                </div>
                            </div>
                        </asp:Panel>

                        <div class="form-actions">
                            <asp:Button ID="btnSave" runat="server" Text="שמור שינויים" OnClick="btnSave_Click" CssClass="btn-save" />
                            <asp:Button ID="btnCancel" runat="server" Text="ביטול" OnClick="btnCancel_Click" CssClass="btn-cancel" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </section>

    <style>
        .edit-user-shell {
            width: min(900px, 95%);
            margin: 30px auto 60px;
            padding: 0 20px;
        }

        .edit-user-container {
            max-width: 800px;
            margin: 0 auto;
        }

        .back-link {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            margin-bottom: 24px;
            color: var(--brand);
            text-decoration: none;
            font-weight: 600;
            transition: color .2s ease;
        }

        .back-link:hover {
            color: var(--brand-dark);
            text-decoration: none;
        }

        .edit-user-card {
            background: var(--surface);
            border-radius: 16px;
            padding: 32px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .card-header {
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 2px solid var(--border);
        }

        .card-title {
            font-size: 28px;
            font-weight: 700;
            color: var(--heading);
            margin: 0;
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .card-title i {
            color: var(--brand);
        }

        .form-container {
            display: flex;
            flex-direction: column;
            gap: 24px;
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }

        .form-group {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .form-label {
            font-weight: 600;
            color: var(--heading);
            font-size: 14px;
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .form-label i {
            color: var(--brand);
            font-size: 14px;
        }

        .form-input {
            padding: 12px 16px;
            border: 2px solid var(--border);
            border-radius: 8px;
            font-size: 15px;
            direction: rtl;
            background: var(--surface);
            color: var(--text);
            transition: border-color 0.3s ease;
        }

        .form-input:focus {
            outline: none;
            border-color: var(--brand);
        }

        .form-actions {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            margin-top: 20px;
            padding-top: 24px;
            border-top: 2px solid var(--border);
        }

        .btn-save {
            padding: 12px 32px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            transition: background .2s ease;
            font-size: 15px;
        }

        .btn-save:hover {
            background: var(--brand-dark);
        }

        .btn-cancel {
            padding: 12px 32px;
            background: var(--surface);
            color: var(--text);
            border: 2px solid var(--border);
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            transition: all .2s ease;
            font-size: 15px;
            text-decoration: none;
        }

        .btn-cancel:hover {
            background: rgba(0,0,0,0.05);
            border-color: var(--text);
        }

        .message-label {
            padding: 12px 16px;
            border-radius: 8px;
            margin-bottom: 20px;
            font-weight: 600;
            display: block;
        }

        .message-label.success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }

        .message-label.error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }

        .error-message {
            text-align: center;
            padding: 40px 20px;
            color: var(--text);
        }

        .error-message i {
            font-size: 48px;
            color: #dc3545;
            margin-bottom: 16px;
            display: block;
        }

        .error-message p {
            font-size: 18px;
            margin: 0;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .form-actions {
                flex-direction: column;
            }

            .btn-save, .btn-cancel {
                width: 100%;
            }
        }
    </style>

</asp:Content>
