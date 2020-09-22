using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text;

namespace ExampleClientNetworkingProject
{
    class Program
    {
        //You can adapt the code below for communications on port 3461 and 3462.
        static void SynchronousConnection(string address, int port)
        {
            try
            {
                TcpClient client = new TcpClient(address, port);

                //A using statement should automatically flush when it goes out of scope
                using(BufferedStream stream = new BufferedStream(client.GetStream()))
                {
                    BinaryReader reader = new BinaryReader(stream);
                    BinaryWriter writer = new BinaryWriter(stream);

                    //TODO: do work!
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }
        }

        //A continuous connection is the best approach for communication on 3463
        public static void ContinuousConnection(string address, int port)
        {
            TcpClient client;
            BufferedStream stream;
            BinaryReader reader;
            BinaryWriter writer;
            try
            {
                client = new TcpClient(address, port);
                stream = new BufferedStream(client.GetStream());
                reader = new BinaryReader(stream);
                writer = new BinaryWriter(stream);

                //if you don't use a using statement, you'll need to flush manually.
                stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: {0}", ex.Message);
                throw ex;
            }
            while(true)
            {
                //TODO: do work!
            }
        }

        static async Task TaskedConnection(string address, int port)
        {
            await Task.Run(() => { SynchronousConnection(address, port); });
        }

        public static void ThreadedConnection(string address, int port)
        {
            ThreadStart ts = () => { ContinuousConnection(address, port); } ;
            Thread thread = new Thread(ts);
            thread.Start();

            //if you want to block until the thread is done, call join.  Otherwise, you can
            //just return
            //thread.Join();
        }
        static void Main(string[] args)
        {
            string address;
            string username;
            string password;
            
            BinaryWriter auth_writer = null;
            BinaryReader auth_reader = null;
            TcpClient auth_client = null;

            BinaryWriter rec_writer = null;
            BinaryReader rec_reader = null;
            TcpClient rec_client = null;

            BinaryWriter send_writer = null;
            BinaryReader send_reader = null;
            TcpClient send_client = null;

            
            Console.Write("Enter server IP address: ");
            address = Console.ReadLine();

            Console.Write("Enter User Name: ");
            username = Console.ReadLine();

            Console.Write("Enter Password: ");
            password = Console.ReadLine();

           try{
               auth_client = new TcpClient(address, 3461);
               BufferedStream auth_stream = new BufferedStream(auth_client.GetStream());
               auth_writer = new BinaryWriter(auth_stream);
               auth_reader = new BinaryReader(auth_stream);

               auth_writer.Write(IPAddress.NetworkToHostOrder(username.Length));
               auth_writer.Write(Encoding.UTF8.GetBytes(username));

               auth_writer.Write(IPAddress.NetworkToHostOrder(password.Length));
               auth_writer.Write(Encoding.UTF8.GetBytes(password));

               int accesskey_length = IPAddress.NetworkToHostOrder(auth_reader.ReadInt32());
               string accesskey = Encoding.UTF8.GetString(auth_reader.ReadBytes(accesskey_length));
               
               send_client = new TcpClient(address, 3462);
               BufferedStream send_stream = new BufferedStream(send_client.GetStream());
               send_writer = new BinaryWriter(send_stream);
               send_reader = new BinaryReader(send_stream);

               send_writer.Write(IPAddress.NetworkToHostOrder(accesskey_length));
               send_writer.Write(Encoding.UTF8.GetBytes(accesskey));
               
               rec_client = new TcpClient(address, 3463);
               BufferedStream rec_stream = new BufferedStream(rec_client.GetStream());
               rec_writer = new BinaryWriter(rec_stream);
               rec_reader = new BinaryReader(rec_stream);


               Console.WriteLine(accesskey);
               Console.WriteLine("Authentication complete.");




           }    

           catch(Exception e){
              Console.WriteLine(e.Message);
           }
        }
    }
}
