using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    private const int Port = 12345;
    private const int BufferSize = 1024;

    private UdpClient udpServer;
    private Dictionary<IPEndPoint, string> connectedClients;


    public void Start()
    {

        Console.OutputEncoding = Encoding.UTF8;
        udpServer = new UdpClient(Port);

        Console.WriteLine("TRÒ CHƠI DÒ MÌN:\n");
        Console.WriteLine("Máy chủ bắt đầu. Đang chờ kết nối...\n");
        Console.WriteLine("====================================================================\n");
        connectedClients = new Dictionary<IPEndPoint, string>();

        while (connectedClients.Count < 2)
        {
            IPEndPoint clientEndPoint = null;
            byte[] data = udpServer.Receive(ref clientEndPoint);
            string message = Encoding.UTF8.GetString(data);
            ProcessClientMessage(message, clientEndPoint);
        }
        Console.Write(Environment.NewLine);
        Console.WriteLine("====================================================================\n");
        Console.WriteLine("Tất cả người chơi đã kết nối.");
        Console.WriteLine("Bắt đầu trò chơi...\n");

        SendGameStartMessage();
        SendMinefield();

        while (true)
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedData = udpServer.Receive(ref clientEndPoint);
            string receivedMessage = Encoding.ASCII.GetString(receivedData);
            int selectedNumber;

            if (int.TryParse(receivedMessage, out selectedNumber) && selectedNumber >= 1 && selectedNumber <= 36)
            {
                if (CheckNumberAvailability(selectedNumber))
                {
                    string clientName = connectedClients[clientEndPoint];
                    Console.WriteLine("Người chơi {0} đã chọn số {1}", clientName.ToUpper(), selectedNumber);
                    byte[] response = Encoding.ASCII.GetBytes("OK");
                    udpServer.Send(response, response.Length, clientEndPoint);
                }
                else
                {
                    byte[] response = Encoding.ASCII.GetBytes("Retry");
                    udpServer.Send(response, response.Length, clientEndPoint);
                }
            }
            else
            {
                byte[] response = Encoding.ASCII.GetBytes("Retry");
                udpServer.Send(response, response.Length, clientEndPoint);
            }
        }
    }
    private static HashSet<int> selectedNumbers = new HashSet<int>();

    private static bool CheckNumberAvailability(int number)
    {
        lock (selectedNumbers)
        {
            if (selectedNumbers.Contains(number))
            {
                return false; // Số đã được chọn trước đó
            }
            else
            {
                selectedNumbers.Add(number);
                return true; // Số chưa được chọn
            }
        }
    }


    private void ProcessClientMessage(string message, IPEndPoint clientEndPoint)
    {
        if (IsValidNickname(message) && !IsNicknameTaken(message) && !ContainsSequentialCharacters(message))
        {
            connectedClients.Add(clientEndPoint, message);
            Console.WriteLine($"Người chơi thứ {connectedClients.Count} nickname: {message.ToUpper()}");
        }
        else
        {
            SendNicknameRequest(clientEndPoint);
        }
    }

    private bool IsValidNickname(string nickname)
    {
        if (string.IsNullOrEmpty(nickname) || nickname.Length > 10)
        {
            return false;
        }

        foreach (char c in nickname)
        {
            if (!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsNicknameTaken(string nickname)
    {
        foreach (var kvp in connectedClients)
        {
            if (kvp.Value.Equals(nickname, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsSequentialCharacters(string nickname)
    {
        for (int i = 0; i < nickname.Length - 1; i++)
        {
            char currentChar = nickname[i];
            char nextChar = nickname[i + 1];

            if ((char.IsDigit(currentChar) && IsSequentialDigit(currentChar, nextChar)) ||
                (char.IsLetter(currentChar) && IsSequentialLetter(currentChar, nextChar)))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSequentialDigit(char currentChar, char nextChar)
    {
        return (nextChar == currentChar + 1) && (nextChar >= '1' && nextChar <= '9');
    }

    private bool IsSequentialLetter(char currentChar, char nextChar)
    {
        return (char.ToLower(nextChar) == char.ToLower(currentChar) + 1) &&
               ((nextChar >= 'a' && nextChar <= 'z') || (nextChar >= 'A' && nextChar <= 'Z'));
    }

    private void SendNicknameRequest(IPEndPoint clientEndPoint)
    {
        byte[] data = Encoding.UTF8.GetBytes("InvalidNickname");
        udpServer.Send(data, data.Length, clientEndPoint);
    }

    private void SendGameStartMessage()
    {
        byte[] data = Encoding.UTF8.GetBytes("Bắt đầu trò chơi");

        foreach (var clientEndPoint in connectedClients.Keys)
        {
            udpServer.Send(data, data.Length, clientEndPoint);
        }
    }

    private void SendMinefield()
    {
        int[,] minefield = GenerateMinefield();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Bãi Mìn:");

        int count = 1;
        for (int i = 0; i < minefield.GetLength(0); i++)
        {
            for (int j = 0; j < minefield.GetLength(1); j++)
            {
                minefield[i, j] = count++;
                sb.AppendFormat("{0,2} ", minefield[i, j]);
            }
            sb.AppendLine();
        }

        byte[] data = Encoding.UTF8.GetBytes(sb.ToString());

        foreach (var clientEndPoint in connectedClients.Keys)
        {
            udpServer.Send(data, data.Length, clientEndPoint);
        }
    }

    private void SendInvalidPositionMessage(IPEndPoint clientEndPoint)
    {
        byte[] data = Encoding.UTF8.GetBytes("InvalidPosition");
        udpServer.Send(data, data.Length, clientEndPoint);
    }

    private int[,] GenerateMinefield()
    {
        int[,] minefield = new int[6, 6];
        Random random = new Random();

        // Đặt số lượng mìn
        int mineCount = 7;

        // Đặt mìn ngẫu nhiên
        while (mineCount > 0)
        {
            int row = random.Next(0, 6);
            int col = random.Next(0, 6);

            if (minefield[row, col] == 0)
            {
                minefield[row, col] = random.Next(1, 37); // Đặt giá trị từ 1 đến 36
                mineCount--;
            }
        }

        return minefield;
    }

    private void ProcessPlayerPosition(int position, IPEndPoint clientEndPoint)
    {
        // Xử lý vị trí của người chơi ở đây
    }
}

class Program
{
    static void Main()
    {
        Server server = new Server();
        server.Start();
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Nhập một phím bất kỳ để thoát...");
        Console.ReadKey();
    }
}