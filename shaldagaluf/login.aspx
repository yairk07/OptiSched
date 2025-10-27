<%@ Page Title="Login" Language="C#" MasterPageFile="~/danimaster.master" CodeFile="login.aspx.cs"AutoEventWireup="true"Inherits="login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
    <h2>דף התחברות</h2>

    <asp:Panel runat="server">
        <div>
            <label for="txtUserName">שם משתמש:</label>
            <asp:TextBox ID="txtUserName" runat="server" CssClass="textbox" />
        </div>

        <div>
            <label for="txtPassword">סיסמה:</label>
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="textbox" />
        </div>

        <div>
            <asp:Button ID="btnLogin" runat="server" Text="היכנס" OnClick="btnLogin_Click" CssClass="button" />
        </div> 

        <asp:Label ID="lblError" runat="server" ForeColor="Red" />
    </asp:Panel>
</asp:Content>

