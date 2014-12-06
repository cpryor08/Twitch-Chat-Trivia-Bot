using System;
using System.Data;
using System.Timers;
using System.Threading;
using System.Collections.Generic;
namespace DynaBotv2
{
    public abstract class Trivia
    {
        private static Dictionary<string, TrivQuestion[]> Categories;
        public static string CurrentCategory = "League of Legends";
        public static string LastHint = "";
        public static string CurrentAnswer = "";
        public static bool WaitingOnAnswer = false;
        private static int AnswerTimeStamp = 0;
        private static System.Timers.Timer Timer;
        public static int TotalCategories;
        public static int TotalQuestions;
        public static bool Running = false;
        public static void Start() { Running = true; Timer.Start(); }
        public static void Stop() { Timer.Stop(); Running = false; }
        private static string Winner = "";
        public static void AnswerQuestion(string winner)
        {
            if (!WaitingOnAnswer)
                return;
            WaitingOnAnswer = false;
            AnswerTimeStamp = Environment.TickCount;
            Winner = winner;
        }
        public static void Load()
        {
            Timer = new System.Timers.Timer();
            Timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            Timer.Interval = 1000;
            Categories = new Dictionary<string, TrivQuestion[]>();
            DataTable dt = null;
            try
            {
                dt = MainWindow.Database.GetDataTable("SELECT * FROM `Categories`");
            }
            catch (Exception e) { }
            TotalCategories = dt.Rows.Count;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                int CategoryID = int.Parse(dr.ItemArray[0].ToString());
                string CategoryName = dr.ItemArray[1].ToString();
                DataTable dt2 = MainWindow.Database.GetDataTable("SELECT * FROM `Questions` WHERE `CategoryID`='" + CategoryID + "'");
                if (dt2.Rows.Count > 0)
                {
                    Categories.Add(CategoryName, new TrivQuestion[dt2.Rows.Count]);
                    TotalQuestions += dt2.Rows.Count;
                    for (int x = 0; x < dt2.Rows.Count; x++)
                    {
                        DataRow dr2 = dt2.Rows[x];
                        Categories[CategoryName][x] = new TrivQuestion();
                        Categories[CategoryName][x].Question = dr2.ItemArray[2].ToString();
                        Categories[CategoryName][x].Answer = dr2.ItemArray[3].ToString();
                        Categories[CategoryName][x].Hint1 = dr2.ItemArray[4].ToString();
                        Categories[CategoryName][x].Hint2 = dr2.ItemArray[5].ToString();
                        Categories[CategoryName][x].Hint3 = dr2.ItemArray[6].ToString();
                    }
                }
            }
        }
        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Timer.Interval = MainWindow.QuestionInterval;
            Timer.Stop();
            int rand = MainWindow.Random.Next(0, Math.Max(0, Categories[CurrentCategory].Length - 1));
            TrivQuestion currentQuestion = Categories[CurrentCategory][rand];
            CurrentAnswer = currentQuestion.Answer;
            MainWindow.SendMessage(currentQuestion.Question);
            int currentTime = Environment.TickCount;
            WaitingOnAnswer = true;
            for (int i = 0; i < 3; i++)
            {
                for (int x = 0; x < MainWindow.HintInterval; x++)
                {
                    if (!WaitingOnAnswer)
                        break;
                    Thread.Sleep(1000);
                }
                if (!WaitingOnAnswer)
                    break;
                switch (i)
                {
                    case 0: MainWindow.SendMessage("First Hint: " + currentQuestion.Hint1); LastHint = currentQuestion.Hint1; break;
                    case 1: MainWindow.SendMessage("Second Hint: " + currentQuestion.Hint2); LastHint = currentQuestion.Hint2; break;
                    case 2: MainWindow.SendMessage("Last Hint: " + currentQuestion.Hint3); LastHint = currentQuestion.Hint3; break;
                    default: break;
                }
            }
            if (!WaitingOnAnswer)
            {
                int elapsedtime = AnswerTimeStamp - currentTime;
                DataTable userData = MainWindow.Database.GetDataTable("SELECT `points`, `fastesttime` FROM `users` WHERE `username`='" + Winner + "'");
                int currentPoints = 0;
                int fastestTime = 0;
                if (userData.Rows.Count > 0)
                {
                    currentPoints = int.Parse(userData.Rows[0].ItemArray[0].ToString());
                    fastestTime = int.Parse(userData.Rows[0].ItemArray[1].ToString());
                }
                currentPoints += 25;
                string toSend = Winner + " answered in " + elapsedtime + " milliseconds. " + Winner + " has earned a total of " + currentPoints + " points!";
                if (elapsedtime < fastestTime || fastestTime == 0)
                {
                    fastestTime = (int)elapsedtime;
                    toSend += " " + Winner + " has beat his fastest time!";
                }
                MainWindow.Database.ExecuteNonQuery("UPDATE `users` SET `points`=" + currentPoints + ", `fastesttime`=" + fastestTime + " WHERE `username`='" + Winner + "'");
                toSend += " The answer was " + currentQuestion.Answer;
                MainWindow.SendMessage(toSend);
                MainWindow.SendMessage("!add 25 " + Winner);
            }
            else
            {
                MainWindow.SendMessage("Time has ran out! The answer was " + CurrentAnswer);
            }
            Timer.Start();
        }
    }
    public class TrivQuestion
    {
        public string Question;
        public string Answer;
        public byte RewardLevel;
        public string Hint1;
        public string Hint2;
        public string Hint3;
    }
}
