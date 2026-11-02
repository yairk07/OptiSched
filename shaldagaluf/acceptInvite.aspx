<%@ Page Title="הצטרפות להזמנה" Language="C#" MasterPageFile="~/danimaster.master" AutoEventWireup="true" CodeFile="acceptInvite.aspx.cs" Inherits="acceptInvite" Culture="he-IL" UICulture="he-IL" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <section class="auth-section">
        <div class="auth-card">
            <div class="auth-info">
                <span class="hero-eyebrow">Invite</span>
                <h2>הצטרפות לטבלה משותפת</h2>
                <p>קישור הזמנה מצוות OptiSched</p>
            </div>
            <div class="auth-form">
                <asp:Label ID="lblMessage" runat="server" CssClass="auth-error" />
                <asp:HyperLink ID="lnkCalendar" runat="server" Visible="false" CssClass="button" Text="עבור לטבלה" />
            </div>
        </div>
    </section>
</asp:Content>
