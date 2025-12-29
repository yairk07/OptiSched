<%@ Page Title="טבלה משותפת" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="sharedCalendarDetails.aspx.cs" Inherits="sharedCalendarDetails" Debug="true" ResponseEncoding="utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="shared-calendar-details-shell">
        <div class="shared-calendar-details-container">
            <asp:Panel ID="pnlContent" runat="server">
                <div class="calendar-header">
                    <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="sharedCalendars.aspx" CssClass="back-link">
                        &laquo; חזרה לטבלאות משותפות
                    </asp:HyperLink>
                    <asp:Label ID="calendarTitle" runat="server" CssClass="calendar-title"></asp:Label>
                    <asp:Label ID="calendarDescription" runat="server" CssClass="calendar-description"></asp:Label>
                </div>

                <asp:Panel ID="pnlNotMember" runat="server" Visible="false" CssClass="not-member-panel">
                    <div class="join-section">
                        <h3>הצטרף לטבלה</h3>
                        <p>שלח בקשה להצטרפות לטבלה זו</p>
                        <div class="form-group">
                            <label class="form-label">הודעה (אופציונלי)</label>
                            <asp:TextBox ID="txtJoinMessage" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-input" placeholder="הודעה למנהל הטבלה"></asp:TextBox>
                        </div>
                        <asp:Button ID="btnSendJoinRequest" runat="server" Text="שלח בקשה להצטרפות" OnClick="btnSendJoinRequest_Click" CssClass="btn-join-request" />
                        <asp:Label ID="lblJoinMessage" runat="server" CssClass="form-message"></asp:Label>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlMember" runat="server" Visible="false">
                    <div class="calendar-tabs">
                        <asp:Button ID="btnTabEvents" runat="server" Text="אירועים" OnClick="btnTabEvents_Click" CssClass="tab-button active" />
                        <asp:Button ID="btnTabRequests" runat="server" Text="בקשות הצטרפות" OnClick="btnTabRequests_Click" CssClass="tab-button" Visible="false" />
                    </div>

                    <asp:Panel ID="pnlEvents" runat="server">
                        <asp:Panel ID="pnlAddEvent" runat="server" Visible="false" CssClass="add-event-panel">
                            <h3>הוסף אירוע</h3>
                            <div class="form-row">
                                <div class="form-group">
                                    <label class="form-label">כותרת <span class="required">*</span></label>
                                    <asp:TextBox ID="txtEventTitle" runat="server" CssClass="form-input" placeholder="הזן כותרת"></asp:TextBox>
                                </div>
                                <div class="form-group">
                                    <label class="form-label">תאריך <span class="required">*</span></label>
                                    <asp:TextBox ID="txtEventDate" runat="server" TextMode="Date" CssClass="form-input"></asp:TextBox>
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group">
                                    <label class="form-label">שעה</label>
                                    <asp:TextBox ID="txtEventTime" runat="server" TextMode="Time" CssClass="form-input"></asp:TextBox>
                                </div>
                                <div class="form-group">
                                    <label class="form-label">קטגוריה</label>
                                    <asp:DropDownList ID="ddlEventCategory" runat="server" CssClass="form-input">
                                        <asp:ListItem Text="אירוע" Value="אירוע" Selected="True"></asp:ListItem>
                                        <asp:ListItem Text="יום הולדת" Value="יום הולדת"></asp:ListItem>
                                        <asp:ListItem Text="פגישה" Value="פגישה"></asp:ListItem>
                                        <asp:ListItem Text="מטלה" Value="מטלה"></asp:ListItem>
                                        <asp:ListItem Text="אחר" Value="אחר"></asp:ListItem>
                                    </asp:DropDownList>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="form-label">הערות</label>
                                <asp:TextBox ID="txtEventNotes" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-input" placeholder="הערות נוספות"></asp:TextBox>
                            </div>
                            <div class="form-actions">
                                <asp:Button ID="btnSaveEvent" runat="server" Text="שמור אירוע" OnClick="btnSaveEvent_Click" CssClass="btn-save" />
                                <asp:Button ID="btnCancelEvent" runat="server" Text="ביטול" OnClick="btnCancelEvent_Click" CssClass="btn-cancel" />
                            </div>
                        </asp:Panel>

                        <div class="events-actions">
                            <asp:Button ID="btnAddEvent" runat="server" Text="הוסף אירוע" OnClick="btnAddEvent_Click" CssClass="btn-add-event" />
                        </div>

                        <div class="events-table-container">
                        <asp:DataList ID="dlEvents" runat="server" RepeatLayout="Table" CssClass="events-table">
                            <HeaderTemplate>
                                <table class="events-table">
                                    <thead>
                                        <tr>
                                            <th>כותרת</th>
                                            <th>תאריך</th>
                                            <th>שעה</th>
                                            <th>קטגוריה</th>
                                            <th>הערות</th>
                                            <th>נוצר על ידי</th>
                                            <th>פעולות</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                            </HeaderTemplate>
                            <ItemTemplate>
                                        <tr>
                                            <td><%# GetSafeString(Eval("Title"), "ללא כותרת") %></td>
                                            <td><%# GetSafeDate(Eval("EventDate")) %></td>
                                            <td><%# GetSafeString(Eval("EventTime"), "—") %></td>
                                            <td><%# GetSafeString(Eval("Category"), "אחר") %></td>
                                            <td><%# GetSafeString(Eval("Notes"), "—") %></td>
                                            <td><%# GetSafeString(Eval("CreatedByName"), "ללא שם") %></td>
                                            <td>
                                                <asp:LinkButton ID="lnkEdit" runat="server" CommandArgument='<%# Eval("Id") %>' OnClick="lnkEdit_Click" CssClass="edit-link">ערוך</asp:LinkButton>
                                                <asp:LinkButton ID="lnkDelete" runat="server" CommandArgument='<%# Eval("Id") %>' OnClick="lnkDelete_Click" CssClass="delete-link" OnClientClick="return confirm('האם אתה בטוח שברצונך למחוק את האירוע?');">מחק</asp:LinkButton>
                                            </td>
                                        </tr>
                            </ItemTemplate>
                            <FooterTemplate>
                                    </tbody>
                                </table>
                            </FooterTemplate>
                        </asp:DataList>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlRequests" runat="server" Visible="false">
                        <h3>בקשות הצטרפות</h3>
                        <asp:DataList ID="dlRequests" runat="server" CssClass="requests-list">
                            <ItemTemplate>
                                <div class="request-card">
                                    <div class="request-info">
                                        <strong><%# Eval("UserName") %> (<%# Eval("firstName") %> <%# Eval("lastName") %>)</strong>
                                        <p><%# Eval("Message") %></p>
                                        <small>תאריך בקשה: <%# Convert.ToDateTime(Eval("RequestDate")).ToString("dd/MM/yyyy HH:mm") %></small>
                                    </div>
                                    <div class="request-actions">
                                        <asp:Button ID="btnApprove" runat="server" Text="אשר" CommandArgument='<%# Eval("RequestId") %>' OnClick="btnApprove_Click" CssClass="btn-approve" />
                                        <asp:Button ID="btnReject" runat="server" Text="דחה" CommandArgument='<%# Eval("RequestId") %>' OnClick="btnReject_Click" CssClass="btn-reject" />
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:DataList>
                    </asp:Panel>
                </asp:Panel>
            </asp:Panel>

            <asp:Panel ID="pnlNotFound" runat="server" Visible="false" CssClass="not-found-panel">
                <h2>הטבלה לא נמצאה</h2>
                <asp:HyperLink ID="lnkBackNotFound" runat="server" NavigateUrl="sharedCalendars.aspx" CssClass="back-link">
                    &laquo; חזרה לטבלאות משותפות
                </asp:HyperLink>
            </asp:Panel>
        </div>
    </section>

    <style>
        .shared-calendar-details-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .shared-calendar-details-container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .back-link {
            display: inline-block;
            margin-bottom: 20px;
            color: var(--brand);
            text-decoration: none;
            font-weight: 600;
        }

        .back-link:hover {
            text-decoration: underline;
        }

        .calendar-header {
            margin-bottom: 30px;
        }

        .calendar-title {
            display: block;
            font-size: 28px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 8px;
        }

        .calendar-description {
            display: block;
            color: var(--text);
            opacity: 0.8;
            font-size: 16px;
            margin-bottom: 20px;
        }

        .not-member-panel {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .join-section {
            text-align: center;
        }

        .calendar-tabs {
            display: flex;
            gap: 12px;
            margin-bottom: 30px;
            border-bottom: 2px solid var(--border);
        }

        .tab-button {
            padding: 12px 24px;
            background: transparent;
            border: none;
            border-bottom: 3px solid transparent;
            color: var(--text);
            font-weight: 600;
            cursor: pointer;
            transition: all .2s ease;
        }

        .tab-button.active {
            color: var(--brand);
            border-bottom-color: var(--brand);
        }

        .add-event-panel {
            background: var(--surface);
            border-radius: 20px;
            padding: 30px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            margin-bottom: 30px;
        }

        .events-actions {
            margin-bottom: 20px;
        }

        .btn-add-event {
            padding: 12px 24px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
        }


        .requests-list {
            display: flex;
            flex-direction: column;
            gap: 16px;
        }

        .request-card {
            background: var(--surface);
            border-radius: 12px;
            padding: 20px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .request-info {
            flex: 1;
        }

        .request-actions {
            display: flex;
            gap: 12px;
        }

        .btn-approve, .btn-reject {
            padding: 8px 16px;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
        }

        .btn-approve {
            background: var(--success);
            color: #fff;
        }

        .btn-reject {
            background: #ff6b6b;
            color: #fff;
        }

        .form-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 20px;
        }

        .form-group {
            margin-bottom: 20px;
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
            box-sizing: border-box;
        }

        .form-actions {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
        }

        .btn-save, .btn-cancel, .btn-join-request {
            padding: 12px 24px;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
        }

        .btn-save, .btn-join-request {
            background: var(--brand);
            color: #fff;
        }

        .btn-cancel {
            background: var(--surface);
            color: var(--text);
            border: 1px solid var(--border);
        }

        .form-message {
            display: block;
            padding: 12px;
            border-radius: 8px;
            margin-top: 16px;
            text-align: center;
            font-weight: 600;
        }

        @media (max-width: 768px) {
            .form-row {
                grid-template-columns: 1fr;
            }

            .request-card {
                flex-direction: column;
                align-items: flex-start;
            }
        }
    </style>
</asp:Content>
