using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DynaBotv2
{
    public class Bot
    {
        private Socket Connection;
        public int UniqueID;
        private string Username;
        private string Password;
        private string Channel;
        public bool Running = false;
        public int LastMessage = 0;
        public Bot(Credentials C)
        {
            this.Username = C.Username;
            this.Password = C.Password;
            this.Channel = C.Channel;
            Connect();
        }

        public bool Connect()
        {
            try
            {
                Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Connection.Connect(new IPEndPoint(IPAddress.Parse(Dns.GetHostAddresses("irc.twitch.tv")[0].ToString()), 6667));
                if (Connection.Connected)
                {
                    Send("PASS " + Password + "\r\n");
                    Send("USER " + Username + " 0 * :" + Username + "\r\n");
                    Send("NICK " + Username + "\r\n");
                    Send("JOIN " + Channel + "\r\n");
                    Running = Connection.Connected;
                    return Running;
                }
            }
            catch { }
            return false;
        }
        public void Disconnect()
        {
            Connection.Shutdown(SocketShutdown.Both);
            Connection.Disconnect(true);
        }
        public bool Send(string Msg)
        {
            try
            {
                LastMessage = Environment.TickCount;
                Connection.Send(ASCIIEncoding.ASCII.GetBytes(Msg));
                return true;
            }
            catch { Running = false; return false; }
        }
        public void ListenForData()
        {
            while (true)
            {
                while (Connection.Connected)
                {
                    byte[] buffer = new byte[1204];
                    int len = Connection.Receive(buffer);
                    string data = ASCIIEncoding.ASCII.GetString(buffer);

                    if (UniqueID > 0)
                        continue;
                    string prefix;
                    string command;
                    string[] parms;
                    ParseIrcMessage(data, out prefix, out command, out parms);
                    switch (command)
                    {
                        case "001": break;
                        case "002": break;
                        case "PING": Send(data.Replace("PING", "PONG")); break;
                        case "PRIVMSG":
                            {
                                string Msg = parms[1].Replace("\r\n", "").Replace("\0", "");
                                if (Msg.StartsWith("@"))
                                {
                                    string[] msgParts = Msg.Split(' ');
                                    string from = prefix.Split('!')[0];
                                    if (MainWindow.Owner.ToLower().Equals(from.ToLower()))
                                    {
                                        switch (msgParts[0])
                                        {
                                            case "@trivia":
                                                {
                                                    if (msgParts.Length > 1)
                                                    {
                                                        switch (msgParts[1])
                                                        {
                                                            case "on":
                                                            case "start": Trivia.Start(); MainWindow.SendMessage("Trivia has now started."); break;
                                                            case "off":
                                                            case "stop": Trivia.Stop(); MainWindow.SendMessage("Trivia has now stopped."); break;
                                                            case "hint": MainWindow.SendMessage("Last Hint: " + Trivia.LastHint); break;
                                                            case "category": MainWindow.SendMessage("This option isnt enabled yet."); break;
                                                            default: break;
                                                        }
                                                    }
                                                    break;
                                                }
                                            case "@assault":
                                                {
                                                    if (msgParts.Length > 1)
                                                    {
                                                        Assault.Start(msgParts[1]);
                                                    }
                                                    break;
                                                }
                                            case "@raffle":
                                                {
                                                    if (msgParts.Length > 1)
                                                    {
                                                        switch (msgParts[1])
                                                        {
                                                            case "start": Raffle.Start(); break;
                                                            case "stop": Raffle.End(); break;
                                                            case "roll": Raffle.Roll(); break;
                                                            default: break;
                                                        }
                                                    }
                                                    break;
                                                }
                                            default: break;
                                        }
                                    }
                                    switch (msgParts[0])
                                    {
                                        case "@rank": break;
                                        case "@join": if (Raffle.Started) { Raffle.Add(from); } break;
                                        default: break;
                                    }
                                }
                                else
                                {
                                    if (Msg.ToLower().Equals(Trivia.CurrentAnswer.ToLower()))
                                        Trivia.AnswerQuestion(prefix.Split('!')[0]);
                                }
                                lock (MainWindow.MessageQueue)
                                    MainWindow.MessageQueue.Enqueue(prefix.Split('!')[0] + ": " + Msg);
                                break;
                            }
                        default: break;
                    }
                }
                #region Reconnect
                Running = false;
                for (int i = 0; i < 3; i++)
                {
                    lock (MainWindow.MessageQueue)
                        MainWindow.MessageQueue.Enqueue("Bot #" + UniqueID + " has disconnected. Attempting to reconnect. Attempt #" + (i + 1));
                    if (Connect())
                        break;
                    Thread.Sleep(5000);
                }
                if (!Running)
                    return;
                #endregion
            }
        }
        private void ParseIrcMessage(string message, out string prefix, out string command, out string[] parameters)
        {
            int prefixEnd = -1, trailingStart = message.Length;
            string trailing = null;
            prefix = command = String.Empty;
            parameters = new string[] { };
            if (message.StartsWith(":"))
            {
                prefixEnd = message.IndexOf(" ");
                prefix = message.Substring(1, prefixEnd - 1);
            }
            trailingStart = message.IndexOf(" :");
            if (trailingStart >= 0)
                trailing = message.Substring(trailingStart + 2);
            else
                trailingStart = message.Length;
            var commandAndParameters = message.Substring(prefixEnd + 1, trailingStart - prefixEnd - 1).Split(' ');
            command = commandAndParameters.First();
            if (commandAndParameters.Length > 1)
                parameters = commandAndParameters.Skip(1).ToArray();
            if (!String.IsNullOrEmpty(trailing))
                parameters = parameters.Concat(new string[] { trailing }).ToArray();
        }
    }
}
