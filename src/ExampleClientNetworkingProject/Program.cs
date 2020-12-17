using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Mime;


namespace ExampleClientNetworkingProject
{

    class Program
    {

        public System.Net.Mail.AttachmentCollection Attachments { get; }
        static class MsgHistory
        {
            public static Queue<Tuple<string, string, string>> History = new Queue<Tuple<string, string, string>>();
        }

        public class Messenger
        {
            public Queue<Tuple<string, string, string>> Messages = new Queue<Tuple<string, string, string>>();

        }



        //You can adapt the code below for communications on port 3461 and 3462.
        static void SynchronousConnection(string address, int port, string authentication, Messenger messenger)
        {
            try
            {

                while (true)
                {
                    foreach (var msg in messenger.Messages)
                    {
                        Console.WriteLine(msg.Item3 + " " + msg.Item1 + ": " + msg.Item2);
                    }
                    messenger.Messages.Clear();

                    TcpClient client = new TcpClient(address, port);
                    BufferedStream stream = new BufferedStream(client.GetStream());
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    //TODO: do work!
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    writer.Write(IPAddress.NetworkToHostOrder(authentication.Length));
                    writer.Write(Encoding.UTF8.GetBytes(authentication));
                    Console.Write("Say something [q = quit; enter = refresh chat]: ");

                    string new_msg = Console.ReadLine();
                    Console.Write("\n");
                    writer.Write(IPAddress.NetworkToHostOrder(new_msg.Length));
                    writer.Write(Encoding.UTF8.GetBytes(new_msg));

                    writer.Flush();
                    

                    if (new_msg == "q" || new_msg == "Q")
                    {

                        Console.WriteLine("\n" + "Thank you for chatting, chat will be exported to a text file.");
                        string file = ("ChatHistory" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt");

                        using (TextWriter tw = new StreamWriter(file))
                        {
                            foreach (Tuple<string, string, string> msg in MsgHistory.History)
                            {
                                tw.WriteLine(msg.Item3 + "\n" + msg.Item1 + ": " + msg.Item2 + "\n");
                            }

                        }
                        Console.WriteLine("Export Complete. Where should we email this?");
                        string email = Console.ReadLine();
                        EmailResults(email, file);
                        Thread.Sleep(2000);
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                       
                        client.Dispose();
                        writer.Dispose();
                        reader.Dispose();
                        Environment.Exit(0);
                    }

                    Thread.Sleep(1500);
                }

            }

            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;

            }

        }



        static void StartConnection(string address, int port, string username, string password)
        {

            try
            {
                Messenger messenger = new Messenger();

                TcpClient client = new TcpClient(address, port);
                //A using statement should automatically flush when it goes out of scope
                using (BufferedStream stream = new BufferedStream(client.GetStream()))
                {

                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    //TODO: do work!
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    writer.Write(IPAddress.NetworkToHostOrder(username.Length));
                    writer.Write(Encoding.UTF8.GetBytes(username));

                    writer.Write(IPAddress.NetworkToHostOrder(password.Length));
                    writer.Write(Encoding.UTF8.GetBytes(password));

                    int authkey_length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    string authkey = Encoding.UTF8.GetString(reader.ReadBytes(authkey_length));

                    Console.Write("Authentication Complete. Your key is: " + authkey + "\n");
                    Console.Write("Quitting will export the chat to a text file." + "\n");

                    TaskedConnection(address, 3462, authkey, messenger).Wait(1);
                    ThreadedConnection(address, 3463, authkey, messenger);



                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }

        }

        //A continuous connection is the best approach for communication on 3463
        public static void ContinuousConnection(string address, int port, string authentication, Messenger messenger)
        {


            try
            {
                TcpClient client = new TcpClient(address, port);
                BufferedStream stream = new BufferedStream(client.GetStream());
                BinaryReader reader = new BinaryReader(stream);
                BinaryWriter writer = new BinaryWriter(stream);

                while (true)
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    writer.Write(IPAddress.NetworkToHostOrder(authentication.Length));
                    writer.Write(Encoding.UTF8.GetBytes(authentication));

                    int user_length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] user_bytes = reader.ReadBytes(user_length);
                    string user = Encoding.UTF8.GetString(user_bytes);

                    int msg_length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] msg_bytes = reader.ReadBytes(msg_length);
                    string msg = Encoding.UTF8.GetString(msg_bytes);

                    string time = DateTime.Now.ToString("HH:mm:ss tt");

                    messenger.Messages.Enqueue(new Tuple<string, string, string>(user, msg, time));
                    MsgHistory.History.Enqueue(new Tuple<string, string, string>(user, msg, time));


                    writer.Flush();
                    Thread.Sleep(100);
                    //if you don't use a using statement, you'll need to flush manually.
                }

            }

            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }

        }

        static async Task TaskedConnection(string address, int port, string authentication, Messenger messenger)
        {
            await Task.Run(() => { SynchronousConnection(address, port, authentication, messenger); });
        }


        public static void ThreadedConnection(string address, int port, string authentication, Messenger messenger)
        {
            ThreadStart ts = () => { ContinuousConnection(address, port, authentication, messenger); };
            Thread thread = new Thread(ts);
            thread.Start();

            //if you want to block until the thread is done, call join.  Otherwise, you can
            //just return
            thread.Join();
        }

        public static void EmailResults(string email, string file)
        {
            string sender = "final346pro@gmail.com";
            var senderEmail = new MailAddress(sender, "Stephen's Final Project Email");
            var pass = "ABC!123!";


            var receiverEmail = new MailAddress(email, "User");
            var sub = "Chat History";
            Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);

            var body = "Your chat history is attached below.";
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(senderEmail.Address, pass)
            };
            using (var mess = new MailMessage(senderEmail, receiverEmail)
            {
                Subject = sub,
                Body = body,
            })
                try
                {
                    mess.Attachments.Add(data);
                    smtp.Send(mess);

                    Console.WriteLine("Email sent!");

                }

                catch (Exception ex)
                {
                    Console.WriteLine("Error sending email: {0}", ex.Message);
                    throw;
                }
        }
        static void Main(string[] args)
        {
            var rand = new Random();
            int rInt = rand.Next(0,999);
            string address = "127.0.0.1";
            string username = null;
            string password = rInt.ToString();

            Console.Write("Enter User Name: ");
            username = Console.ReadLine();

            try
            {
                //clear messages for new user


                StartConnection(address, 3461, username, password);

            }

            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }

        }
    }
}
