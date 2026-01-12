<%@ Page Title="Create Database Tables" Language="C#" AutoEventWireup="true" CodeFile="createDatabaseTables.aspx.cs" Inherits="createDatabaseTables" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Create Database Tables</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
            direction: ltr;
        }
        .success {
            color: green;
            padding: 10px;
            background: #e8f5e9;
            border: 1px solid #4caf50;
            border-radius: 4px;
            margin: 10px 0;
        }
        .error {
            color: red;
            padding: 10px;
            background: #ffebee;
            border: 1px solid #f44336;
            border-radius: 4px;
            margin: 10px 0;
        }
        .info {
            color: #2196f3;
            padding: 10px;
            background: #e3f2fd;
            border: 1px solid #2196f3;
            border-radius: 4px;
            margin: 10px 0;
        }
        button {
            padding: 10px 20px;
            background: #2196f3;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
        }
        button:hover {
            background: #1976d2;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Create Database Tables</h1>
        <div id="messageDiv" runat="server"></div>
        <asp:Button ID="btnCreateTables" runat="server" Text="Create Missing Tables" OnClick="btnCreateTables_Click" />
    </form>
</body>
</html>

