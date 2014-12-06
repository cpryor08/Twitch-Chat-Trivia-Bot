using System;
using System.Threading;
using System.Collections.Generic;
namespace DynaBotv2
{
    public abstract class Assault
    {
        private static string Target = "";
        private static string oldChannel = "";
        public static string Message = "";
        public static int MessageCount = 5;
        public static int Interval = 0;
        public static void Start(string target)
        {
            Target = "#" + target;
            ThreadPool.QueueUserWorkItem(new WaitCallback(Execute));
        }
        private static void Execute(object state)
        {
            oldChannel = MainWindow.Channel;
            MainWindow.Channel = Target;
            bool TriviaRunning = Trivia.Running;
            if (TriviaRunning)
                Trivia.Stop();
            foreach (Bot B in MainWindow.Bots)
            {
                B.Disconnect();
            }
            Thread.Sleep(5000);
            for (int i = 0; i < MessageCount; i++)
            {
                MainWindow.SendMessage(Message);
                Thread.Sleep(Interval);
            }
            MainWindow.Channel = oldChannel;
            foreach (Bot B in MainWindow.Bots)
            {
                B.Disconnect();
            }
            if (TriviaRunning)
                Trivia.Start();
        }
    }
}
