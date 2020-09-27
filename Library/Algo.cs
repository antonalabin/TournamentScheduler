using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Library
{
    public abstract class Algorithm
    {
        public Model model;
        public bool writeProgress;
        public TimeSpan? timeSpan;

        public Algorithm()
        {
            writeProgress = false;
            timeSpan = null;
        }

        public abstract Schedule Solve(Schedule schedule = null);
    }

    public class Greedy : Algorithm
    {
        protected int n; // Число команд
        protected int r; // Число турнирных кругов
        protected int t; // Число туров
        protected int g; // Число игр в туре
        protected int s; // Число временных слотов в день
        protected int d; // Число дней
        protected List<Wish> wishes; // Пожелания

        public int minGamesСonsid;
        public int tr;

        public Greedy(Model model, int minGamesСonsid = 1, int tr = 300) : base()
        {
            this.model = model;
            n = model.n;
            r = model.r;
            s = model.s;
            d = model.d;
            t = r * (n - 1 + n % 2);
            g = n / 2 + n % 2;
            wishes = model.wishes;
            this.minGamesСonsid = minGamesСonsid;
            this.tr = tr;
        }

        public override Schedule Solve(Schedule schedule = null)
        {
            if (schedule == null)
                schedule = new Schedule(model);
            else
                schedule = new Schedule(schedule);
            schedule.criterion = null;
            schedule.fulfillment = null;

            Random random = new Random();

            int[] wishImportances = new int[wishes.Count];
            for (int i = 0; i < wishes.Count; i++)
            {
                if ((wishes[i] is NeedHelp) && (wishes[i].importancePercent < 100) && (random.Next(2) == 1))
                    wishImportances[i] = 100;
                else
                    wishImportances[i] = wishes[i].importancePercent;
            }

            List<int[]> currGames = new List<int[]>(0);
            List<int[]> otherGames = new List<int[]>(0);
            for (int i = 0; i < t; i++)
                for (int j = 0; j < g; j++)
                    if (schedule.y[i, j] == null)
                        otherGames.Add(new int[2] { i, j });
            if (random.Next(2) == 1)
                otherGames.Reverse();
            int minGames = minGamesСonsid;

            do
            {
                while ((currGames.Count < minGames) && (otherGames.Count > 0))
                {
                    if (!schedule.x[otherGames[0][0], otherGames[0][1], 0].HasValue)
                        SetTourRivals(schedule, otherGames[0][0]);
                    int a;
                    for (a = 1; (a < otherGames.Count) && (otherGames[a][0] == otherGames[0][0]); a++) ;
                    a = random.Next(a);
                    currGames.Add(otherGames[a]);
                    otherGames.RemoveAt(a);
                }
                bool[,,] demSuit = new bool[currGames.Count, d, s];
                int[] demSuitCoeff = new int[currGames.Count];
                for (int game = 0; game < currGames.Count; game++)
                {
                    demSuitCoeff[game] = 1;
                    for (int day = 0; day < d; day++)
                        for (int slot = 0; slot < s; slot++)
                        {
                            demSuit[game, day, slot] = true;
                            for (int wish = 0; (wish < wishes.Count) && (demSuit[game, day, slot]); wish++)
                            {
                                if ((wishImportances[wish] == 100) && (!(wishes[wish] is RivalsWish))
                                    && (!wishes[wish].Fulfilled(day, slot, currGames[game][0], currGames[game][1], schedule, model)))
                                {
                                    demSuit[game, day, slot] = false;
                                    demSuitCoeff[game]++;
                                }
                            }
                        }
                }

                int sum = 0;
                for (int i = 0; i < currGames.Count; i++)
                {
                    if (demSuitCoeff[i] == d * s + 1)
                        demSuitCoeff[i] = 0;
                    else
                        sum += demSuitCoeff[i];
                }
                if (sum > 0)
                {
                    int game = -1;
                    for (int a = random.Next(sum); a >= 0; a -= demSuitCoeff[game])
                        game++;

                    List<int[]> tourn = new List<int[]>(1);
                    {
                        List<int[]> list = new List<int[]>(1);
                        for (int day = 0; day < d; day++)
                            for (int slot = 0; slot < s; slot++)
                                if (demSuit[game, day, slot])
                                    list.Add(new int[2] { day, slot });
                        int tournSize = (list.Count * tr + 99) / 100;
                        for (int i = 0; i < tournSize; i++)
                        {
                            int a = random.Next(list.Count);
                            if (!tourn.Contains(list[a]))
                                tourn.Add(list[a]);
                        }
                    }

                    int ch = 0;
                    if (tourn.Count > 1)
                    {
                        List<int> best = new List<int>(1);
                        int max = 0;
                        for (int i = 0; i < tourn.Count; i++)
                        {
                            sum = 0;
                            for (int wish = 0; wish < wishes.Count; wish++)
                                if ((wishImportances[wish] < 100) && (!(wishes[wish] is RivalsWish)))
                                    if (wishes[wish].Fulfilled(tourn[i][0], tourn[i][1], currGames[game][0], currGames[game][1], schedule, model))
                                        sum += wishes[wish].importancePercent;
                            if (sum == max)
                                best.Add(i);
                            else if (sum > max)
                            {
                                max = sum;
                                best.Clear();
                                best.Add(i);
                            }
                        }
                        ch = best[random.Next(best.Count)];
                    }

                    schedule.y[currGames[game][0], currGames[game][1]] = tourn[ch][0];
                    schedule.z[currGames[game][0], currGames[game][1]] = tourn[ch][1];
                    currGames.RemoveAt(game);
                }
                else if (otherGames.Count > 0)
                    minGames++;
                else
                    break;
            }
            while ((currGames.Count > 0) || (otherGames.Count > 0));

            return schedule;
        }

        private void SetTourRivals(Schedule schedule, int tour)
        {
            Random random = new Random();
            int toursInRound = schedule.teams - 1 + schedule.teams % 2;
            int round = tour / toursInRound;

            if (schedule.roundsTeams[round] == null)
            {
                schedule.roundsTeams[round] = new int[schedule.teams];
                List<int> teams = new List<int>(schedule.teams);
                for (int i = 0; i < schedule.teams; i++)
                    teams.Add(i);
                for (int i = 0; i < schedule.teams; i++)
                {
                    int a = random.Next(teams.Count);
                    schedule.roundsTeams[round][i] = teams[a];
                    teams.RemoveAt(a);
                }
            }

            List<int[]> c = new List<int[]>(toursInRound);
            c.Add(new int[schedule.teams]);
            for (int j = 0; j < schedule.teams; j++)
                c[0][j] = j;
            for (int i = 1; i < toursInRound; i++)
            {
                c.Add(new int[schedule.teams]);
                for (int j = 0; j < schedule.teams; j++)
                {
                    if ((j < schedule.teams - 1) || (schedule.teams % 2 == 1))
                    {
                        if (c[i - 1][j] == 0)
                            c[i][j] = schedule.teams - 2 + schedule.teams % 2;
                        else
                            c[i][j] = c[i - 1][j] - 1;
                    }
                    else
                        c[i][j] = j;
                }
            }
            for (int i = 0; i < toursInRound; i++)
                for (int j = 0; j < schedule.teams; j++)
                    c[i][j] = schedule.roundsTeams[round][c[i][j]];

            for (int i = tour - tour % toursInRound; i < tour - tour % toursInRound + toursInRound; i++)
            {
                if (schedule.x[i, 0, 0].HasValue)
                    for (int j = 0; j < c.Count; j++)
                        for (int k = 0; k < schedule.games; k++)
                            if (((schedule.x[i, k, 0] == c[j][0]) && (schedule.x[i, k, 1] == c[j][schedule.teams - 1])) ||
                                ((schedule.x[i, k, 1] == c[j][0]) && (schedule.x[i, k, 0] == c[j][schedule.teams - 1])))
                            {
                                c.RemoveAt(j);
                                j = c.Count;
                                break;
                            }
            }
            int e = random.Next(c.Count);
            for (int i = 0; i < schedule.games; i++)
            {
                schedule.x[tour, i, 0] = c[e][i];
                schedule.x[tour, i, 1] = c[e][schedule.teams - 1 - i];
            }
        }
    }

    public class Backtracking : Algorithm
    {
        protected int iter;
        protected Algorithm algo;

        public Backtracking(Algorithm algo, int par = 2) : base()
        {
            model = algo.model;
            this.iter = par;
            this.algo = algo;
        }

        public override Schedule Solve(Schedule schedule = null)
        {
            Schedule bestSchedule = null;
            int bestCrit = int.MaxValue;

            object threadLock = new object();
            Parallel.For(0, iter, i =>
            {
                Schedule sch = algo.Solve(schedule);
                if (!sch.criterion.HasValue)
                {
                    sch.criterion = 0;
                    for (int j = 0; j < model.criteria.Count; j++)
                        sch.criterion += model.criteria[j].Value(sch) * model.criteria[j].importancePercent;
                    sch.criterion = (sch.criterion + 100 - 1) / 100;
                }

                lock (threadLock)
                {
                    if (sch.criterion < bestCrit)
                    {
                        bestCrit = (int)sch.criterion;
                        bestSchedule = sch;
                        if (writeProgress)
                            Console.WriteLine(bestCrit);
                    }
                }
            });

            return bestSchedule;
        }
    }

    public class Genetic : Algorithm
    {
        protected int iter;
        Population pop;
        public int iterToIncrMut;

        public Genetic(Population pop, int iter = 100, int iterToIncrMut = 5) : base()
        {
            this.iter = iter;
            this.pop = pop;
            model = pop.algo.model;
            this.iterToIncrMut = iterToIncrMut;
        }
        public override Schedule Solve(Schedule schedule = null)
        {
            DateTime startTime = DateTime.Now;

            if (schedule == null)
                schedule = pop.algo.Solve();
            else
                schedule = new Schedule(schedule);

            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    schedule.y[i, j] = schedule.z[i, j] = null;
            schedule.criterion = null;
            schedule.fulfillment = null;

            pop.NewPopulation(schedule);
            schedule = pop.bestSchedule;

            Console.WriteLine(schedule.criterion);
            for (int i = 0; (i < iter) && ((!timeSpan.HasValue) || (DateTime.Now - timeSpan < startTime)); i++)
            {
                if ((iterToIncrMut > 0) && (i % iterToIncrMut == 0) && (pop.mutation < 99))
                    pop.mutation++;

                pop.NewGeneration();

                if (pop.bestSchedule.criterion < schedule.criterion)
                {
                    schedule = pop.bestSchedule;

                    if (writeProgress)
                    {
                        int sum = 0;
                        for (int j = 0; j < pop.popSize; j++)
                            sum += (int)pop.p[j].criterion;
                        Console.WriteLine("{0} {1}  {2}", schedule.criterion, sum / pop.popSize, DateTime.Now - startTime);
                    }

                }
            }
            return schedule;
        }
        public Schedule Solve(List<Schedule> schedules)
        {
            DateTime startTime = DateTime.Now;

            pop.NewPopulation(schedules);
            Schedule schedule = pop.bestSchedule;

            Console.WriteLine(schedule.criterion);
            for (int i = 0; (i < iter) && ((!timeSpan.HasValue) || (DateTime.Now - timeSpan < startTime)); i++)
            {
                if ((iterToIncrMut > 0) && (i % iterToIncrMut == 0) && (pop.mutation < 99))
                    pop.mutation++;

                pop.NewGeneration();

                if (pop.bestSchedule.criterion < schedule.criterion)
                {
                    schedule = pop.bestSchedule;

                    if (writeProgress)
                    {
                        int sum = 0;
                        for (int j = 0; j < pop.popSize; j++)
                            sum += (int)pop.p[j].criterion;
                        Console.WriteLine("{0} {1} {2}  {3}", schedule.criterion, sum / pop.popSize, i, DateTime.Now - startTime);
                    }
                }
            }
            return schedule;
        }


    }

    public class LocalSearch : Algorithm
    {
        protected Algorithm algo;
        int iter;
        int num;

        public LocalSearch(Algorithm algo, int iter = 5, int num = 10) : base()
        {
            model = algo.model;
            this.algo = algo;
            this.iter = iter;
            this.num = num;
        }

        public override Schedule Solve(Schedule schedule = null)
        {
            DateTime startTime = DateTime.Now;

            if (schedule == null)
                schedule = new Schedule(model);
            if (!schedule.criterion.HasValue)
            {
                schedule.criterion = 0;
                for (int i = 0; i < model.criteria.Count; i++)
                    schedule.criterion += model.criteria[i].Value(schedule) * model.criteria[i].importancePercent;
                schedule.criterion = (schedule.criterion + 100 - 1) / 100;
            }
            if (writeProgress)
                Console.WriteLine(schedule.criterion);

            for (int i = 0; (i < iter) && ((!timeSpan.HasValue) || (DateTime.Now - timeSpan < startTime)); i++)
            {
                Schedule newSchedule = new Schedule(schedule);
                newSchedule.criterion = null;
                if (newSchedule.fulfillment == null)
                {
                    bool[,,] fulfillment = new bool[model.wishes.Count, newSchedule.tours, newSchedule.games];
                    Parallel.For(0, newSchedule.tours, j =>
                    {
                        for (int k = 0; k < newSchedule.games; k++)
                            if (newSchedule.y[j, k].HasValue)
                                for (int l = 0; l < model.wishes.Count; l++)
                                    if (model.wishes[l].importancePercent < 100)
                                        fulfillment[l, j, k] = model.wishes[l].Fulfilled((int)newSchedule.y[j, k], (int)newSchedule.z[j, k], j, k, newSchedule, model);
                    });
                    schedule.fulfillment = newSchedule.fulfillment = fulfillment;
                }
                List<int[]> list = new List<int[]>(newSchedule.tours * newSchedule.games);
                int sum = 0;
                for (int j = 0; j < newSchedule.tours; j++)
                    for (int k = 0; k < newSchedule.games; k++)
                    {
                        if (newSchedule.y[j, k].HasValue)
                        {
                            list.Add(new int[3] { j, k, 100 });
                            for (int l = 0; l < model.wishes.Count; l++)
                                if ((model.wishes[l].importancePercent < 100) && (newSchedule.fulfillment[l, j, k] == false))
                                    list[list.Count - 1][2] += model.wishes[l].importancePercent;
                            sum += list[list.Count - 1][2];
                        }
                    }
                newSchedule.fulfillment = null;
                Random random = new Random();
                for (int j = 0; (j < num) && (list.Count > 0); j++)
                {
                    int game = -1;
                    for (int a = random.Next(sum); a >= 0; a -= list[game][2])
                        game++;
                    newSchedule.y[list[game][0], list[game][1]] = newSchedule.z[list[game][0], list[game][1]] = null;
                    sum -= list[game][2];
                    list.RemoveAt(game);
                }

                newSchedule = algo.Solve(newSchedule);

                if (!newSchedule.criterion.HasValue)
                {
                    newSchedule.criterion = 0;
                    for (int j = 0; j < model.criteria.Count; j++)
                        newSchedule.criterion += model.criteria[j].Value(newSchedule) * model.criteria[j].importancePercent;
                    newSchedule.criterion = (newSchedule.criterion + 100 - 1) / 100;
                }
                if (newSchedule.criterion <= schedule.criterion)
                {
                    if (writeProgress && (newSchedule.criterion < schedule.criterion))
                        Console.WriteLine(newSchedule.criterion + " " + (DateTime.Now - startTime));
                    schedule = newSchedule;
                }
            }
            if (schedule.fulfillment == null)
            {
                bool[,,] fulfillment = new bool[model.wishes.Count, schedule.tours, schedule.games];
                Parallel.For(0, schedule.tours, j =>
                {
                    for (int k = 0; k < schedule.games; k++)
                        if (schedule.y[j, k].HasValue)
                            for (int l = 0; l < model.wishes.Count; l++)
                                if (model.wishes[l].importancePercent < 100)
                                    fulfillment[l, j, k] = model.wishes[l].Fulfilled((int)schedule.y[j, k], (int)schedule.z[j, k], j, k, schedule, model);
                });
                schedule.fulfillment = fulfillment;
            }
            return schedule;
        }
    }
}
