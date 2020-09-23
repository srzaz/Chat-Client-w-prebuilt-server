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
        
        //You can adapt the code below for communications on port 3461 and 3462.
        static void SynchronousConnection(string address, int port, int authentication)
        {
            

                try
                {

                    TcpClient client = new TcpClient(address,port);
                    
                    //A using statement should automatically flush when it goes out of scope
                    using(BufferedStream stream = new BufferedStream(client.GetStream()))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        BinaryWriter writer = new BinaryWriter(stream);
                        //TODO: do work!
                        writer.Write(IPAddress.NetworkToHostOrder(authentication));
                        string authkey = Encoding.UTF8.GetString(reader.ReadBytes(authentication));
                        writer.Write(Encoding.UTF8.GetBytes(authkey));
                        

                        
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine("error: {0}", ex.Message);
                    throw ex;
                }
            
        }

        static void SynchronousConnection(string address, int port, string username,string password)
        {
            
                try
                {

                    TcpClient client = new TcpClient(address,port);
                    
                    
                    //A using statement should automatically flush when it goes out of scope
                    using(BufferedStream stream = new BufferedStream(client.GetStream()))
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
                        
                        Console.Write("Authentication Complete.");
                        TaskedConnection(address, 3462, authkey_length).Wait();
                        ThreadedConnection(address, 3463, username, authkey_length);
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine("error: {0}", ex.Message);
                    throw ex;
                }
            
        }
        
        //A continuous connection is the best approach for communication on 3463
        public static void ContinuousConnection(string address, int port, string username,int authentication)
        {
             Queue<string> Messages = new Queue<string>();
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
                foreach(string message in Messages){
                    Console.Write(username + ": " + message);
                }
                Console.Write("");
                Messages.Clear();
                string new_msg = Console.ReadLine();
                Messages.Enqueue(new_msg);

                writer.Write(IPAddress.NetworkToHostOrder(authentication));
                string authkey = Encoding.UTF8.GetString(reader.ReadBytes(authentication));
                writer.Write(Encoding.UTF8.GetBytes(authkey));

                writer.Write(IPAddress.NetworkToHostOrder(new_msg.Length));
                writer.Write(Encoding.UTF8.GetBytes(new_msg));

                writer.Flush();

                //TODO: do work!

            }
        }

        static async Task TaskedConnection(string address, int port,  int authentication)
        {
            await Task.Run(() => { SynchronousConnection(address, port, authentication ); });
        }

         static async Task TaskedConnection(string address, int port, string user, string password)
        {
            await Task.Run(() => { SynchronousConnection(address, port, user, password ); });
        }
        
        public static void ThreadedConnection(string address,  int port, string username, int authentication)
        {
            ThreadStart ts = () => { ContinuousConnection(address, port, username, authentication); } ;
            Thread thread = new Thread(ts);
            thread.Start();
            
            //if you want to block until the thread is done, call join.  Otherwise, you can
            //just return
            thread.Join();
        }
        static void Main(string[] args)
        {
            string address;
            string username;
            string password;
            
            

            
            Console.Write("Enter server IP address: ");
            address = Console.ReadLine();

            Console.Write("Enter User Name: ");
            username = Console.ReadLine();

            Console.Write("Enter Password: ");
            password = Console.ReadLine();


           try{
               
                TaskedConnection(address, 3461, username, password).Wait();
                
               




           }    

           catch(Exception e){
              Console.WriteLine(e.Message);
           }
        }
    }
}
