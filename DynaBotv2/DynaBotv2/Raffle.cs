using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynaBotv2
{
    public static class Raffle
    {
        public static string LastWinner = "";
        public static List<string> Participants;
        public static bool Started = false;
        public static void Start()
        {
            Participants = new List<string>();
            Started = true;
            MainWindow.SendMessage("A raffle has now started! Type in @join to join in!");
        }
        public static void End()
        {
            Started = false;
            MainWindow.SendMessage("The raffle is no longer accepting any participants.");
        }
        public static void Roll()
        {
            if (Participants != null)
            {
                if (Participants.Count > 0)
                {
                    Random R = new Random(Environment.TickCount);
                    int x = R.Next(0, Participants.Count - 1);
                    LastWinner = Participants[x];
                    MainWindow.SendMessage("The raffle winner was " + Participants[x] + "! Congratulations.");
                }
            }
        }
        public static void Add(string Participant)
        {
            Participants.Add(Participant);
            lock (MainWindow.RaffleQueue)
                MainWindow.RaffleQueue.Enqueue(Participant);
        }
    }
}
