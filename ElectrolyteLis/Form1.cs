using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace ElectrolyteLis
{
    public partial class Form1 : Form
    {
        Thread server;
        String[] months = { "ETC", "Jan", "Feb", "March", "April", "May", "June", "July", "Aug", "Sep", "Oct", "Nov", "Dec" };
        public volatile bool stopserver = false;
        string ipadd = "";
        string foldername = "";
        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    textBoxIpaddress.Text =  ip.ToString();
                }
            }

            /**/
            linkLabel.Text = Properties.Settings.Default.DefaultPath.ToString();
            /**/
            foldername = linkLabel.Text.ToString();
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            buttonSelect.Enabled = false;
            ipadd = textBoxIpaddress.Text.ToString();
            server = new Thread(serverthread);
            server.Start();


        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Socket sk = (Socket)e.Argument;
            String path ="EA_Server\\";
            char[] charsToTrim = { '\0', '.', ' ','\n','\r' };
            byte[] b = new byte[1024];
            string clientIPAddress = sk.LocalEndPoint.ToString();
            int k = sk.Receive(b);
            //b[k - 1] = 0;

            /**/
            String tempStr = System.Text.Encoding.UTF8.GetString(b);
            tempStr = tempStr.TrimEnd(charsToTrim);
            if (tempStr.Contains("END"))
            {
                sk.Close();
                return;
            }
            /**/
            
            DateTime localDate = DateTime.Now;
            int year = localDate.Year;
            int month = localDate.Month;
            int dayofmonth = localDate.Day;
            path += year.ToString() + "\\";
            path += months[month] + "\\";
            path += dayofmonth.ToString();
            path = foldername + path;

            /**/
            String headerStr = "Date Time,Sample Type,PID,Patient Name,Results";
            /**/

            ASCIIEncoding asen = new ASCIIEncoding();
            string result = "";
            result = System.Text.Encoding.UTF8.GetString(b);
            
            result = result.TrimEnd(charsToTrim);

            /**/
            int pos1 = result.IndexOf(',');
            String writeBuffer = result.Substring(pos1+1);
            String typeStr = result.Substring(0, pos1);
            String fileName = null;

            bool writeHeaderFlag = false;

            switch(typeStr)
            {
                case "RESULT":
                    fileName = "PatientRecords.csv";
                    break;

                case "REPORT":
                    fileName = "Report.csv";
                    break;

                default:
                    fileName = "Test.csv";
                    break;
            }
            /**/

            if (Directory.Exists(path))            
            {
                if (File.Exists(path + "\\" + fileName) == false)
                    writeHeaderFlag = true;

                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(path+"\\"+ fileName, true))
                {
                    /*Header*/
                    if(writeHeaderFlag)
                    {
                        writeHeaderFlag = false;
                        file.WriteLine(headerStr);
                        file.Flush();
                    }                    
                    /**/

                    file.WriteLine(writeBuffer);
                    file.Flush();
                    file.Close();
                }
                backgroundWorker1.ReportProgress(1, "Data Received from: "+ clientIPAddress);
            }
            else
            {
                Directory.CreateDirectory(path);

                    using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(path + "\\"+ fileName, true))
                {
                    /*Header*/
                    file.WriteLine(headerStr);
                    file.Flush();
                    /**/

                    file.WriteLine(writeBuffer);
                    file.Flush();
                    file.Close();
                }
                backgroundWorker1.ReportProgress(1, "Data Received from: " + clientIPAddress);
            }
            sk.Send(asen.GetBytes("ACK\r\n"));
            sk.Close();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                toolStripStatus.Text = e.UserState.ToString();

            }
            catch(Exception)
            {

            }            
        }

        private void serverthread()
        {
            try
            {

                IPAddress ipAd = IPAddress.Parse(ipadd);
                TcpListener myList = new TcpListener(ipAd, 3000);
                myList.Start();
                //("Waiting for a connection.....");
                stopserver = true;
                while (stopserver)
                {
                    Invoke(new Action(() => { toolStripStatus.Text = "Server Waiting for Connection:..."; }));
                    Socket s = myList.AcceptSocket();
                    backgroundWorker1.RunWorkerAsync(s);
                }
                myList.Stop();
            }
            catch (Exception )
            {
                //Console.WriteLine("Error..... " + e.StackTrace);
            }Thread.Sleep(2000);
            Invoke(new Action(()=>{ this.Close(); }));
            
        }

        private void buttonFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                linkLabel.Text = folderBrowserDialog1.SelectedPath;
                foldername = folderBrowserDialog1.SelectedPath;

                Properties.Settings.Default.DefaultPath = foldername;
                Properties.Settings.Default.Save();
            }
        }


        private void buttonExit_Click(object sender, EventArgs e)
        {
            StopServerThread();
            
        }

        private void label4_Click(object sender, EventArgs e)
        {
            String path = String.Empty;
            DateTime localDate = DateTime.Now;
            int year = localDate.Year;
            int month = localDate.Month;
            int dayofmonth = localDate.Day;
            path += year.ToString() + "\\";
            path += months[month] + "\\";
            path += dayofmonth.ToString();

            path = foldername + path;
            if (Directory.Exists(path))
            {
                Process.Start(path);
                try
                {
                    //+ "\\Patientdata.csv"
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = path + "\\Patientdata.csv",
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception we)
                {
                    MessageBox.Show(we.Message.ToString());
                }

            }


        }

        public  void StopServerThread()
        {
            if (!stopserver)
            {
                this.Close();
                return;
            }
            stopserver = false;
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];

            // Connect to a remote device.  
            try
            {
   
                IPAddress ipAd = IPAddress.Parse(ipadd);
                IPEndPoint remoteEP = new IPEndPoint(ipAd, 3000);
                // Create a TCP/IP  socket.  
                Socket sender = new Socket(ipAd.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    //Console.WriteLine("Socket connected to {0}",
                    //    sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.  
                    byte[] msg = Encoding.ASCII.GetBytes("END\r\n");

                    // Send the data through the socket.  
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);
                    //Console.WriteLine("Echoed test = {0}",
                    //    Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException )
                {
                    //Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException )
                {
                    //Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception )
                {
                    //Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception )
            {
                //Console.WriteLine(e.ToString());
            }
        }
    }
}
