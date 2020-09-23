using System;
using System.Collections.Generic;
using System.Linq;

namespace Library
{
    public class Model
    {
        public int n
        {
            get { return teams.Length; }
        }
        public int r; // Число турнирных кругов
        public int d
        {
            get { return dates.Count; }
        }
        public int s
        {
            get { return times.Count; }
        }
        public List<DateTime> dates;
        public List<int> times;
        public List<Wish> wishes;
        public List<Criterion> criteria;
        public string[] teams;

        public Model(string[] teams, int rounds, List<DateTime> dates, List<int> times)
        {
            this.teams = (string[])teams.Clone();
            r = rounds;
            this.dates = new List<DateTime>(dates);
            this.times = new List<int>(times);
            wishes = new List<Wish>();
            criteria = new List<Criterion>();
            criteria.Add(new GeneralCriterion(this));
        }
    }

    public abstract class Wish
    {
        public int importancePercent;

        public Wish(int importancePercent)
        {
            this.importancePercent = importancePercent;
        }

        public abstract bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model);
        public abstract int NumOfViolations(Schedule schedule, Model model);
    }
    public abstract class TeamWish : Wish
    {
        public int team;

        public TeamWish(int importancePercent, int team) : base(importancePercent)
        {
            this.team = team;
        }
    }
    public abstract class RivalsWish : Wish
    {
        public RivalsWish(int importancePercent) : base(importancePercent)
        {
        }
    }
    public interface NeedHelp
    {
    }

    public interface AlgoHelp
    {
    }

    public class ToursInOrder : Wish, NeedHelp
    {
        public ToursInOrder(int importancePercent) : base(importancePercent)
        {
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            int a = day * model.s + slot;
            int b = model.d * model.s * tour / schedule.tours;
            int c = model.d * model.s * (tour + 1) / schedule.tours - 1;
            if (tour > 0)
            {
                int max = -1;
                int i;
                for (i = 0; i < schedule.games; i++)
                    if ((schedule.y[tour - 1, i].HasValue) && (schedule.z[tour - 1, i].HasValue))
                    {
                        int d = (int)schedule.y[tour - 1, i] * model.s + (int)schedule.z[tour - 1, i];
                        if (d > max)
                            max = d;
                    }
                if (max > -1)
                    b = (int)max;
            }
            if (tour < schedule.tours - 1)
            {
                int min = int.MaxValue;
                int i;
                for (i = 0; i < schedule.games; i++)
                    if ((schedule.y[tour + 1, i].HasValue) && (schedule.z[tour + 1, i].HasValue))
                    {
                        int d = (int)schedule.y[tour + 1, i] * model.s + (int)schedule.z[tour + 1, i];
                        if (d < min)
                            min = d;
                    }
                if (min < int.MaxValue)
                    c = min;
            }
            return ((a >= b) && (a <= c));
        }

        public override int NumOfViolations(Schedule schedule, Model model)
        {
            List<int[]>[,] vs = new List<int[]>[schedule.tours, schedule.games];
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    vs[i, j] = new List<int[]>();
            for (int tour = 0; tour < schedule.tours; tour++)
                for (int game = 0; game < schedule.games; game++)
                    if (schedule.y[tour, game].HasValue)
                        for (int i = tour + 1; i < schedule.tours; i++)
                            for (int j = 0; j < schedule.games; j++)
                                if (schedule.y[i, j].HasValue)
                                    if ((schedule.y[tour, game] > schedule.y[i, j])
                                        || ((schedule.y[tour, game] == schedule.y[i, j]) && (schedule.z[tour, game] > schedule.z[i, j])))
                                    {
                                        vs[tour, game].Add(new int[2] { i, j });
                                        vs[i, j].Add(new int[2] { tour, game });
                                    }
            int[] tg = new int[2];
            int value = 0;
            for (int max = 0; ; max = 0)
            {
                for (int i = 0; i < schedule.tours; i++)
                    for (int j = 0; j < schedule.games; j++)
                        if (vs[i, j].Count > max)
                        {
                            max = vs[i, j].Count;
                            tg[0] = i;
                            tg[1] = j;
                        }
                if (max > 0)
                {
                    value++;
                    for (int i = 0; i < vs[tg[0], tg[1]].Count; i++)
                    {
                        int[] a = vs[tg[0], tg[1]][i];
                        for (int k = 0; k < vs[a[0], a[1]].Count; k++)
                            if ((vs[a[0], a[1]][k][0] == tg[0]) && (vs[a[0], a[1]][k][1] == tg[1]))
                                vs[a[0], a[1]].RemoveAt(k);
                    }
                    vs[tg[0], tg[1]] = new List<int[]>();
                }
                else
                    break;
            }
            return value;
        }
    }

    public class ToursInOrder1 : Wish
    {
        public ToursInOrder1(int importancePercent) : base(importancePercent)
        {
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            List<int> list = new List<int>(0);
            for (int i = 0; i < schedule.games; i++)
                if ((schedule.y[tour, i].HasValue) && (i != gameInTour))
                    list.Add((int)schedule.y[tour, i] * model.s + (int)schedule.z[tour, i]);
            int a = day * model.s + slot;
            if (list.Count == 0)
            {
                for (int i = 0; i < schedule.tours; i++)
                {
                    if (i != tour)
                    {
                        bool before = false;
                        bool after = false;
                        for (int j = 0; j < schedule.games; j++)
                            if (schedule.y[i, j].HasValue)
                            {
                                if (((int)schedule.y[i, j] * model.s + (int)schedule.z[i, j]) < a)
                                    before = true;
                                else if (((int)schedule.y[i, j] * model.s + (int)schedule.z[i, j]) > a)
                                    after = true;
                            }
                        if (before && after)
                            return false;
                    }

                }
                return true;
            }
            else
            {
                list.Sort(); // проверить
                for (int i = 0; i < schedule.tours; i++)
                    if (i != tour)
                    {
                        for (int j = 0; j < schedule.games; j++)
                            if (schedule.y[i, j].HasValue)
                            {
                                int v = (int)schedule.y[i, j] * model.s + (int)schedule.z[i, j];
                                if (((v > a) && (v <= list[0]))
                                    || ((v < a) && (v >= list[list.Count - 1])))
                                    return false;
                            }
                    }
                return true;
            }
        }

        public override int NumOfViolations(Schedule schedule, Model model)
        {
            // сделать. искать большие кучки и относительно них ошибки


            List<int[]>[,] vs = new List<int[]>[schedule.tours, schedule.games];
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    vs[i, j] = new List<int[]>();
            for (int tour = 0; tour < schedule.tours; tour++)
                for (int game = 0; game < schedule.games; game++)
                    if (schedule.y[tour, game].HasValue)
                        for (int i = tour + 1; i < schedule.tours; i++)
                            for (int j = 0; j < schedule.games; j++)
                                if (schedule.y[i, j].HasValue)
                                    if ((schedule.y[tour, game] > schedule.y[i, j])
                                        || ((schedule.y[tour, game] == schedule.y[i, j]) && (schedule.z[tour, game] > schedule.z[i, j])))
                                    {
                                        vs[tour, game].Add(new int[2] { i, j });
                                        vs[i, j].Add(new int[2] { tour, game });
                                    }
            int[] tg = new int[2];
            int value = 0;
            for (int max = 0; ; max = 0)
            {
                for (int i = 0; i < schedule.tours; i++)
                    for (int j = 0; j < schedule.games; j++)
                        if (vs[i, j].Count > max)
                        {
                            max = vs[i, j].Count;
                            tg[0] = i;
                            tg[1] = j;
                        }
                if (max > 0)
                {
                    value++;
                    for (int i = 0; i < vs[tg[0], tg[1]].Count; i++)
                    {
                        int[] a = vs[tg[0], tg[1]][i];
                        for (int k = 0; k < vs[a[0], a[1]].Count; k++)
                            if ((vs[a[0], a[1]][k][0] == tg[0]) && (vs[a[0], a[1]][k][1] == tg[1]))
                                vs[a[0], a[1]].RemoveAt(k);
                    }
                    vs[tg[0], tg[1]] = new List<int[]>();
                }
                else
                    break;
            }
            return value;
        }
    }

    public class ToursInOrder2 : Wish, NeedHelp
    {
        public ToursInOrder2(int importancePercent) : base(importancePercent)
        {
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            List<int[]> list = new List<int[]>(0);
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    if ((schedule.y[i, j].HasValue) && ((i != tour) || (j != gameInTour)))
                        list.Add(new int[2] { model.s * (int)schedule.y[i, j] + (int)schedule.z[i, j], i });
            list.OrderBy(match => match[0]);
            List<List<int>> ll = new List<List<int>>(0);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i][1] == tour)
                {
                    if ((i == 0) || ((i > 0) && (list[i - 1][1] != tour)))
                        ll.Add(new List<int>(1) { i });
                    else
                        ll[ll.Count - 1].Add(i);
                }
            }
            if (ll.Count == 0)
            {
                if (list.Count == 0)
                    return true;

                if (model.s * day + slot <= list[0][0])
                    return true;
                for (int i = 0; i < list.Count - 1; i++)
                {
                    if ((list[i][1] != list[i + 1][1])
                        && (model.s * day + slot >= list[i][0]) && (model.s * day + slot <= list[i + 1][0]))
                        return true;
                }
                if (model.s * day + slot >= list[list.Count - 1][0])
                    return true;
                return false;
            }
            else
            {
                int max = 0;
                for (int i = 0; i < ll.Count; i++)
                {
                    if (ll[i].Count > max)
                    {
                        max = ll[i].Count;
                        ll[i].RemoveRange(0, i);
                        i = 0;
                    }
                    else if (ll[i].Count < max)
                    {
                        ll[i].RemoveAt(i);
                        i--;
                    }
                }
                for (int i = 0; i < ll.Count; i++)
                {
                    int q;
                    if (ll[i][0] == 0)
                        q = 0;
                    else
                        q = list[ll[i][0] - 1][0];
                    int w;
                    if (ll[i][ll[i].Count - 1] == list.Count - 1)
                        w = model.s * model.d - 1;
                    else
                        w = list[ll[i][ll[i].Count - 1] + 1][0];
                    if ((model.s * day + slot >= q) && (model.s * day + slot <= w))
                        return true;
                }
                return false;
            }
        }

        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            for (int j = 0; j < schedule.tours; j++)
                for (int k = 0; k < schedule.games; k++)
                {
                    if (schedule.y[j, k].HasValue)
                    {
                        if (!Fulfilled((int)schedule.y[j, k], (int)schedule.z[j, k], j, k, schedule, model))
                            sum++;
                    }
                    else
                        sum++;
                }
            return sum;
        }
    }

    // проверено
    public class MaxGamesPerTeamPerWeek : Wish
    {
        int num;
        public MaxGamesPerTeamPerWeek(int importancePercent, int num) : base(importancePercent)
        {
            this.num = num;
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            int dayOfWeek = (int)model.dates[day].DayOfWeek;
            if (dayOfWeek == 0)
                dayOfWeek = 7;
            dayOfWeek--;
            int[] sum = new int[2] { 0, 0 };
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    for (int k = 0; k < 2; k++)
                        if (((schedule.x[i, j, 0] == schedule.x[tour, gameInTour, k]) || (schedule.x[i, j, 1] == schedule.x[tour, gameInTour, k]))
                            && ((i != tour) || (j != gameInTour)))
                            if (schedule.y[i, j].HasValue)
                            {
                                int a = dayOfWeek + (int)((model.dates[(int)schedule.y[i, j]] - (model.dates[day])).TotalDays);
                                if ((a >= 0) && (a <= 6))
                                    sum[k]++;
                            }
            for (int i = 0; i < 2; i++)
                if (sum[i] >= num)
                    return false;
            return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int value = 0;
            int[,] numsGamesPerDay = new int[model.n, model.d];
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    if (schedule.y[i, j].HasValue)
                        for (int k = 0; k < 2; k++)
                            numsGamesPerDay[(int)schedule.x[i, j, k], (int)schedule.y[i, j]]++;
                    else
                        value += 2;

            for (int i = 0; i < model.n; i++)
            {
                int sum = numsGamesPerDay[i, 0];
                for (int j = 1; j < model.d; j++)
                {
                    if ((model.dates[j - 1].DayOfWeek != DayOfWeek.Sunday)
                        && (((int)model.dates[j - 1].DayOfWeek + (model.dates[j] - model.dates[j - 1]).TotalDays) < 8))
                        sum += numsGamesPerDay[i, j];
                    else
                    {
                        if (sum > num)
                            value += sum - num;
                        sum = numsGamesPerDay[i, j];
                    }
                }
                if (sum > num)
                    value += sum - num;
            }
            return value;
        }
    }
    // проверено
    public class MaxGamesLeadersPerDay : Wish
    {
        int num;
        int[] teams;

        public MaxGamesLeadersPerDay(int importancePercent, int num, int[] teams) : base(importancePercent)
        {
            this.num = num;
            this.teams = teams;
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((teams.Contains((int)schedule.x[tour, gameInTour, 0])) && (teams.Contains((int)schedule.x[tour, gameInTour, 1])))
            {
                int sum = 0;
                for (int i = 0; i < schedule.tours; i++)
                    for (int j = 0; j < schedule.games; j++)
                        if ((schedule.y[i, j] == day) && ((i != tour) || (j != gameInTour))
                            && (teams.Contains((int)schedule.x[i, j, 0])) && (teams.Contains((int)schedule.x[i, j, 1])))
                            sum++;
                return (sum < num);
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int no = 0;
            int[] amounts = new int[model.d];
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    if ((teams.Contains((int)schedule.x[i, j, 0])) && (teams.Contains((int)schedule.x[i, j, 1])))
                    {
                        if (schedule.y[i, j].HasValue)
                            amounts[(int)schedule.y[i, j]]++;
                        else
                            no++;
                    }

            int sum = 0;
            int max = 0;
            for (int i = 0; i < model.d; i++)
            {
                if (amounts[i] > num)
                    sum += amounts[i] - num;
                if (amounts[i] > max)
                    max = amounts[i];
            }
            if (max > num)
                max = num;
            if (max + no > num)
                sum += max + no - num;

            return sum;
        }
    }
    public class LeadersWish : RivalsWish
    {
        int[] teams;
        int[] tours;

        public LeadersWish(int importancePercent, int[] teams, int[] tours) : base(importancePercent)
        {
            this.teams = teams;
            this.tours = tours;
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            for (int i = 0; i < 2; i++)
            {
                int j;
                for (j = 0; j < teams.Length; j++)
                    if (schedule.x[tour, gameInTour, i] == teams[j])
                        break;
                if (j == teams.Length)
                    return true;
            }
            for (int i = 0; i < tours.Length; i++)
                if (tour == tours[i])
                    return true;
            return false;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            for (int j = 0; j < schedule.tours; j++)
                for (int k = 0; k < schedule.games; k++)
                {
                    if (schedule.y[j, k].HasValue)
                    {
                        int[] temp = new int[] { (int)schedule.y[j, k], (int)schedule.z[j, k] };
                        schedule.y[j, k] = schedule.z[j, k] = null;
                        if (!Fulfilled(temp[0], temp[1], j, k, schedule, model))
                            sum++;
                        schedule.y[j, k] = temp[0];
                        schedule.z[j, k] = temp[1];
                    }
                    else
                        sum++;
                }
            return sum;
        }
    }

    // проверено
    public class MaxGamesPerHour : Wish
    {
        int num;
        public MaxGamesPerHour(int importancePercent, int num) : base(importancePercent)
        {
            this.num = num;
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            int sum = 0;
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    if ((schedule.y[i, j] == day) && (schedule.z[i, j] == slot) && ((i != tour) || (j != gameInTour)))
                        sum++;
            return (sum < num);
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            int[,] amounts = new int[model.d, model.s];
            int no = 0;

            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    if (schedule.y[i, j].HasValue)
                        amounts[(int)schedule.y[i, j], (int)schedule.z[i, j]]++;
                    else
                        no++;

            int max = 0;
            for (int i = 0; (i < model.d) && (max < num); i++)
                for (int j = 0; j < model.s; j++)
                    if (amounts[i, j] > max)
                    {
                        if (amounts[i, j] > num)
                            max = num;
                        else
                            max = amounts[i, j];
                    }
            if (no + max - num > 0)
                sum += no + max - num;

            for (int i = 0; i < model.d; i++)
                for (int j = 0; j < model.s; j++)
                {
                    amounts[i, j] -= num;
                    if (amounts[i, j] > 0)
                        sum += amounts[i, j];
                }

            return sum;
        }
    }

    // Проверено
    public class MinMaxGamesPerDay : Wish
    {
        int min;
        int max;
        public MinMaxGamesPerDay(int importancePercent, int min, int max) : base(importancePercent)
        {
            this.min = min;
            this.max = max;
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            int[] sum = new int[model.d];
            int nul = 0;
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                {
                    if ((schedule.y[i, j].HasValue) && ((i != tour) || (j != gameInTour)))
                        sum[(int)schedule.y[i, j]]++;
                    else
                        nul++;
                }
            if (sum[day] >= max)
                return false;
            else if (sum[day] >= min)
            {
                int a = 0;
                for (int i = 0; i < model.d; i++)
                    if ((sum[i] > 0) && (sum[i] < min))
                        a += min - sum[i];
                return (nul > a);
            }
            else if (sum[day] > 0)
                return true;
            else
            {
                int a = 0;
                for (int i = 0; i < model.d; i++)
                    if ((sum[i] > 0) && (sum[i] < min))
                        a += min - sum[i];
                a += min - 1;
                return (nul > a);
            }
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int result = 0;
            int[] sum = new int[model.d];
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                {
                    if (schedule.y[i, j].HasValue)
                        sum[(int)schedule.y[i, j]]++;
                    else
                        result++;
                }

            for (int i = 0; i < model.d; i++)
            {
                if (sum[i] > max)
                    result += sum[i] - max;
                else if ((sum[i] > 0) && (sum[i] < min))
                    result += min - sum[i];
            }
            return result;
        }
    }
    public class DayTeamWish : TeamWish
    {
        int[] days;
        bool wish;
        public DayTeamWish(int importancePercent, int team, int[] days, bool wish = true) : base(importancePercent, team)
        {
            this.days = days;
            this.wish = wish;
        }
        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((schedule.x[tour, gameInTour, 0] == team) || (schedule.x[tour, gameInTour, 1] == team))
            {
                for (int i = 0; i < days.Length; i++)
                    if (days[i] == day)
                        if (wish)
                            return true;
                        else
                            return false;
                if (wish)
                    return false;
                else
                    return true;
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            for (int j = 0; j < schedule.tours; j++)
                for (int k = 0; k < schedule.games; k++)
                {
                    if (schedule.y[j, k].HasValue)
                    {
                        int[] temp = new int[] { (int)schedule.y[j, k], (int)schedule.z[j, k] };
                        schedule.y[j, k] = schedule.z[j, k] = null;
                        if (!Fulfilled(temp[0], temp[1], j, k, schedule, model))
                            sum++;
                        schedule.y[j, k] = temp[0];
                        schedule.z[j, k] = temp[1];
                    }
                    else
                        sum++;
                }
            return sum;
        }
    }

    // проверено
    public class DaysOfWeekTeamWish : TeamWish
    {
        DayOfWeek[] daysOfWeek;
        bool wish;
        public DaysOfWeekTeamWish(int importancePercent, int team, DayOfWeek[] daysOfWeek, bool wish = true) : base(importancePercent, team)
        {
            this.daysOfWeek = daysOfWeek;
            this.wish = wish;
        }
        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((schedule.x[tour, gameInTour, 0] == team) || (schedule.x[tour, gameInTour, 1] == team))
            {
                for (int i = 0; i < daysOfWeek.Length; i++)
                    if (daysOfWeek[i] == model.dates[day].DayOfWeek)
                        return wish;
                return !wish;
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            if (schedule.fulfillment != null)
            {
                int numWish = model.wishes.IndexOf(this);
                for (int j = 0; j < schedule.tours; j++)
                    for (int k = 0; k < schedule.games; k++)
                        if ((schedule.x[j, k, 0] == team) || (schedule.x[j, k, 1] == team))
                        {
                            if (schedule.y[j, k].HasValue)
                            {
                                if (!schedule.fulfillment[numWish, j, k])
                                    sum++;
                            }
                            else
                                sum++;
                            break;
                        }
            }
            else
            {
                for (int j = 0; j < schedule.tours; j++)
                    for (int k = 0; k < schedule.games; k++)
                        if ((schedule.x[j, k, 0] == team) || (schedule.x[j, k, 1] == team))
                        {
                            if (schedule.y[j, k].HasValue)
                            {
                                if (!Fulfilled((int)schedule.y[j, k], (int)schedule.z[j, k], j, k, schedule, model))
                                    sum++;
                            }
                            else
                                sum++;
                            break;
                        }
            }

            return sum;
        }
    }
    // проверено
    public class DayOfWeekTeamPerToursWish : TeamWish
    {
        DayOfWeek dayOfWeek;
        int numTours;
        int timesPerTours;

        public DayOfWeekTeamPerToursWish(int importancePercent, int team, DayOfWeek dayOfWeek, int tours, int timesPerTours) : base(importancePercent, team)
        {
            this.dayOfWeek = dayOfWeek;
            this.numTours = tours;
            this.timesPerTours = timesPerTours;
        }
        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((schedule.x[tour, gameInTour, 0] == team) || (schedule.x[tour, gameInTour, 1] == team))
            {
                int num = 0;
                int no = 0;
                for (int i = tour - tour % numTours; (i < tour - tour % numTours + numTours) && (i < schedule.tours); i++)
                {
                    for (int j = 0; j < schedule.games; j++)
                    {
                        if ((schedule.x[i, j, 0] == team) || (schedule.x[i, j, 1] == team))
                        {
                            if ((schedule.y[i, j].HasValue) && ((i != tour) || (j != gameInTour)))
                            {
                                if (model.dates[(int)schedule.y[i, j]].DayOfWeek == dayOfWeek)
                                    num++;
                            }
                            else
                                no++;
                            break;
                        }
                    }
                }
                if (model.dates[day].DayOfWeek == dayOfWeek)
                    return (num < timesPerTours);
                else
                {
                    if (tour == schedule.tours - 1)
                        no += schedule.tours % numTours;
                    return (no > timesPerTours - num);
                }
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int value = int.MaxValue;
            for (int l = 0; l <= schedule.tours % numTours; l++)
            {
                int sum = 0;
                for (int i = 0 - (numTours - l); i < schedule.tours; i += numTours)
                {
                    int num = 0;
                    int no = 0;
                    for (int j = i; (j < i + numTours); j++)
                    {
                        if ((j >= 0) && (j < schedule.tours))
                        {
                            for (int k = 0; k < schedule.games; k++)
                                if ((schedule.x[j, k, 0] == team) || (schedule.x[j, k, 1] == team))
                                {
                                    if (schedule.y[j, k].HasValue)
                                    {
                                        if (model.dates[(int)schedule.y[j, k]].DayOfWeek == dayOfWeek)
                                            num++;
                                    }
                                    else
                                        no++;
                                    break;
                                }
                        }
                        else
                            no++;
                    }
                    if (num + no - timesPerTours > timesPerTours - num)
                        sum += num + no - timesPerTours;
                    else
                        sum += timesPerTours - num;
                }
                if (sum < value)
                    value = sum;
            }
            return value;
        }
    }
    public class TimeDayOfWeekTeamWish : TeamWish
    {
        DayOfWeek dayOfWeek;
        int time;
        bool wish;
        public TimeDayOfWeekTeamWish(int importancePercent, int team, DayOfWeek dayOfWeek, int time, bool wish = true) : base(importancePercent, team)
        {
            this.dayOfWeek = dayOfWeek;
            this.time = time;
            this.wish = wish;
        }
        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((schedule.x[tour, gameInTour, 0] == team) || (schedule.x[tour, gameInTour, 1] == team))
            {
                if ((dayOfWeek == model.dates[day].DayOfWeek) && (time == slot))
                    return wish;
                return !wish;
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            if (schedule.fulfillment != null)
            {
                int numWish = model.wishes.IndexOf(this);
                for (int j = 0; j < schedule.tours; j++)
                    for (int k = 0; k < schedule.games; k++)
                        if ((schedule.x[j, k, 0] == team) || (schedule.x[j, k, 1] == team))
                        {
                            if (schedule.y[j, k].HasValue)
                            {
                                if (!schedule.fulfillment[numWish, j, k])
                                    sum++;
                            }
                            else
                                sum++;
                            break;
                        }
            }
            else
            {
                for (int j = 0; j < schedule.tours; j++)
                    for (int k = 0; k < schedule.games; k++)
                        if ((schedule.x[j, k, 0] == team) || (schedule.x[j, k, 1] == team))
                        {
                            if (schedule.y[j, k].HasValue)
                            {
                                if ((dayOfWeek == model.dates[(int)schedule.y[j, k]].DayOfWeek) && (time == schedule.z[j, k]))
                                {
                                    if (!wish)
                                        sum++;
                                }
                                else if (wish)
                                    sum++;
                            }
                            else
                                sum++;
                            break;
                        }
            }

            return sum;
        }
    }


    public class DayOfWeekPerMonthTeamWish : TeamWish
    {
        DayOfWeek dayOfWeek;
        int percent;

        public DayOfWeekPerMonthTeamWish(int importancePercent, int team, DayOfWeek dayOfWeek, int percent) : base(importancePercent, team)
        {
            this.dayOfWeek = dayOfWeek;
            this.percent = percent;
        }

        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            int month = model.dates[day].Month - 1;
            int[,] nDays = new int[(13 + model.dates[model.dates.Count - 1].Month - model.dates[0].Month) % 12, 7];
            int not = 0;
            int sum = 0;
            for (int i = 0; i < schedule.tours; i++)
                for (int j = 0; j < schedule.games; j++)
                    if ((schedule.x[i, j, 0] == team) || (schedule.x[i, j, 1] == team))
                    {
                        if ((schedule.y[i, j].HasValue) && ((i != tour) || (j != gameInTour)))
                        {
                            nDays[model.dates[(int)schedule.y[i, j]].Month - 1, (int)model.dates[(int)schedule.y[i, j]].DayOfWeek]++;
                            if (model.dates[(int)schedule.y[i, j]].Month - 1 == month)
                                sum++;
                        }
                        else
                            not++;
                        break;
                    }

            if (model.dates[day].DayOfWeek == dayOfWeek)
            {
                if (nDays[month, (int)dayOfWeek] + 1 <= (sum + 1) * percent / 100)
                    return true;
                else
                {

                }
            }
            else
                if (nDays[month, (int)dayOfWeek] + 1 >= ((sum + 1) * percent + 99) / 100)
                return true;
            return true;
        }

        public override int NumOfViolations(Schedule schedule, Model model)
        {
            return 10;
        }
    }

    // проверено
    public class TimeSlotTeamWish : TeamWish
    {
        int[] slots;
        bool wish;
        public TimeSlotTeamWish(int importancePercent, int team, int[] slots, bool wish = true) : base(importancePercent, team)
        {
            this.slots = slots;
            this.wish = wish;
        }
        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((schedule.x[tour, gameInTour, 0] == team) || (schedule.x[tour, gameInTour, 1] == team))
            {
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] == slot)
                        return wish;
                return !wish;
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            if (schedule.fulfillment != null)
            {
                int numWish = model.wishes.IndexOf(this);
                for (int j = 0; j < schedule.tours; j++)
                    for (int k = 0; k < schedule.games; k++)
                        if (schedule.y[j, k].HasValue)
                        {
                            if (schedule.fulfillment[numWish, j, k] == false)
                                sum++;
                        }
                        else
                            sum++;
            }
            else
            {
                for (int j = 0; j < schedule.tours; j++)
                    for (int k = 0; k < schedule.games; k++)
                    {
                        if (schedule.y[j, k].HasValue)
                        {
                            if (!Fulfilled((int)schedule.y[j, k], (int)schedule.z[j, k], j, k, schedule, model))
                                sum++;
                        }
                        else
                            sum++;
                    }
            }

            return sum;
        }
    }
    public class DayTimeSlotTeamWish : TeamWish
    {
        int[][] daysSlots;
        bool wish;

        public DayTimeSlotTeamWish(int importancePercent, int team, int[][] daysSlots, bool wish = true) : base(importancePercent, team)
        {
            this.daysSlots = daysSlots;
            this.wish = wish;
        }
        public override bool Fulfilled(int day, int slot, int tour, int gameInTour, Schedule schedule, Model model)
        {
            if ((schedule.x[tour, gameInTour, 0] == team) || (schedule.x[tour, gameInTour, 1] == team))
            {
                for (int i = 0; i < daysSlots.Length; i++)
                    if ((daysSlots[i][0] == day) && (daysSlots[i][1] == slot))
                    {
                        if (wish)
                            return true;
                        else
                            return false;
                    }
                if (wish)
                    return false;
                else
                    return true;
            }
            else
                return true;
        }
        public override int NumOfViolations(Schedule schedule, Model model)
        {
            int sum = 0;
            for (int j = 0; j < schedule.tours; j++)
                for (int k = 0; k < schedule.games; k++)
                {
                    if (schedule.y[j, k].HasValue)
                    {
                        int[] temp = new int[] { (int)schedule.y[j, k], (int)schedule.z[j, k] };
                        schedule.y[j, k] = schedule.z[j, k] = null;
                        if (!Fulfilled(temp[0], temp[1], j, k, schedule, model))
                            sum++;
                        schedule.y[j, k] = temp[0];
                        schedule.z[j, k] = temp[1];
                    }
                    else
                        sum++;
                }
            return sum;
        }
    }

    public abstract class Criterion
    {
        public int importancePercent;
        protected Model model;
        public Criterion(Model model, int importancePercent)
        {
            this.model = model;
            this.importancePercent = importancePercent;
        }
        public abstract int Value(Schedule schedule);
    }
    public class GeneralCriterion : Criterion
    {
        public GeneralCriterion(Model model, int importancePercent = 100) : base(model, importancePercent)
        {
        }
        public override int Value(Schedule schedule)
        {
            Schedule sch = new Schedule(schedule);
            int value = 0;
            for (int i = 0; i < model.wishes.Count; i++)
                if ((model.wishes[i].importancePercent < 100) && (!(model.wishes[i] is AlgoHelp)))
                    value += model.wishes[i].importancePercent * model.wishes[i].NumOfViolations(sch, model);
            value = (value + 99 - 1) / 99;
            return value;
        }
    }
}
