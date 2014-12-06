using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace DynaBotv2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static Random Random;
        public static Database Database;
        public static Queue<string> MessageQueue = new Queue<string>();
        public static Queue<string> RaffleQueue = new Queue<string>();
        public static List<Bot> Bots = new List<Bot>();
        public const string CommandOperator = "@";
        public static string Channel = "twelvek";
        new public static string Owner = "Twelvek";
        public static int QuestionInterval = 40000;
        public static int HintInterval = 20;
        private static BackgroundWorker MessageWorker = new BackgroundWorker();
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Database = new DynaBotv2.Database();
            Random = new System.Random(Environment.TickCount);
            #region Load Settings
            IniFile F = new IniFile("Settings.ini");
            Owner = F.ReadString("Default", "Owner");
            Channel = "#" + F.ReadString("Default", "Channel");
            Trivia.CurrentCategory = F.ReadString("Default", "Category");
            QuestionInterval = F.ReadInt32("Default", "QuestionInterval") * 1000;
            HintInterval = F.ReadInt32("Default", "HintInterval");
            Assault.Message = F.ReadString("Assault", "Message");
            Assault.MessageCount = F.ReadInt32("Assault", "MessageCount");
            Assault.Interval = F.ReadInt32("Assault", "MessageDelay");
            #endregion
            #region Load Trivia
            Trivia.Load();
            lock (MessageQueue)
                MessageQueue.Enqueue("Loaded " + Trivia.TotalCategories + " categories and " + Trivia.TotalQuestions + " questions.");
            #endregion
            MessageWorker.DoWork += new DoWorkEventHandler(bw_DoWork);
            MessageWorker.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            MessageWorker.WorkerReportsProgress = true;
            MessageWorker.RunWorkerAsync();
            #region Load Bots
            DataTable dt = null;
            dt = Database.GetDataTable("SELECT * FROM `Bots` WHERE `InUse`=1");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                Credentials C = new Credentials();
                C.Username = dr[1].ToString();
                C.Password = dr[3].ToString();
                C.Channel = Channel;
                Bot B = new Bot(C);
                B.UniqueID = i;
                if (B.Running)
                {
                    Bots.Add(B);
                    lock (MessageQueue)
                        MessageQueue.Enqueue("Loaded bot #" + i + " " + C.Username + " : " + C.Password + " : " + C.Channel + " successfully.");
                }
            }
            lock (MessageQueue)
                MessageQueue.Enqueue("Loaded " + Bots.Count + " bots successfully.");
            #endregion

            foreach (Bot B in Bots)
                new Thread(new ThreadStart(B.ListenForData)).Start();
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                MessageWorker.ReportProgress(0);
                Thread.Sleep(1000);
            }
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            while (MessageQueue.Count > 0)
            {
                lock (MessageQueue)
                {
                    string Msg = MessageQueue.Dequeue();
                    listBox1.Items.Add(Msg);
                    int itemCount = listBox1.Items.Count - 1;
                    if (itemCount > -1)
                        listBox1.ScrollIntoView(listBox1.Items[itemCount]);
                }
            }
            while (RaffleQueue.Count > 0)
            {
                lock (RaffleQueue)
                {
                    string Part = RaffleQueue.Dequeue();
                    listBox2.Items.Add(Part);
                }
            }
        }
        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text != "")
            {
                listBox1.Items.Add(textBox1.Text);

                if (listBox1.Items.Count >= 100)
                    listBox1.Items.RemoveAt(0);

                int itemCount = listBox1.Items.Count - 1;
                if (itemCount > -1)
                    listBox1.ScrollIntoView(listBox1.Items[itemCount]);

                Bots[0].Send(textBox1.Text);

                textBox1.Text = "";
            }
        }
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (textBox1.Text != "")
                {
                    listBox1.Items.Add(textBox1.Text);

                    if (listBox1.Items.Count >= 100)
                        listBox1.Items.RemoveAt(0);

                    int itemCount = listBox1.Items.Count - 1;
                    if (itemCount > -1)
                        listBox1.ScrollIntoView(listBox1.Items[itemCount]);

                    Bots[0].Send(textBox1.Text);

                    textBox1.Text = "";
                }
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        public static void SendMessage(string Message)
        {
            Bot B = null;
            long quickestTime = 999999999;
            foreach (Bot bot in Bots)
            {
                if (B == null)
                {
                    B = bot;
                    quickestTime = bot.LastMessage;
                }
                else
                {
                    if (bot.LastMessage < quickestTime)
                    {
                        B = bot;
                        quickestTime = bot.LastMessage;
                    }
                }
            }
            if (Environment.TickCount - quickestTime < 5000)
            {
                lock (MessageQueue)
                    MessageQueue.Enqueue("All bots have recently sent a message, waiting 20 seconds to prevent lockup.");
                Thread.Sleep(20000);
            }
            if (B != null)
            {
                if (B.Send("PRIVMSG " + Channel + " :" + Message + "\r\n"))
                {
                    lock (MessageQueue)
                        MessageQueue.Enqueue(Message);
                }
                else
                {
                    SendMessage(Message);
                }
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Raffle.Start();
            button2.IsEnabled = false;
            button3.IsEnabled = true;
            MessageBox.Show("A raffle has now been started.");
        }
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            button2.IsEnabled = true;
            button3.IsEnabled = false;
            Raffle.End();
        }
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            Raffle.Roll();
            MessageBox.Show("The winner is " + Raffle.LastWinner + "!");
        }
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            Raffle.Participants.Clear();
            listBox2.Items.Clear();
        }
        private void listBox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void button6_Click(object sender, RoutedEventArgs e)
        {
            Trivia.Start();
            button6.IsEnabled = false;
            button7.IsEnabled = true;
        }
        private void button7_Click(object sender, RoutedEventArgs e)
        {
            Trivia.Stop();
            button7.IsEnabled = false;
            button6.IsEnabled = true;
        }
        private void button8_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This option is not available yet.");
        }
        private void button9_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This option is not available yet.");
        }

        private void button10_Click(object sender, RoutedEventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.twitch.tv/kraken/streams/" + MainWindow.Channel.Replace("#", ""));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(resStream);
            string sdata = sr.ReadToEnd();
            sdata = sdata.Replace(@"\", "").Replace("_", "");
            JsonSerializer js = new JsonSerializer();

        }
        public class KeyValue
        {
            public string key;
            public string value;
        }
    }
}
