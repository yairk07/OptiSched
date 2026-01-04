<%@ Page Title="ניהול בקשות גישה" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="calendarAccessRequests.aspx.cs" Inherits="calendarAccessRequests" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="access-requests-shell">
        <div class="access-requests-container">
            <div class="access-requests-header">
                <h2 class="access-requests-title">ניהול בקשות גישה</h2>
                <p class="access-requests-subtitle">אשר או דחה בקשות גישה לטבלאות משותפות</p>
            </div>

            <asp:Label ID="lblMessage" runat="server" CssClass="form-message"></asp:Label>

            <div class="requests-grid">
                <asp:Label ID="lblNoRequests" runat="server" Visible="true" CssClass="no-requests-message" Text="אין בקשות גישה ממתינות." />
                <asp:Repeater ID="rptRequests" runat="server" OnItemCommand="rptRequests_ItemCommand">
                    <ItemTemplate>
                        <div class="request-card">
                            <div class="request-card-header">
                                <h3 class="request-calendar-name"><%# Eval("CalendarName") ?? "ללא שם" %></h3>
                            </div>
                            <div class="request-card-body">
                                <div class="request-meta">
                                    <span class="meta-item">מבקש: <%# Eval("RequesterName") ?? "ללא שם" %></span>
                                    <span class="meta-item">אימייל: <%# Eval("RequesterEmail") ?? "" %></span>
                                    <span class="meta-item">תאריך בקשה: <%# Eval("RequestDate") != DBNull.Value ? Convert.ToDateTime(Eval("RequestDate")).ToString("dd/MM/yyyy HH:mm") : "" %></span>
                                </div>
                                <%# !string.IsNullOrEmpty(Eval("Message")?.ToString()) ? "<p class='request-message'>" + Eval("Message") + "</p>" : "" %>
                            </div>
                            <div class="request-card-footer">
                                <div class="permission-selector">
                                    <label for='<%# "ddlPermission_" + Eval("RequestId") %>' class="permission-label">הרשאה:</label>
                                    <asp:DropDownList ID="ddlPermission" runat="server" CssClass="permission-dropdown">
                                        <asp:ListItem Value="Read" Text="קריאה בלבד" Selected="True"></asp:ListItem>
                                        <asp:ListItem Value="Write" Text="עריכה בלבד"></asp:ListItem>
                                        <asp:ListItem Value="ReadWrite" Text="קריאה ועריכה"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                                <asp:Button ID="btnApprove" runat="server" Text="אשר" CommandName="Approve" CommandArgument='<%# Eval("RequestId") %>' CssClass="btn-approve" />
                                <asp:Button ID="btnReject" runat="server" Text="דחה" CommandName="Reject" CommandArgument='<%# Eval("RequestId") %>' CssClass="btn-reject" />
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
    </section>

    <style>
        .access-requests-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .access-requests-container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .access-requests-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .access-requests-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .access-requests-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
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

        .requests-grid {
            margin-top: 40px;
        }

        .no-requests-message {
            text-align: center;
            padding: 60px 20px;
            color: #666;
            font-size: 16px;
            background: var(--surface-alt);
            border-radius: 12px;
            margin: 30px 0;
        }

        .request-card {
            background: var(--surface);
            border-radius: 16px;
            padding: 24px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            margin-bottom: 24px;
        }

        .request-card-header {
            margin-bottom: 16px;
        }

        .request-calendar-name {
            font-size: 20px;
            font-weight: 700;
            color: var(--heading);
            margin: 0;
        }

        .request-card-body {
            margin-bottom: 20px;
        }

        .request-meta {
            display: flex;
            flex-direction: column;
            gap: 6px;
            font-size: 13px;
            color: var(--text);
            opacity: 0.6;
            margin-bottom: 12px;
        }

        .request-message {
            color: var(--text);
            opacity: 0.8;
            font-size: 14px;
            padding: 12px;
            background: var(--surface-alt);
            border-radius: 8px;
        }

        .request-card-footer {
            padding-top: 16px;
            border-top: 1px solid var(--border);
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            align-items: center;
        }

        .permission-selector {
            display: flex;
            align-items: center;
            gap: 8px;
            margin-right: auto;
        }

        .permission-label {
            font-size: 14px;
            color: var(--text);
            font-weight: 500;
        }

        .permission-dropdown {
            padding: 8px 12px;
            border-radius: 6px;
            border: 1px solid var(--border);
            background: var(--surface);
            color: var(--text);
            font-size: 14px;
            min-width: 150px;
        }

        .btn-approve, .btn-reject {
            padding: 10px 20px;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            border: none;
            transition: background .2s ease;
        }

        .btn-approve {
            background: var(--success);
            color: #fff;
        }

        .btn-approve:hover {
            background: #28a745;
        }

        .btn-reject {
            background: var(--danger);
            color: #fff;
        }

        .btn-reject:hover {
            background: #dc3545;
        }
    </style>
</asp:Content>


