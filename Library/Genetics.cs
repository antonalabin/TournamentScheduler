using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library
{
    public abstract class Population
    {
        public List<Schedule> p;
        public int popSize;
        public Schedule bestSchedule;
        public Algorithm algo;

        public int tournSize; // Размер турнира
        public int elite; // Численность элиты        
        public int culled; // Численность брака
        public int mutation; // Вероятность мутации

        public abstract List<Schedule> Selection(List<Schedule> p, int n);
        public abstract void NewPopulation(List<Schedule> schedules);
        public abstract void NewPopulation(Schedule schedule);
        public abstract List<Schedule> Crossover(Schedule a, Schedule b, int n);
        public abstract Schedule Mutation(Schedule a);
        public abstract void NewGeneration();
    }

    public class Pop : Population
    {
        Random random;
        public Algorithm LocalSearch;
        public int crossWeight;
        public int tour1;

        public Pop(Algorithm algo, int tournSize = 2, int popSize = 200, int initMutation = 3, int mutWeight = 14, int crossWeight = 20, int tour1 = 75, int elitePerc = 60, int culledPerc = 35)
        {
            this.popSize = popSize;
            this.mutation = initMutation;
            this.algo = algo;
            random = new Random();
            LocalSearch = new LocalSearch(algo, 1, mutWeight);
            this.tournSize = tournSize;
            elite = popSize * elitePerc / 100;
            culled = popSize * culledPerc / 100;
            this.crossWeight = crossWeight;
            this.tour1 = tour1;
        }

        public override void NewPopulation(List<Schedule> schedules)
        {
            p = new List<Schedule>(popSize);
            for (int i = 0; i < popSize; i++)
                p.Add(new Schedule(schedules[i]));
            bestSchedule = p[0];
            for (int i = 0; i < popSize; i++)
            {
                if (!p[i].criterion.HasValue)
                {
                    p[i].criterion = 0;
                    for (int j = 0; j < algo.model.criteria.Count; j++)
                        p[i].criterion += algo.model.criteria[j].Value(p[i]) * algo.model.criteria[j].importancePercent;
                    p[i].criterion = (p[i].criterion + 100 - 1) / 100;
                }
                if (p[i].criterion < bestSchedule.criterion)
                    bestSchedule = p[i];
            }
        }

        public override void NewPopulation(Schedule schedule)
        {
            if (schedule == null)
                schedule = algo.Solve();
            else
                schedule = new Schedule(schedule);
            if (!schedule.criterion.HasValue)
            {
                schedule.criterion = 0;
                for (int i = 0; i < algo.model.criteria.Count; i++)
                    schedule.criterion += algo.model.criteria[i].Value(schedule) * algo.model.criteria[i].importancePercent;
                schedule.criterion = (schedule.criterion + 100 - 1) / 100;
            }
            bestSchedule = new Schedule(schedule);
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    schedule.y[i, j] = schedule.z[i, j] = null;
            schedule.criterion = null;
            schedule.fulfillment = null;

            p = new List<Schedule>(popSize);

            for (int i = 0; i < popSize; i++)
            {
                p.Add(algo.Solve(schedule));

                if (!p[i].criterion.HasValue)
                {
                    p[i].criterion = 0;
                    for (int j = 0; j < algo.model.criteria.Count; j++)
                        p[i].criterion += algo.model.criteria[j].Value(p[i]) * algo.model.criteria[j].importancePercent;
                    p[i].criterion = (p[i].criterion + 100 - 1) / 100;
                }
                if (p[i].criterion < bestSchedule.criterion)
                    bestSchedule = p[i];
            }
        }

        public override List<Schedule> Selection(List<Schedule> p, int n)
        {
            List<Schedule> list = new List<Schedule>(n);
            List<Schedule> schedules = new List<Schedule>(p);
            for (int i = 0; i < n; i++)
            {
                Schedule temp;
                int best = 0;
                List<int> bst = new List<int>(1);
                int ra;
                for (int j = 0; (j < tournSize) && (j < schedules.Count); j++)
                {
                    ra = random.Next(schedules.Count - j);
                    temp = schedules[ra];
                    schedules[ra] = schedules[schedules.Count - 1 - j];
                    schedules[schedules.Count - 1 - j] = temp;
                    if (temp.criterion < schedules[schedules.Count - 1 - best].criterion)
                    {
                        best = j;
                        bst.Clear();
                        bst.Add(j);
                    }
                    else if ((temp.criterion == schedules[schedules.Count - 1 - best].criterion))
                        bst.Add(j);
                }
                best = bst[random.Next(bst.Count)];
                list.Add(schedules[schedules.Count - 1 - best]);
                schedules.RemoveAt(schedules.Count - 1 - best);
            }
            return list;
        }
        public override List<Schedule> Crossover(Schedule a, Schedule b, int n)
        {
            List<Schedule> children = new List<Schedule>(n);
            Schedule[] parents = new Schedule[2] { a, b };

            for (int i = 0; i < n; i++)
            {
                if (random.Next(2) == 0)
                    parents.Reverse<Schedule>();

                for (int q = 0; q < 2; q++)
                    if (parents[q].fulfillment == null)
                    {
                        bool[,,] fulfillment = new bool[algo.model.wishes.Count, a.tours, a.games];
                        Parallel.For(0, a.tours, j =>
                        {
                            for (int k = 0; k < a.games; k++)
                                if (parents[q].y[j, k].HasValue)
                                    for (int l = 0; l < algo.model.wishes.Count; l++)
                                        if (algo.model.wishes[l].importancePercent < 100)
                                            fulfillment[l, j, k] = algo.model.wishes[l].Fulfilled((int)parents[q].y[j, k], (int)parents[q].z[j, k], j, k, parents[q], algo.model);
                        });
                        parents[q].fulfillment = fulfillment;
                    }

                List<int[]> matches = new List<int[]>(a.tours * a.games);
                for (int j = 0; j < a.tours; j++)
                    for (int k = 0; k < a.games; k++)
                    {
                        if (parents[1].y[j, k].HasValue)
                        {
                            if (parents[0].y[j, k].HasValue)
                            {
                                if ((parents[0].y[j, k] != parents[1].y[j, k]) || (parents[0].z[j, k] != parents[1].z[j, k]))
                                {
                                    matches.Add(new int[3] { j, k, 0 });
                                    for (int l = 0; l < algo.model.wishes.Count; l++)
                                        if ((algo.model.wishes[l].importancePercent < 100) && (parents[0].fulfillment[l, j, k]))
                                            matches[matches.Count - 1][2] -= algo.model.wishes[l].importancePercent;
                                }
                                else
                                    continue;
                            }
                            else
                                matches.Add(new int[3] { j, k, 0 });

                            for (int l = 0; l < algo.model.wishes.Count; l++)
                                if (algo.model.wishes[l].importancePercent < 100)
                                {
                                    matches[matches.Count - 1][2] += algo.model.wishes[l].importancePercent;
                                    if (parents[1].fulfillment[l, j, k])
                                        matches[matches.Count - 1][2] += algo.model.wishes[l].importancePercent;
                                }
                        }
                    }

                Schedule res = new Schedule(a);
                for (int j = 0; j < res.tours; j++)
                    for (int k = 0; k < res.games; k++)
                        res.y[j, k] = res.z[j, k] = null;
                res.criterion = null;
                res.fulfillment = null;
                int v = (matches.Count * crossWeight + 99) / 100;
                for (int j = 0; (j < v) && (matches.Count > 0); j++)
                {
                    int max = 0;
                    List<int> list = new List<int>(1);
                    int tr = (matches.Count * tour1 + 99) / 100;
                    for (int k = 0; k < tr; k++)
                    {
                        int c = random.Next(matches.Count);
                        if (matches[c][2] > max)
                        {
                            max = matches[c][2];
                            list.Clear();
                            list.Add(c);
                        }
                        else if (matches[c][2] == max)
                            list.Add(c);
                    }
                    int f = list[random.Next(list.Count)];
                    res.y[matches[f][0], matches[f][1]] = parents[1].y[matches[f][0], matches[f][1]];
                    res.z[matches[f][0], matches[f][1]] = parents[1].z[matches[f][0], matches[f][1]];
                    matches.RemoveAt(f);
                }
                for (int j = 0; j < res.tours; j++)
                    for (int k = 0; k < res.games; k++)
                        if ((!res.y[j, k].HasValue) && (parents[0].y[j, k].HasValue))
                        {
                            res.y[j, k] = parents[0].y[j, k];
                            res.z[j, k] = parents[0].z[j, k];
                            for (int l = 0; l < algo.model.wishes.Count; l++)
                                if ((algo.model.wishes[l].importancePercent == 100)
                                    && (!algo.model.wishes[l].Fulfilled((int)res.y[j, k], (int)res.z[j, k], j, k, res, algo.model)))
                                {
                                    res.y[j, k] = res.z[j, k] = null;
                                    break;
                                }
                        }

                children.Add(algo.Solve(res));
            }

            return children;
        }

        public override Schedule Mutation(Schedule a)
        {
            return LocalSearch.Solve(a);
        }

        public override void NewGeneration()
        {
            p.Sort((x, y) => ((int)y.criterion).CompareTo((int)x.criterion));

            List<Schedule> allParents = new List<Schedule>(p);
            allParents.RemoveRange(0, culled);

            while (p.Count < 2 * popSize - elite)
            {
                if ((random.Next(100) < mutation) || (allParents.Count == 1))
                    p.Add(Mutation(Selection(allParents, 1)[0]));
                else
                {
                    List<Schedule> parents = Selection(allParents, 2);
                    p.AddRange(Crossover(parents[0], parents[1], 1));
                }

                for (int k = 0; k < p.Count - 1; k++)
                {
                    bool same = false;
                    if (p[k].criterion == p[p.Count - 1].criterion)
                    {
                        same = true;
                        for (int i = 0; i < bestSchedule.tours; i++)
                            for (int j = 0; j < bestSchedule.games; j++)
                                if ((p[k].y[i, j] != p[p.Count - 1].y[i, j]) || (p[k].z[i, j] != p[p.Count - 1].z[i, j]))
                                {
                                    i = bestSchedule.tours;
                                    same = false;
                                    break;
                                }
                    }
                    if (same)
                    {
                        p.RemoveAt(p.Count - 1);
                        break;
                    }
                }
            }

            p.RemoveRange(0, popSize - elite);

            for (int i = elite; i < p.Count; i++)
            {
                if (!p[i].criterion.HasValue)
                {
                    p[i].criterion = 0;
                    for (int j = 0; j < algo.model.criteria.Count; j++)
                        p[i].criterion += algo.model.criteria[j].Value(p[i]) * algo.model.criteria[j].importancePercent;
                    p[i].criterion = (p[i].criterion + 100 - 1) / 100;
                }
                if (p[i].criterion < bestSchedule.criterion)
                    bestSchedule = p[i];
            }
        }
    }
}
