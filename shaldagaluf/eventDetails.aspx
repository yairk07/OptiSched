<%@ Page Title="פרטי אירוע" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="eventDetails.aspx.cs" Inherits="eventDetails" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="head" ContentPlaceHolderID="head" runat="server"></asp:Content>

<asp:Content ID="body" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <section class="edit-event-shell">
        <div class="edit-event-container">
            <div class="edit-event-header">
                <h2 class="edit-event-title">פרטי האירוע</h2>
                <p class="edit-event-subtitle">צפייה בפרטי האירוע</p>
            </div>

            <asp:Panel ID="pnlContent" runat="server" Visible="false">
                <div class="edit-event-form">
                    <div class="form-group">
                        <label class="form-label">כותרת</label>
                        <div class="form-display-value">
                            <asp:Label ID="lblTitle" runat="server" />
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label class="form-label">תאריך</label>
                            <div class="form-display-value">
                                <asp:Label ID="lblDate" runat="server" />
                            </div>
                        </div>

                        <div class="form-group">
                            <label class="form-label">שעה</label>
                            <div class="form-display-value">
                                <asp:Label ID="lblTime" runat="server" />
                            </div>
                        </div>
                    </div>

                    <div class="form-row">
                        <div class="form-group">
                            <label class="form-label">קטגוריה</label>
                            <div class="form-display-value">
                                <asp:Label ID="lblCategory" runat="server" />
                            </div>
                        </div>

                        <div class="form-group">
                            <label class="form-label">משתמש</label>
                            <div class="form-display-value">
                                <asp:Label ID="lblUserName" runat="server" />
                            </div>
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">סוג אירוע</label>
                        <div class="form-display-value">
                            <asp:Label ID="lblEventType" runat="server" />
                        </div>
                    </div>

                    <div class="form-group">
                        <label class="form-label">הערות</label>
                        <div class="form-display-value form-display-textarea">
                            <asp:Label ID="lblNotes" runat="server" />
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiles" runat="server" Visible="false">
                        <div class="form-group">
                            <label class="form-label">קבצים מצורפים</label>
                            <asp:Repeater ID="rptFiles" runat="server">
                                <ItemTemplate>
                                    <div style="padding: 8px; border: 1px solid var(--border); border-radius: 4px; margin-bottom: 8px;">
                                        <a href='<%# "downloadFile.ashx?id=" + Eval("Id") %>' target="_blank" style="color: var(--brand); text-decoration: none;">
                                            📎 <%# Eval("file_name") %>
                                        </a>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlImages" runat="server" Visible="false">
                        <div class="form-group">
                            <label class="form-label">תמונות</label>
                            <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px;">
                                <asp:Repeater ID="rptImages" runat="server">
                                    <ItemTemplate>
                                        <div style="border: 1px solid var(--border); border-radius: 4px; overflow: hidden;">
                                            <a href='<%# "showImage.ashx?id=" + Eval("Id") %>' target="_blank">
                                                <img src='<%# "showImage.ashx?id=" + Eval("Id") %>' alt='<%# Eval("image_name") %>' style="width: 100%; height: 200px; object-fit: cover; cursor: pointer;" />
                                            </a>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                    </asp:Panel>

                    <div class="form-actions">
                        <asp:HyperLink ID="lnkEdit" runat="server" CssClass="btn-save" Text="ערוך" />
                        <asp:HyperLink ID="lnkBack" runat="server" Text="חזרה" NavigateUrl="allEvents.aspx" CssClass="btn-cancel" />
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlNotFound" runat="server" Visible="false">
                <div class="not-found-message">
                    <h3>אירוע לא נמצא</h3>
                    <p>האירוע המבוקש לא נמצא במערכת.</p>
                    <asp:HyperLink ID="lnkBackNotFound" runat="server" Text="חזרה לרשימת האירועים" NavigateUrl="allEvents.aspx" CssClass="btn-save" />
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

        .form-display-value {
            width: 100%;
            padding: 12px 16px;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 15px;
            direction: rtl;
            background: var(--bg);
            color: var(--text);
            min-height: 20px;
            box-sizing: border-box;
        }

        .form-display-textarea {
            min-height: 120px;
            white-space: pre-wrap;
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
            text-decoration: none;
            display: inline-block;
        }

        .btn-save:hover {
            background: var(--brand-dark);
            text-decoration: none;
            color: #fff;
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

        .not-found-message {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            text-align: center;
        }

        .not-found-message h3 {
            font-size: 24px;
            color: var(--heading);
            margin-bottom: 16px;
        }

        .not-found-message p {
            color: var(--text);
            margin-bottom: 24px;
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

