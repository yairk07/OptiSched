<%@ Page Title="טבלאות משותפות" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="sharedCalendars.aspx.cs" Inherits="sharedCalendars" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="shared-calendars-shell">
        <div class="shared-calendars-container">
            <div class="shared-calendars-header">
                <h2 class="shared-calendars-title">טבלאות משותפות</h2>
                <p class="shared-calendars-subtitle">צור טבלה משותפת חדשה או הצטרף לטבלה קיימת</p>
            </div>

            <div class="shared-calendars-actions">
                <asp:Button ID="btnCreateNew" runat="server" Text="צור טבלה משותפת חדשה" OnClick="btnCreateNew_Click" CssClass="btn-create" />
            </div>

            <asp:Panel ID="pnlCreateForm" runat="server" Visible="false" CssClass="create-form-panel">
                <div class="form-group">
                    <label class="form-label">שם הטבלה <span class="required">*</span></label>
                    <asp:TextBox ID="txtCalendarName" runat="server" CssClass="form-input" placeholder="הזן שם לטבלה"></asp:TextBox>
                </div>

                <div class="form-group">
                    <label class="form-label">תיאור</label>
                    <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-input" placeholder="תיאור הטבלה (אופציונלי)"></asp:TextBox>
                </div>

                <div class="form-actions">
                    <asp:Button ID="btnSaveCalendar" runat="server" Text="צור טבלה" OnClick="btnSaveCalendar_Click" CssClass="btn-save" />
                    <asp:Button ID="btnCancelCreate" runat="server" Text="ביטול" OnClick="btnCancelCreate_Click" CssClass="btn-cancel" />
                </div>
            </asp:Panel>

            <asp:Label ID="lblMessage" runat="server" CssClass="form-message"></asp:Label>

            <asp:Panel ID="pnlPendingRequests" runat="server" Visible="false" CssClass="pending-requests-section">
                <h3 class="section-title">בקשות הצטרפות ממתינות</h3>
                <asp:DataList ID="dlPendingRequests" runat="server" RepeatLayout="Flow" CssClass="requests-list">
                    <ItemTemplate>
                        <div class="request-item">
                            <div class="request-info">
                                <strong><%# Eval("CalendarName") ?? "ללא שם" %></strong>
                                <span class="request-date">תאריך בקשה: <%# Convert.ToDateTime(Eval("RequestDate")).ToString("dd/MM/yyyy HH:mm") %></span>
                            </div>
                            <div class="request-status">
                                <span class="status-badge pending">ממתין לאישור</span>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:DataList>
                <asp:Label ID="lblNoRequests" runat="server" Text="אין בקשות הצטרפות ממתינות" CssClass="no-requests-message" Visible="false" />
            </asp:Panel>

            <div class="calendars-grid">
                <h3 class="section-title">הטבלאות שלי</h3>
                <asp:Label ID="lblNoCalendars" runat="server" Visible="true" CssClass="no-calendars-message" Text="אין טבלאות משותפות להצגה. לחץ על 'צור טבלה משותפת חדשה' כדי להתחיל." />
                <asp:DataList ID="dlCalendars" runat="server" RepeatLayout="Flow" CssClass="calendars-list">
                    <ItemTemplate>
                        <div class="calendar-card">
                            <div class="calendar-card-header">
                                <h3 class="calendar-name"><%# Eval("CalendarName") ?? "ללא שם" %></h3>
                                <div class="calendar-badges">
                                    <%# Convert.ToInt32(Eval("IsAdmin") ?? 0) == 1 ? "<span class='badge admin'>מנהל</span>" : "" %>
                                    <%# Convert.ToInt32(Eval("IsMember") ?? 0) == 1 ? "<span class='badge member'>חבר</span>" : "" %>
                                    <%# Convert.ToInt32(Eval("IsMember") ?? 0) == 1 && Eval("Permission") != null && Eval("Permission").ToString() == "ReadWrite" ? "<span class='badge permission'>עריכה</span>" : "" %>
                                    <%# Convert.ToInt32(Eval("IsMember") ?? 0) == 1 && Eval("Permission") != null && Eval("Permission").ToString() == "Read" ? "<span class='badge permission read-only'>ראיה בלבד</span>" : "" %>
                                </div>
                            </div>
                            <div class="calendar-card-body">
                                <p class="calendar-description"><%# Eval("Description") ?? "" %></p>
                                <div class="calendar-meta">
                                    <span class="meta-item">יוצר: <%# Eval("CreatorName") ?? "ללא שם" %></span>
                                    <span class="meta-item">תאריך: <%# Eval("CreatedDate") != DBNull.Value ? Convert.ToDateTime(Eval("CreatedDate")).ToString("dd/MM/yyyy") : "" %></span>
                                </div>
                            </div>
                            <div class="calendar-card-footer">
                                <%# GetCalendarActionButton(Container.DataItem) %>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:DataList>
            </div>
        </div>
    </section>

    <style>
        .shared-calendars-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .shared-calendars-container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .shared-calendars-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .shared-calendars-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .shared-calendars-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .shared-calendars-actions {
            margin-bottom: 30px;
            text-align: center;
        }

        .btn-create {
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

        .btn-create:hover {
            background: var(--brand-dark);
            transform: translateY(-1px);
        }

        .create-form-panel {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            margin-bottom: 30px;
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

        .form-actions {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            margin-top: 24px;
        }

        .btn-save {
            padding: 12px 28px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
        }

        .btn-cancel {
            padding: 12px 28px;
            background: var(--surface);
            color: var(--text);
            border: 1px solid var(--border);
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
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

        .section-title {
            font-size: 24px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 20px;
            padding-bottom: 12px;
            border-bottom: 2px solid var(--border);
        }

        .pending-requests-section {
            margin-bottom: 40px;
            background: var(--surface);
            border-radius: 16px;
            padding: 24px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .requests-list {
            display: flex;
            flex-direction: column;
            gap: 12px;
        }

        .request-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 16px;
            background: var(--bg);
            border-radius: 8px;
            border: 1px solid var(--border);
        }

        .request-info {
            display: flex;
            flex-direction: column;
            gap: 6px;
        }

        .request-info strong {
            font-size: 16px;
            color: var(--heading);
        }

        .request-date {
            font-size: 13px;
            color: var(--text);
            opacity: 0.7;
        }

        .request-status {
            display: flex;
            align-items: center;
        }

        .status-badge {
            padding: 6px 14px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: 600;
        }

        .status-badge.pending {
            background: #ffa726;
            color: #fff;
        }

        .no-requests-message {
            text-align: center;
            padding: 20px;
            color: var(--text);
            opacity: 0.6;
            font-size: 14px;
        }

        .calendars-grid {
            margin-top: 40px;
            width: 100%;
        }

        .calendars-grid .section-title {
            margin-bottom: 24px;
            font-size: 24px;
            font-weight: 700;
            color: var(--heading);
            padding-bottom: 12px;
            border-bottom: 2px solid var(--border);
        }

        .calendars-list {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
            gap: 28px;
            margin-top: 24px;
            align-items: stretch;
            width: 100%;
        }

        @media (max-width: 768px) {
            .calendars-list {
                grid-template-columns: 1fr;
                gap: 20px;
            }
        }

        .calendar-card {
            background: var(--surface);
            border-radius: 16px;
            padding: 28px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            transition: transform .2s ease, box-shadow .2s ease;
            display: flex;
            flex-direction: column;
            min-height: 280px;
            height: 100%;
        }

        .calendar-card:hover {
            transform: translateY(-4px);
            box-shadow: var(--shadow-lg);
        }

        .calendar-card-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 18px;
            gap: 12px;
        }

        .calendar-name {
            font-size: 22px;
            font-weight: 700;
            color: var(--heading);
            margin: 0;
            line-height: 1.3;
            flex: 1;
        }

        .calendar-badges {
            display: flex;
            gap: 8px;
            flex-wrap: wrap;
            justify-content: flex-end;
        }

        .badge {
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: 600;
        }

        .badge.admin {
            background: var(--brand);
            color: #fff;
        }

        .badge.member {
            background: var(--success);
            color: #fff;
        }

        .badge.permission {
            background: #4a90e2;
            color: #fff;
        }

        .badge.permission.read-only {
            background: #95a5a6;
            color: #fff;
        }

        .calendar-card-body {
            margin-bottom: 20px;
            flex: 1;
        }

        .calendar-description {
            color: var(--text);
            opacity: 0.8;
            margin-bottom: 16px;
            font-size: 14px;
            line-height: 1.6;
            min-height: 40px;
        }

        .calendar-meta {
            display: flex;
            flex-direction: column;
            gap: 8px;
            font-size: 13px;
            color: var(--text);
            opacity: 0.7;
            padding-top: 12px;
            border-top: 1px solid var(--border);
        }

        .meta-item {
            display: flex;
            align-items: center;
            gap: 6px;
        }

        .no-calendars-message {
            text-align: center;
            padding: 60px 20px;
            color: #666;
            font-size: 16px;
            background: var(--surface-alt);
            border-radius: 12px;
            margin: 30px 0;
        }

        .calendar-card-footer {
            padding-top: 16px;
            border-top: 1px solid var(--border);
            margin-top: auto;
        }

        .btn-view, .btn-join {
            display: inline-block;
            padding: 10px 20px;
            background: var(--brand);
            color: #fff;
            border-radius: 8px;
            text-decoration: none;
            font-weight: 600;
            transition: background .2s ease;
        }

        .btn-view:hover, .btn-join:hover, .btn-request:hover {
            background: var(--brand-dark);
            text-decoration: none;
        }

        .btn-request {
            display: inline-block;
            padding: 10px 20px;
            background: var(--brand);
            color: #fff;
            border-radius: 8px;
            text-decoration: none;
            font-weight: 600;
            transition: background .2s ease;
            border: none;
            cursor: pointer;
        }

        .btn-requested {
            display: inline-block;
            padding: 10px 20px;
            background: #666;
            color: #fff;
            border-radius: 8px;
            font-weight: 600;
        }

        @media (max-width: 768px) {
            .calendars-list {
                grid-template-columns: 1fr;
            }
        }
    </style>

    <script type="text/javascript">
        function requestAccess(calendarId) {
            console.log('requestAccess called with calendarId:', calendarId);
            if (confirm('האם אתה בטוח שברצונך לבקש גישה לטבלה זו?')) {
                console.log('User confirmed, redirecting to:', 'sharedCalendars.aspx?requestAccess=' + calendarId);
                // Use simple query string approach
                window.location.href = 'sharedCalendars.aspx?requestAccess=' + calendarId;
            } else {
                console.log('User cancelled');
            }
        }
    </script>
</asp:Content>
