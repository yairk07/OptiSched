<%@ Page Title="עריכת אירוע" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="editEvent.aspx.cs" Inherits="editEvent" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="head" ContentPlaceHolderID="head" runat="server"></asp:Content>

<asp:Content ID="body" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <section class="edit-event-shell">
        <div class="edit-event-container">
            <div class="edit-event-header">
                <h2 class="edit-event-title">עריכת אירוע</h2>
                <p class="edit-event-subtitle">עדכן את פרטי האירוע ושמור את השינויים</p>
            </div>

            <asp:Panel ID="pnlForm" runat="server">
                <div class="edit-event-form">
                    <div class="form-group">
                        <label class="form-label">כותרת</label>
                        <asp:TextBox ID="txtTitle" runat="server" CssClass="form-input" placeholder="הזן כותרת לאירוע" />
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label class="form-label">תאריך</label>
                            <asp:TextBox ID="txtDate" runat="server" CssClass="form-input" TextMode="Date" />
                        </div>

                        <div class="form-group">
                            <label class="form-label">שעה</label>
                            <asp:TextBox ID="txtTime" runat="server" CssClass="form-input" TextMode="Time" />
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">קטגוריה</label>
                        <asp:DropDownList ID="ddlCategory" runat="server" CssClass="form-input">
                            <asp:ListItem Text="אירוע" Value="אירוע"></asp:ListItem>
                            <asp:ListItem Text="יום הולדת" Value="יום הולדת"></asp:ListItem>
                            <asp:ListItem Text="פגישה" Value="פגישה"></asp:ListItem>
                            <asp:ListItem Text="מטלה" Value="מטלה"></asp:ListItem>
                            <asp:ListItem Text="אחר" Value="אחר"></asp:ListItem>
                        </asp:DropDownList>
                    </div>

                    <div class="form-group">
                        <label class="form-label">הערות</label>
                        <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="5" CssClass="form-input form-textarea" placeholder="הזן הערות נוספות (אופציונלי)" />
                    </div>

                    <div class="form-actions">
                        <asp:Button ID="btnSave" runat="server" Text="שמור שינויים" OnClick="btnSave_Click" CssClass="btn-save" />
                        <asp:HyperLink ID="lnkBack" runat="server" Text="ביטול" NavigateUrl="allEvents.aspx" CssClass="btn-cancel" />
                    </div>
                </div>
            </asp:Panel>
        </div>
    </section>

    <style>
        .edit-event-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .edit-event-container {
            max-width: 700px;
            margin: 0 auto;
        }

        .edit-event-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .edit-event-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .edit-event-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .edit-event-form {
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

        .form-textarea {
            resize: vertical;
            min-height: 120px;
            font-family: inherit;
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

</asp:Content>
