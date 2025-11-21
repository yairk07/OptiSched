<%@ Page Title="פרטי משתמש" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="exuserdetails.aspx.cs" Inherits="exuserdetails" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <section class="user-details-shell">
        <!-- פאנל שמציג את פרטי המשתמש -->
        <asp:Panel ID="pnlContent" runat="server" Visible="false">
            <div class="user-details-container">
                <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="exusers.aspx" CssClass="back-link">
                    ← חזרה לרשימת המשתמשים
                </asp:HyperLink>

                <div class="user-details-card">
                    <div class="user-details-header">
                        <div class="user-avatar-large">
                            <span id="avatarLetter" runat="server"></span>
                        </div>
                        <div class="user-header-info">
                            <h2 class="user-details-title">פרטי המשתמש</h2>
                            <p class="user-details-subtitle"><asp:Label ID="lblUserName" runat="server" /></p>
                        </div>
                    </div>

                    <div class="user-details-body">
                        <div class="detail-row">
                            <div class="detail-label">
                                <span class="detail-icon">👤</span>
                                <span>שם פרטי</span>
                            </div>
                            <div class="detail-value">
                                <asp:Label ID="lblFirstName" runat="server" />
                            </div>
                        </div>

                        <div class="detail-row">
                            <div class="detail-label">
                                <span class="detail-icon">👤</span>
                                <span>שם משפחה</span>
                            </div>
                            <div class="detail-value">
                                <asp:Label ID="lblLastName" runat="server" />
                            </div>
                        </div>

                        <div class="detail-row">
                            <div class="detail-label">
                                <span class="detail-icon">📧</span>
                                <span>אימייל</span>
                            </div>
                            <div class="detail-value">
                                <asp:Label ID="lblEmail" runat="server" />
                            </div>
                        </div>

                        <div class="detail-row">
                            <div class="detail-label">
                                <span class="detail-icon">📱</span>
                                <span>טלפון</span>
                            </div>
                            <div class="detail-value">
                                <asp:Label ID="lblPhone" runat="server" />
                            </div>
                        </div>

                        <div class="detail-row">
                            <div class="detail-label">
                                <span class="detail-icon">📍</span>
                                <span>עיר</span>
                            </div>
                            <div class="detail-value">
                                <asp:Label ID="lblCity" runat="server" />
                            </div>
                        </div>

                        <div class="detail-row">
                            <div class="detail-label">
                                <span class="detail-icon">🔐</span>
                                <span>רמת גישה</span>
                            </div>
                            <div class="detail-value">
                                <span class="access-badge"><asp:Label ID="lblAccessLevel" runat="server" /></span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <!-- פאנל במקרה שהמשתמש לא נמצא -->
        <asp:Panel ID="pnlNotFound" runat="server" Visible="false">
            <div class="not-found-container">
                <asp:HyperLink ID="lnkBack2" runat="server" NavigateUrl="exusers.aspx" CssClass="back-link">
                    ← חזרה לרשימת המשתמשים
                </asp:HyperLink>

                <div class="not-found-card">
                    <div class="not-found-icon">❌</div>
                    <h2>המשתמש לא נמצא</h2>
                    <p>לא נמצאו פרטים עבור המשתמש המבוקש.</p>
                </div>
            </div>
        </asp:Panel>
    </section>

    <style>
        .user-details-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .user-details-container {
            max-width: 800px;
            margin: 0 auto;
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

        .user-details-card {
            background: var(--surface);
            border-radius: 20px;
            padding: 32px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
        }

        .user-details-header {
            display: flex;
            align-items: center;
            gap: 24px;
            margin-bottom: 32px;
            padding-bottom: 24px;
            border-bottom: 2px solid var(--border);
        }

        .user-avatar-large {
            width: 80px;
            height: 80px;
            border-radius: 50%;
            background: var(--brand);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 32px;
            font-weight: 700;
            flex-shrink: 0;
        }

        .user-header-info {
            flex: 1;
        }

        .user-details-title {
            font-size: 28px;
            font-weight: 700;
            color: var(--heading);
            margin: 0 0 8px 0;
        }

        .user-details-subtitle {
            font-size: 18px;
            color: var(--text);
            opacity: 0.8;
            margin: 0;
        }

        .user-details-body {
            display: flex;
            flex-direction: column;
            gap: 20px;
        }

        .detail-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding: 16px;
            background: rgba(229, 9, 20, 0.03);
            border-radius: 12px;
            border: 1px solid var(--border);
        }

        .detail-label {
            display: flex;
            align-items: center;
            gap: 10px;
            font-weight: 600;
            color: var(--heading);
            font-size: 15px;
        }

        .detail-icon {
            font-size: 20px;
        }

        .detail-value {
            color: var(--text);
            font-size: 15px;
        }

        .access-badge {
            display: inline-block;
            padding: 6px 14px;
            background: var(--brand);
            color: #fff;
            border-radius: 20px;
            font-weight: 600;
            font-size: 13px;
        }

        .not-found-container {
            max-width: 600px;
            margin: 0 auto;
        }

        .not-found-card {
            background: var(--surface);
            border-radius: 20px;
            padding: 48px 32px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            text-align: center;
        }

        .not-found-icon {
            font-size: 64px;
            margin-bottom: 20px;
        }

        .not-found-card h2 {
            font-size: 24px;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .not-found-card p {
            color: var(--text);
            opacity: 0.8;
        }
    </style>

</asp:Content>
