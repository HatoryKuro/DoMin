using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class client1
{
    private const int Port = 12345;
    private const int BufferSize = 1024;

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    public void Start()
    {
        Console.OutputEncoding = Encoding.UTF8;
        udpClient = new UdpClient();
        serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);

        Console.WriteLine("TRÒ CHƠI DÒ MÌN:\n");
        Console.WriteLine("====================================================================");
        Console.WriteLine("Quy định:");
        Console.WriteLine("Nickname không được dài quá 10 ký tự.");
        Console.WriteLine("Nickname được tạo thành từ các ký tự 'a'...'z', 'A'...'Z', '0'..'9'.");
        Console.WriteLine("Nickname không được trùng với người chơi khác.");
        Console.WriteLine("====================================================================\n");
        Console.WriteLine("Hãy chọn một tác vụ:");
        Console.WriteLine("1. Nhập nickname và bắt đầu trò chơi");
        Console.WriteLine("2. Thoát");
        Console.Write("Tác vụ của bạn: ");

        string task = Console.ReadLine();

        switch (task)
        {
            case "1":
                EnterNickname();
                break;
            case "2":
                Console.WriteLine("Đã chọn thoát.");
                return;
            default:
                Console.WriteLine("Tác vụ không hợp lệ. Vui lòng chọn lại.");
                Start();
                return;
        }

        Console.WriteLine("Chuẩn bị trò chơi bắt đầu....\n");
        byte[] gameStartMessage = udpClient.Receive(ref serverEndPoint);
        string gameStart = Encoding.UTF8.GetString(gameStartMessage);
        Console.WriteLine(gameStart);
        Console.Write(Environment.NewLine);

        PlayGame();
    }

    private void EnterNickname()
    {
        Console.WriteLine("\nHãy chọn nickname:");
        string nickname = Console.ReadLine();

        byte[] data = Encoding.UTF8.GetBytes(nickname);
        udpClient.Send(data, data.Length, serverEndPoint);

        bool validNickname = false;
        while (!validNickname)
        {
            byte[] response = udpClient.Receive(ref serverEndPoint);
            string message = Encoding.UTF8.GetString(response);
            if (message.Equals("InvalidNickname"))
            {
                Console.WriteLine("Nickname không hợp lệ hoặc bị trùng. Hãy chọn nickname khác:");
                nickname = Console.ReadLine();
                data = Encoding.UTF8.GetBytes(nickname);
                udpClient.Send(data, data.Length, serverEndPoint);
            }
            else
            {
                validNickname = true;
                Console.Write(Environment.NewLine);
                Console.WriteLine("Kết nối với máy chủ thành công.");
            }
        }
    }

    private void PlayGame()
    {
        try
        {
            int mineCount = 0; // Số lượng mìn đã chọn
            while (mineCount < 5)
            {
                int number = GetNumberInput();

                if (number != -1)
                {
                    string receivedMessage = SendAndReceiveData(number.ToString());
                    DisplayServerMessage(receivedMessage);

                    // Nhận thông báo và bãi mìn đã cập nhật từ server
                    string receivedMessage2 = SendAndReceiveData("Update");
                    DisplayMineField(receivedMessage2);

                    if (receivedMessage == "Mine")
                    {
                        mineCount++;
                    }
                }
                else
                {
                    Console.WriteLine("Số không hợp lệ. Vui lòng nhập lại.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Đã xảy ra lỗi: " + ex.Message);
        }
    }

    private int GetNumberInput()
    {
        Console.Write("Nhập một số từ 1 đến 36: ");
        string input = Console.ReadLine();

        if (int.TryParse(input, out int number) && number >= 1 && number <= 36)
        {
            return number;
        }

        return -1; // Trả về -1 nếu số không hợp lệ
    }

    private string SendAndReceiveData(string input)
    {
        byte[] dataToSend = Encoding.ASCII.GetBytes(input);
        udpClient.Send(dataToSend, dataToSend.Length, serverEndPoint);

        byte[] receivedData = udpClient.Receive(ref serverEndPoint);
        string receivedMessage = Encoding.ASCII.GetString(receivedData);

        return receivedMessage;
    }

    private void DisplayServerMessage(string message)
    {
        Console.WriteLine(message);
    }

    private void DisplayMineField(string mineField)
    {
        Console.WriteLine("Bãi Mìn:");
        Console.WriteLine(mineField);
    }
}

class Program
{
    static void Main()
    {
        client1 client1 = new client1();
        client1.Start();
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Nhập một phím bất kỳ để thoát...");
        Console.ReadKey();
    }
}