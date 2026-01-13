<%@ Page Title="Event Details" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="eventDetails.aspx.cs" Inherits="eventDetails"
    ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="head" ContentPlaceHolderID="head" runat="server">
    <style>
        .event-details-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .event-details-container {
            max-width: 700px;
            margin: 0 auto;
        }

        .event-details-header {
            text-align: center;
            margin-bottom: 40px;
        }

        .event-details-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .event-details-subtitle {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .back-link {
            display: inline-block;
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

        .event-details-form {
            background: var(--surface);
            border-radius: 20px;
            padding: 40px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .field {
            margin-bottom: 24px;
        }

        .field-label {
            display: block;
            font-weight: 600;
            color: var(--heading);
            margin-bottom: 8px;
            font-size: 15px;
        }

        .field-value {
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

        .field-value-textarea {
            min-height: 120px;
            white-space: pre-wrap;
        }

        .field-row {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
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
            .field-row {
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

<asp:Content ID="body" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <section class="event-details-shell">
        <div class="event-details-container">
            <a href="allEvents.aspx" class="back-link">&larr; Back to All Events</a>

            <asp:Panel ID="pnlContent" runat="server" Visible="false">
                <div class="event-details-header">
                    <h2 class="event-details-title">Event Details</h2>
                    <p class="event-details-subtitle">View event information</p>
                </div>

                <div class="event-details-form">
                    <div class="field">
                        <span class="field-label">Title:</span>
                        <div class="field-value">
                            <asp:Label ID="lblTitle" runat="server" />
                        </div>
                    </div>

                    <div class="field-row">
                        <div class="field">
                            <span class="field-label">Date:</span>
                            <div class="field-value">
                                <asp:Label ID="lblDate" runat="server" />
                            </div>
                        </div>

                        <div class="field">
                            <span class="field-label">Time:</span>
                            <div class="field-value">
                                <asp:Label ID="lblTime" runat="server" />
                            </div>
                        </div>
                    </div>

                    <div class="field-row">
                        <div class="field">
                            <span class="field-label">Category:</span>
                            <div class="field-value">
                                <asp:Label ID="lblCategory" runat="server" />
                            </div>
                        </div>

                        <div class="field">
                            <span class="field-label">User:</span>
                            <div class="field-value">
                                <asp:Label ID="lblUserName" runat="server" />
                            </div>
                        </div>
                    </div>

                    <div class="field">
                        <span class="field-label">Notes:</span>
                        <div class="field-value field-value-textarea">
                            <asp:Label ID="lblNotes" runat="server" />
                        </div>
                    </div>

                    <asp:Panel ID="pnlFiles" runat="server" Visible="false">
                        <div class="field">
                            <span class="field-label">Attached Files:</span>
                            <div class="field-value">
                                <asp:Repeater ID="rptFiles" runat="server">
                                    <ItemTemplate>
                                        <div style="margin-bottom: 8px;">
                                            <a href='<%# "downloadFile.ashx?id=" + Eval("Id") %>' target="_blank" style="color: #e50914; text-decoration: none;">
                                                📎 <%# Eval("file_name") %>
                                            </a>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlImages" runat="server" Visible="false">
                        <div class="field">
                            <span class="field-label">Images:</span>
                            <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 10px; margin-top: 10px;">
                                <asp:Repeater ID="rptImages" runat="server">
                                    <ItemTemplate>
                                        <a href='<%# "showImage.ashx?id=" + Eval("Id") %>' target="_blank">
                                            <img src='<%# "showImage.ashx?id=" + Eval("Id") %>' alt='<%# Eval("image_name") %>' style="width: 100%; height: 150px; object-fit: cover; border-radius: 4px; border: 1px solid #ddd;" />
                                        </a>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                    </asp:Panel>

                    <div class="form-actions">
                        <asp:HyperLink ID="lnkEdit" runat="server" CssClass="btn-save" Text="Edit" />
                        <a href="allEvents.aspx" class="btn-cancel">Back</a>
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlNotFound" runat="server" Visible="false">
                <div class="not-found-message">
                    <h3>Event Not Found</h3>
                    <p>The requested event was not found in the system.</p>
                    <a href="allEvents.aspx" class="btn-save" style="margin-top: 20px; display: inline-block;">Back to All Events</a>
                </div>
            </asp:Panel>
        </div>
    </section>
</asp:Content>

