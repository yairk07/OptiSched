<%@ Page Title="תיקון נתוני עברית" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="fixHebrewData.aspx.cs" Inherits="fixHebrewData" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="fix-hebrew-shell">
        <div class="fix-hebrew-container">
            <div class="fix-hebrew-card">
                <div class="card-header">
                    <h2 class="card-title">תיקון נתוני עברית במסד הנתונים</h2>
                </div>
                
                <asp:Panel ID="pnlNotAuthorized" runat="server" Visible="false">
                    <div class="error-message">
                        <p>אין לך הרשאה לגשת לדף זה. רק בעלי האתר יכולים להריץ תיקון זה.</p>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlContent" runat="server" Visible="false">
                    <div class="warning-box">
                        <p><strong>אזהרה:</strong> פעולה זו תשנה נתונים במסד הנתונים. ודא שיש לך גיבוי לפני המשך.</p>
                    </div>

                    <div class="form-container">
                        <asp:Label ID="lblMessage" runat="server" CssClass="message-label" Visible="false"></asp:Label>
                        
                        <div class="form-group">
                            <label class="form-label">בחר טבלה לתיקון:</label>
                            <asp:DropDownList ID="ddlTable" runat="server" CssClass="form-input">
                                <asp:ListItem Value="Users" Text="Users (משתמשים)" Selected="True"></asp:ListItem>
                                <asp:ListItem Value="CalendarEvents" Text="CalendarEvents (אירועים אישיים)"></asp:ListItem>
                                <asp:ListItem Value="SharedCalendarEvents" Text="SharedCalendarEvents (אירועים משותפים)"></asp:ListItem>
                            </asp:DropDownList>
                        </div>

                        <div class="form-actions">
                            <asp:Button ID="btnFix" runat="server" Text="הרץ תיקון" OnClick="btnFix_Click" CssClass="btn-save" OnClientClick="return confirm('האם אתה בטוח שברצונך להריץ תיקון זה? פעולה זו תשנה נתונים במסד הנתונים.');" />
                        </div>

                        <asp:Panel ID="pnlResults" runat="server" Visible="false" CssClass="results-panel">
                            <h3>תוצאות התיקון:</h3>
                            <asp:Label ID="lblResults" runat="server" CssClass="results-label"></asp:Label>
                        </asp:Panel>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </section>

    <style>
        .fix-hebrew-shell {
            width: min(1200px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .fix-hebrew-container {
            max-width: 800px;
            margin: 0 auto;
        }

        .fix-hebrew-card {
            background: var(--surface);
            border-radius: 12px;
            padding: 30px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        .card-header {
            margin-bottom: 25px;
            padding-bottom: 15px;
            border-bottom: 2px solid var(--border);
        }

        .card-title {
            font-size: 24px;
            font-weight: 700;
            color: var(--heading);
            margin: 0;
        }

        .warning-box {
            background: #fff3cd;
            border: 1px solid #ffc107;
            border-radius: 8px;
            padding: 15px;
            margin-bottom: 25px;
        }

        .warning-box p {
            margin: 0;
            color: #856404;
        }

        .form-container {
            margin-top: 20px;
        }

        .form-group {
            margin-bottom: 20px;
        }

        .form-label {
            display: block;
            margin-bottom: 8px;
            font-weight: 600;
            color: var(--heading);
        }

        .form-input {
            width: 100%;
            padding: 10px;
            border: 1px solid var(--border);
            border-radius: 6px;
            font-size: 14px;
        }

        .form-actions {
            margin-top: 25px;
        }

        .btn-save {
            background: var(--brand);
            color: white;
            border: none;
            padding: 12px 30px;
            border-radius: 6px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
        }

        .btn-save:hover {
            background: var(--brand-hover);
        }

        .message-label {
            display: block;
            padding: 12px;
            border-radius: 6px;
            margin-bottom: 20px;
        }

        .error-message {
            background: #f8d7da;
            color: #721c24;
            padding: 15px;
            border-radius: 6px;
            border: 1px solid #f5c6cb;
        }

        .results-panel {
            margin-top: 30px;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 8px;
            border: 1px solid var(--border);
        }

        .results-panel h3 {
            margin-top: 0;
            margin-bottom: 15px;
            color: var(--heading);
        }

        .results-label {
            display: block;
            white-space: pre-line;
            line-height: 1.6;
        }
    </style>
</asp:Content>





