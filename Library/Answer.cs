using System;

namespace Library
{
    public class Schedule
    {
        public int teams;
        public int rounds;
        public int tours;
        public int games;
        public int?[,,] x;
        public int?[,] y;
        public int?[,] z;
        public int[][] roundsTeams;
        public int? criterion;
        public bool[,,] fulfillment;

        public Schedule(int teams, int rounds, int tours, int games)
        {
            this.teams = teams;
            this.rounds = rounds;
            this.tours = tours;
            this.games = games;
            x = new int?[tours, games, 2];
            y = new int?[tours, games];
            z = new int?[tours, games];
            for (int i = 0; i < tours; i++)
                for (int j = 0; j < games; j++)
                    x[i, j, 0] = x[i, j, 1] = y[i, j] = z[i, j] = null;
            roundsTeams = new int[rounds][];
            for (int i = 0; i < rounds; i++)
                roundsTeams[i] = null;
            criterion = null;
            fulfillment = null;
        }
        public Schedule(Model model)
        {
            teams = model.n;
            rounds = model.r;
            tours = rounds * (teams - 1 + teams % 2);
            games = teams / 2 + teams % 2;
            x = new int?[tours, games, 2];
            y = new int?[tours, games];
            z = new int?[tours, games];
            for (int i = 0; i < tours; i++)
                for (int j = 0; j < games; j++)
                    x[i, j, 0] = x[i, j, 1] = y[i, j] = z[i, j] = null;
            roundsTeams = new int[rounds][];
            for (int i = 0; i < rounds; i++)
                roundsTeams[i] = null;
            criterion = null;
            fulfillment = null;
        }
        public Schedule(Schedule schedule)
        {
            teams = schedule.teams;
            rounds = schedule.rounds;
            tours = schedule.tours;
            games = schedule.games;
            x = new int?[tours, games, 2];
            y = new int?[tours, games];
            z = new int?[tours, games];
            for (int i = 0; i < tours; i++)
                for (int j = 0; j < games; j++)
                {
                    x[i, j, 0] = schedule.x[i, j, 0];
                    x[i, j, 1] = schedule.x[i, j, 1];
                    y[i, j] = schedule.y[i, j];
                    z[i, j] = schedule.z[i, j];
                }
            roundsTeams = new int[rounds][];
            for (int i = 0; i < rounds; i++)
                if (schedule.roundsTeams[i] != null)
                {
                    roundsTeams[i] = new int[teams];
                    for (int j = 0; j < teams; j++)
                        roundsTeams[i][j] = schedule.roundsTeams[i][j];
                }
            criterion = schedule.criterion;
            fulfillment = schedule.fulfillment;
        }
    }

    public class Answer
    {
        public Schedule schedule;
        protected string[] teams;
        protected DateTime[,] timeSlots;

        public Answer(Schedule schedule, Model model)
        {
            this.schedule = new Schedule(schedule);
            this.teams = (string[])model.teams.Clone();
            this.timeSlots = new DateTime[model.d, model.s];
            for (int i = 0; i < model.d; i++)
                for (int j = 0; j < model.s; j++)
                    timeSlots[i, j] = model.dates[i].AddHours(model.times[j]);
        }

        public Game this[int i, int j]
        {
            get
            {
                if ((i < schedule.tours) && (j < schedule.games))
                {
                    Game game = new Game();
                    if (schedule.x[i, j, 0] != null)
                        game.teams = new string[2] { teams[(int)schedule.x[i, j, 0]], teams[(int)schedule.x[i, j, 1]] };
                    else
                        game.teams = null;
                    if (schedule.y[i, j] != null)
                        game.DateTime = timeSlots[(int)schedule.y[i, j], (int)schedule.z[i, j]];
                    else
                        game.DateTime = null;
                    return game;
                }
                else
                    return null;
            }
        }
    }

    public class Game
    {
        public string[] teams;
        public DateTime? DateTime;
    }
}
