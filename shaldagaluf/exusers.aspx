<%@ Page Title="רשימת משתמשים" Language="C#" MasterPageFile="~/danimaster.master"
    AutoEventWireup="true" CodeFile="exusers.aspx.cs" Inherits="exusers" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <section class="users-shell">
        <div class="users-hero">
            <h2 class="hero-title">רשימת משתמשים</h2>
            <p class="hero-description">ניהול וצפייה בכל המשתמשים הרשומים במערכת</p>
        </div>

        <div class="users-search-section">
            <asp:TextBox ID="txtSearchemail" runat="server" CssClass="search-input" Placeholder="חפש לפי שם, אימייל, טלפון או עיר..." />
            <asp:Button ID="btnSearch" runat="server" Text="חפש" OnClick="btnSearch_Click" CssClass="search-button" />
        </div>

        <div class="users-grid-container">
            <asp:DataList ID="DataListUsers" runat="server"
                RepeatColumns="3"
                RepeatDirection="Horizontal"
                CssClass="users-grid"
                ItemStyle-CssClass="user-card-wrapper">

                <ItemTemplate>
                    <div class="user-card">
                        <div class="user-card-header">
                            <div class="user-avatar">
                                <span><%# Eval("userName").ToString().Substring(0, 1).ToUpper() %></span>
                            </div>
                            <div class="user-name-section">
                                <h3 class="user-name"><%# Eval("userName") %></h3>
                                <p class="user-full-name"><%# Eval("firstName") %> <%# Eval("lastName") %></p>
                            </div>
                        </div>

                        <div class="user-details">
                            <div class="user-detail-item">
                                <span class="detail-icon">📧</span>
                                <span class="detail-text"><%# Eval("email") %></span>
                            </div>
                            <div class="user-detail-item">
                                <span class="detail-icon">📱</span>
                                <span class="detail-text"><%# Eval("phonenum") %></span>
                            </div>
                            <div class="user-detail-item">
                                <span class="detail-icon">📍</span>
                                <span class="detail-text"><%# GetCity(Container.DataItem) %></span>
                            </div>
                        </div>

                        <div class="user-card-footer">
                            <asp:HyperLink ID="lnkMoreInfo" runat="server"
                                Text="פרטים נוספים"
                                NavigateUrl='<%# "exuserdetails.aspx?id=" + Eval("id") %>'
                                CssClass="user-link-btn" />
                        </div>
                    </div>
                </ItemTemplate>

            </asp:DataList>
        </div>
    </section>

    <style>
        .users-shell {
            width: min(1500px, 95%);
            margin: 40px auto 60px;
            padding: 0 20px;
        }

        .users-hero {
            text-align: center;
            margin-bottom: 40px;
        }

        .users-hero .hero-title {
            font-size: 32px;
            font-weight: 700;
            color: var(--heading);
            margin-bottom: 12px;
        }

        .users-hero .hero-description {
            font-size: 16px;
            color: var(--text);
            opacity: 0.8;
        }

        .users-search-section {
            display: flex;
            gap: 12px;
            justify-content: center;
            margin-bottom: 40px;
            max-width: 600px;
            margin-left: auto;
            margin-right: auto;
        }

        .search-input {
            flex: 1;
            padding: 12px 18px;
            border: 1px solid var(--border);
            border-radius: 8px;
            font-size: 15px;
            direction: rtl;
            background: var(--surface);
            color: var(--text);
        }

        .search-button {
            padding: 12px 28px;
            background: var(--brand);
            color: #fff;
            border: none;
            border-radius: 8px;
            font-weight: 600;
            cursor: pointer;
            transition: background .2s ease;
        }

        .search-button:hover {
            background: var(--brand-dark);
        }

        .users-grid-container {
            width: 100%;
        }

        .users-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
            gap: 24px;
            width: 100%;
        }

        .user-card-wrapper {
            width: 100%;
        }

        .user-card {
            background: var(--surface);
            border-radius: 16px;
            padding: 24px;
            box-shadow: var(--shadow-md);
            border: 1px solid var(--border);
            transition: transform .2s ease, box-shadow .2s ease;
            height: 100%;
            display: flex;
            flex-direction: column;
        }

        .user-card:hover {
            transform: translateY(-4px);
            box-shadow: var(--shadow-lg);
        }

        .user-card-header {
            display: flex;
            align-items: center;
            gap: 16px;
            margin-bottom: 20px;
            padding-bottom: 20px;
            border-bottom: 1px solid var(--border);
        }

        .user-avatar {
            width: 60px;
            height: 60px;
            border-radius: 50%;
            background: var(--brand);
            color: #fff;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 24px;
            font-weight: 700;
            flex-shrink: 0;
        }

        .user-name-section {
            flex: 1;
        }

        .user-name {
            font-size: 20px;
            font-weight: 700;
            color: var(--heading);
            margin: 0 0 4px 0;
        }

        .user-full-name {
            font-size: 14px;
            color: var(--text);
            opacity: 0.7;
            margin: 0;
        }

        .user-details {
            flex: 1;
            display: flex;
            flex-direction: column;
            gap: 12px;
            margin-bottom: 20px;
        }

        .user-detail-item {
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 14px;
        }

        .detail-icon {
            font-size: 18px;
        }

        .detail-text {
            color: var(--text);
        }

        .user-card-footer {
            margin-top: auto;
        }

        .user-link-btn {
            display: block;
            text-align: center;
            padding: 10px 20px;
            background: var(--brand);
            color: #fff;
            border-radius: 8px;
            text-decoration: none;
            font-weight: 600;
            transition: background .2s ease;
        }

        .user-link-btn:hover {
            background: var(--brand-dark);
            text-decoration: none;
        }

        @media (max-width: 768px) {
            .users-grid {
                grid-template-columns: 1fr;
            }
        }
    </style>

</asp:Content>
