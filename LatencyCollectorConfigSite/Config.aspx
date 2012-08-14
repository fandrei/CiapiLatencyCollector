<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Config.aspx.cs" Inherits="LatencyCollectorConfigSite.Config" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>AppMetrics Config</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <table width="50%">
            <tr>
                <td>
                    <asp:Button ID="EnableButton" runat="server" Text="Enable polling" 
                        onclick="EnableButton_Click" />
                </td>
                <td>
                    <asp:Button ID="DisableButton" runat="server" Text="Disable polling" 
                        onclick="DisableButton_Click" />
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
