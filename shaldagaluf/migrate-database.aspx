<%@ Page Language="C#" AutoEventWireup="true" CodeFile="migrate-database.aspx.cs" Inherits="migrate_database" ResponseEncoding="utf-8" ContentType="text/html; charset=utf-8" %>

<!DOCTYPE html>
<html>
<head>
    <title>Database Migration - OptiSched</title>
    <meta charset="utf-8" />
</head>
<body>
    <h1>Database Migration to DSD Schema</h1>
    <p>This page will migrate your database to match the DSD structure.</p>
    <p><strong>WARNING:</strong> Make sure to backup your database before running this migration!</p>
    <form runat="server">
        <asp:Button ID="btnMigrate" runat="server" Text="Run Migration" OnClick="btnMigrate_Click" />
        <br /><br />
        <asp:Label ID="lblResult" runat="server" />
    </form>
</body>
</html>


