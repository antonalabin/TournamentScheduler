using Library;
using System;
using System.Collections.Generic;

namespace TournamentScheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            List<DateTime> dates = new List<DateTime>();
            {
                DateTime firstDate = new DateTime(2017, 11, 6);
                DateTime lastDate = new DateTime(2017, 12, 25);
                DateTime date;
                for (int i = 0; (date = firstDate.AddDays(i)).CompareTo(lastDate) != 0; i++)
                    if ((date.DayOfWeek == DayOfWeek.Monday) || (date.DayOfWeek == DayOfWeek.Friday))
                        dates.Add(date);
                firstDate = new DateTime(2018, 1, 12);
                lastDate = new DateTime(2018, 4, 23);
                for (int i = 0; (date = firstDate.AddDays(i)).CompareTo(lastDate) != 0; i++)
                    if ((date.DayOfWeek == DayOfWeek.Monday) || (date.DayOfWeek == DayOfWeek.Friday))
                        dates.Add(date);
            }
            List<int> times = new List<int>() { 19, 20, 21, 22 };
            string[] teams = new string[12] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l" };

            Model model = new Model(teams, 2, dates, times);

            model.wishes.Add(new MaxGamesPerHour(100, 1));
            model.wishes.Add(new ToursInOrder(99));
            model.wishes.Add(new MinMaxGamesPerDay(100, 2, 4));
            model.wishes.Add(new MaxGamesPerTeamPerWeek(99, 1));
            model.wishes.Add(new MaxGamesLeadersPerDay(99, 2, new int[6] { 0, 1, 2, 3, 4, 5 }));
            model.wishes.Add(new DayOfWeekTeamPerToursWish(99, 0, DayOfWeek.Monday, 4, 3));
            model.wishes.Add(new DayOfWeekTeamPerToursWish(99, 1, DayOfWeek.Monday, 4, 3));
            model.wishes.Add(new DaysOfWeekTeamWish(99, 3, new DayOfWeek[] { DayOfWeek.Monday }));
            model.wishes.Add(new TimeSlotTeamWish(99, 3, new int[] { 2 }));
            model.wishes.Add(new TimeSlotTeamWish(99, 4, new int[] { 1, 2 }));
            model.wishes.Add(new TimeSlotTeamWish(99, 5, new int[] { 1, 2 }));
            model.wishes.Add(new DaysOfWeekTeamWish(99, 6, new DayOfWeek[] { DayOfWeek.Monday }));
            model.wishes.Add(new TimeSlotTeamWish(99, 7, new int[] { 0 }));
            model.wishes.Add(new TimeSlotTeamWish(99, 8, new int[] { 1, 2 }));

            Algorithm algo;
            Schedule schedule;

            algo = new Backtracking(new Greedy(model), 20);
            schedule = algo.Solve();

            algo = new LocalSearch(new Backtracking(new Greedy(model)), int.MaxValue)
            {
                writeProgress = true,
                timeSpan = TimeSpan.FromMinutes(1)
            };
            schedule = algo.Solve(schedule);

            Console.WriteLine();

            algo = new Genetic(new Pop(new Backtracking(new Greedy(model), 2), 2, 200), int.MaxValue)
            {
                writeProgress = true,
                timeSpan = TimeSpan.FromMinutes(3)
            };
            schedule = algo.Solve();

            Console.ReadLine();
        }
    }
}
