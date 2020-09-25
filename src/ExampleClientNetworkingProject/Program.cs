using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace ExampleClientNetworkingProject
{
    
    class Program
    {
        static class Messanger
        {
            public static Queue<KeyValuePair<string, string>> Messages;
        }
        //You can adapt the code below for communications on port 3461 and 3462.
        static void SynchronousConnection(string address, int port, string authentication)
        {
            try
            {
                while (true)
                {
                    foreach((string user, string msg) in Messanger.Messages){
                            Console.WriteLine(user + ": " + msg + "\n");
                    }
                    
                    Messanger.Messages.Clear();
                    TcpClient client = new TcpClient(address, port);

                    //A using statement should automatically flush when it goes out of scope
                    BufferedStream stream = new BufferedStream(client.GetStream());
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    //TODO: do work!
                    writer.Write(IPAddress.NetworkToHostOrder(authentication.Length));
                    writer.Write(Encoding.UTF8.GetBytes(authentication));
                    Console.Write("Say something [q = quit; enter = refresh chat]: ");

                    string new_msg = Console.ReadLine();
                    Console.Write("\n");

                    writer.Write(IPAddress.NetworkToHostOrder(new_msg.Length));
                    writer.Write(Encoding.UTF8.GetBytes(new_msg));

                    writer.Flush();
                }

            }

            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }

        }

        static void SynchronousConnection(string address, int port, string username, string password)
        {

            try
            {

                TcpClient client = new TcpClient(address, port);
                //A using statement should automatically flush when it goes out of scope
                using (BufferedStream stream = new BufferedStream(client.GetStream()))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    //TODO: do work!
                    writer.Write(IPAddress.NetworkToHostOrder(username.Length));
                    writer.Write(Encoding.UTF8.GetBytes(username));

                    writer.Write(IPAddress.NetworkToHostOrder(password.Length));
                    writer.Write(Encoding.UTF8.GetBytes(password));

                    int authkey_length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    string authkey = Encoding.UTF8.GetString(reader.ReadBytes(authkey_length));

                    Console.Write("Authentication Complete. Your key is: " + authkey + "\n" + "\n");
                    ThreadedConnection(address, 3463, username, authkey);
                    TaskedConnection(address, 3462, authkey).Wait();

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }

        }

        //A continuous connection is the best approach for communication on 3463
        public static void ContinuousConnection(string address, int port, string username, string authentication)
        {

            try
            {
                while (true)
                {
                    TcpClient client = new TcpClient(address, port);
                    BufferedStream stream = new BufferedStream(client.GetStream());
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(IPAddress.NetworkToHostOrder(authentication.Length));
                    writer.Write(Encoding.UTF8.GetBytes(authentication));
                    
                    int user_length = IPAddress.NetworkToHostOrder(reader.ReadInt32());
                    byte[] user_bytes = reader.ReadBytes(user_length);
                    string user = Encoding.UTF8.GetString(user_bytes);

                    int msg_length = IPAddress.NetworkToHostOrder(reader.ReadInt32()); 
                    byte[] msg_bytes = reader.ReadBytes(msg_length);   
                    string msg = Encoding.UTF8.GetString(msg_bytes);

                    Messanger.Messages.Enqueue(new KeyValuePair<string, string>(user, msg));
                    
                    //if you don't use a using statement, you'll need to flush manually.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }

        }

        static async Task TaskedConnection(string address, int port, string authentication)
        {
            await Task.Run(() => { SynchronousConnection(address, port, authentication); });
        }

        static async Task TaskedConnection(string address, int port, string user, string password)
        {
            await Task.Run(() => { SynchronousConnection(address, port, user, password); });
        }

        public static void ThreadedConnection(string address, int port, string username, string authentication)
        {
            ThreadStart ts = () => { ContinuousConnection(address, port, username, authentication); };
            Thread thread = new Thread(ts);
            thread.Start();

            //if you want to block until the thread is done, call join.  Otherwise, you can
            //just return
            //thread.Join();
        }
        static void Main(string[] args)
        {
            string address = null;
            string username = null;
            string password = null;



            Console.Write("Enter server IP address: ");
            address = Console.ReadLine();

            Console.Write("Enter User Name: ");
            username = Console.ReadLine();

            Console.Write("Enter Password: ");
            password = Console.ReadLine();


            try
            {
                SynchronousConnection(address, 3461, username, password);

            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
