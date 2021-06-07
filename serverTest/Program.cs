using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Device.Gpio;
using System.Collections.Generic;
using handleClient;

namespace serverTest
{
    
   
    class Program
    {
        static public Dictionary<TcpClient, string> clientList = new Dictionary<TcpClient, string>();
            //각 클라이언트마다 리스트에 추가
        static int counter = 0; //user count

        static void Main(string[] args)
        {
            string bindIp = "0.0.0.0";
            const int bindPort = 8000;
            
            TcpListener server = null;
            TcpClient client = null;
            
           

            IPEndPoint localAddress =
                    new IPEndPoint(IPAddress.Parse(bindIp), bindPort);

                server = new TcpListener(localAddress);
                client = default(TcpClient); // 소켓 설정
                server.Start();

                Console.WriteLine("서버 시작... ");
            while (true)
            {
                try
                {
                    counter++; //Client 수 증가
                    client = server.AcceptTcpClient(); //클라이언트가 하나라도 있어야 생성 가능
                    Console.WriteLine("클라이언트 접속 : {0} ",
                        ((IPEndPoint)client.Client.RemoteEndPoint).ToString());

                    NetworkStream stream = client.GetStream();

                    int length;
                    //string data = null;
                    byte[] bytes = new byte[256];
                    length = stream.Read(bytes, 0, bytes.Length);
                    string user_name = Encoding.Unicode.GetString(bytes, 0, length);
                    user_name = user_name.Substring(0, user_name.IndexOf("$")); // client 사용자 명

                    clientList.Add(client, user_name); // cleint 리스트에 추가
                    SendMessage(user_name + " 에서 접속...", "", false); // 모든 client에게 메세지 전송
                    handle h_client = new handle(); // 클라이언트 추가
                    h_client.OnReceived += new handle.MessageDisplayHandler(OnReceived);
                    h_client.OnDisconnected += new handle.DisconnectedHandler(h_client_OnDisconnected);
                    h_client.startClient(client, clientList); 
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    break;
                }
            }
            client.Close();
            server.Stop();
            Console.WriteLine("서버를 종료합니다.");
        }

        static void h_client_OnDisconnected(TcpClient clientSocket) // cleint 접속 해제 핸들러
        {
            if (clientList.ContainsKey(clientSocket))
                clientList.Remove(clientSocket);
        }


        static private void OnReceived(string message, string user_name) // cleint로 부터 받은 데이터
        {
            if(message.Equals("leaveChat")) {
                string displayMessage = "leave user : " + user_name;
                Console.WriteLine(displayMessage + " (" + DateTime.Now.ToLongTimeString() + ")");;
                SendMessage("leaveChat", user_name, true);
            }
            else {
                string displayMessage = "From " + user_name + " : " + message;
                Console.WriteLine(displayMessage + " (" + DateTime.Now.ToLongTimeString() + ")"); // Server단에 출력
                SendMessage(message, user_name, true);
            }
        }

        static public void SendMessage(string message, string user_name, bool flag)
        {
            GpioController controller = new GpioController(PinNumberingScheme.Board);
            var pin = 12;
            controller.OpenPin(pin, PinMode.Output);

            foreach (var pair in clientList)
            {
                TcpClient client_f = pair.Key as TcpClient;
                string clientName = pair.Value;
                NetworkStream stream_f = client_f.GetStream();
                byte[] buffer_f = null;

                if(flag)
                {
                    if(message.Equals("leaveChat"))
                        buffer_f = Encoding.Unicode.GetBytes(user_name + " 에서 접속을 해제했습니다.");
                    else if (message.Equals("on"))
                    {
                        buffer_f = Encoding.Unicode.GetBytes(message);
                        controller.Write(pin,PinValue.High);
                    }    
                    else if (message.Equals("off"))
                    {
                        buffer_f = Encoding.Unicode.GetBytes(message);
                        controller.Write(pin,PinValue.Low);
                    }
                    else{
                        buffer_f = Encoding.Unicode.GetBytes(user_name + " : " + message);
                    }
                }
                else
                {
                    buffer_f = Encoding.Unicode.GetBytes(message);
                }

                if(clientName == "Server Status UI")
                {
                    stream_f.Write(buffer_f, 0, buffer_f.Length); // 버퍼 쓰기
                    stream_f.Flush();
                }
                
            }
        }
    }
}
