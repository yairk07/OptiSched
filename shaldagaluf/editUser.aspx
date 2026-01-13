<%@ Page Title="&#x05E2;&#x05E8;&#x05D9;&#x05DB;&#x05EA; &#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9;" Language="C#" 
    AutoEventWireup="true" CodeFile="editUser.aspx.cs" Inherits="editUser"
    ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<!DOCTYPE html>
<html lang="he" dir="rtl">
<head runat="server">
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>&#x05E2;&#x05E8;&#x05D9;&#x05DB;&#x05EA; &#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9; - OptiSched</title>
    <link href="StyleSheet.css" rel="stylesheet" />
    <style>
        body {
            direction: rtl;
            font-family: 'Segoe UI', 'Heebo', sans-serif;
        }

        .edit-user-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .edit-user-container {
            max-width: 700px;
            margin: 0 auto;
        }

        .edit-user-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .edit-user-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .edit-user-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .edit-user-form {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .form-group {
            margin-bottom: 24px;
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
        }

        .form-label {
            display: block;
            font-weight: 600;
            color: var(--heading);
            margin-bottom: 8px;
            font-size: 15px;
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

        .form-actions {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            margin-top: 32px;
            padding-top: 24px;
            border-top: 1px solid var(--border);
        }

        .btn-save {
            padding: 12px 28px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            cursor: pointer;
            transition: background .2s ease;
        }

        .btn-save:hover {
            background: var(--brand-dark);
        }

        .btn-cancel {
            padding: 12px 28px;
            background: var(--surface);
            color: var(--text);
            border: 1px solid var(--border);
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            text-decoration: none;
            transition: background .2s ease, border-color .2s ease;
            display: inline-block;
        }

        .btn-cancel:hover {
            background: rgba(229, 9, 20, 0.05);
            border-color: var(--brand);
            text-decoration: none;
        }

        .form-message {
            margin-top: 16px;
            font-size: 14px;
        }

        .form-message.success {
            color: #29ad5c;
        }

        .form-message.error {
            color: #e50914;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .form-actions {
                flex-direction: column;
            }

            .btn-save,
            .btn-cancel {
                width: 100%;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="edit-user-shell">
            <div class="edit-user-container">
                <div class="edit-user-header">
                    <h1 class="edit-user-title">&#x05E2;&#x05E8;&#x05D9;&#x05DB;&#x05EA; &#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9;</h1>
                    <p class="edit-user-subtitle">&#x05E2;&#x05D3;&#x05DB;&#x05DF; &#x05D0;&#x05EA; &#x05E4;&#x05E8;&#x05D8;&#x05D9; &#x05D4;&#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9; &#x05D5;&#x05E9;&#x05DE;&#x05D5;&#x05E8; &#x05D0;&#x05EA; &#x05D4;&#x05E9;&#x05D9;&#x05E0;&#x05D5;&#x05D9;&#x05DD;</p>
                </div>

                <asp:Panel ID="pnlForm" runat="server">
                    <div class="edit-user-form">
                        <div class="form-group">
                            <label class="form-label">&#x05E9;&#x05DD; &#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9;</label>
                            <asp:TextBox ID="txtUserName" runat="server" CssClass="form-input" />
                        </div>

                        <div class="form-row">
                            <div class="form-group">
                                <label class="form-label">&#x05E9;&#x05DD; &#x05E4;&#x05E8;&#x05D8;&#x05D9;</label>
                                <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-input" />
                            </div>

                            <div class="form-group">
                                <label class="form-label">&#x05E9;&#x05DD; &#x05DE;&#x05E9;&#x05E4;&#x05D7;&#x05D4;</label>
                                <asp:TextBox ID="txtLastName" runat="server" CssClass="form-input" />
                            </div>
                        </div>

                        <div class="form-group">
                            <label class="form-label">&#x05D0;&#x05D9;&#x05DE;&#x05D9;&#x05D9;&#x05DC;</label>
                            <asp:TextBox ID="txtEmail" runat="server" CssClass="form-input" TextMode="Email" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">&#x05D8;&#x05DC;&#x05E4;&#x05D5;&#x05DF;</label>
                            <asp:TextBox ID="txtPhone" runat="server" CssClass="form-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">&#x05E2;&#x05D9;&#x05E8;</label>
                            <asp:TextBox ID="txtCity" runat="server" CssClass="form-input" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">&#x05E8;&#x05DE;&#x05EA; &#x05D2;&#x05D9;&#x05E9;&#x05D4;</label>
                            <asp:DropDownList ID="ddlRole" runat="server" CssClass="form-input">
                                <asp:ListItem Text="&#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9;" Value="user" />
                                <asp:ListItem Text="&#x05DE;&#x05E0;&#x05D4;&#x05DC;" Value="owner" />
                            </asp:DropDownList>
                        </div>

                        <asp:Label ID="lblMessage" runat="server" CssClass="form-message" Visible="false" />

                        <div class="form-actions">
                            <asp:Button ID="btnSave" runat="server" Text="&#x05E9;&#x05DE;&#x05D5;&#x05E8; &#x05E9;&#x05D9;&#x05E0;&#x05D5;&#x05D9;&#x05D9;&#x05DD;" CssClass="btn-save" OnClick="btnSave_Click" />
                            <a href="exusers.aspx" class="btn-cancel">&#x05D1;&#x05D9;&#x05D8;&#x05D5;&#x05DC;</a>
                        </div>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlNotFound" runat="server" Visible="false">
                    <div class="edit-user-form">
                        <p>&#x05D4;&#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9; &#x05D4;&#x05DE;&#x05D1;&#x05D5;&#x05E7;&#x05E9; &#x05DC;&#x05D0; &#x05E0;&#x05DE;&#x05E6;&#x05D0;.</p>
                        <a href="exusers.aspx" class="btn-cancel">&#x05D7;&#x05D6;&#x05E8;&#x05D4; &#x05DC;&#x05E8;&#x05E9;&#x05D9;&#x05DE;&#x05EA; &#x05D4;&#x05DE;&#x05E9;&#x05EA;&#x05DE;&#x05E9;&#x05D9;&#x05DD;</a>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </form>
</body>
</html>


