using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Customer.WebApi
{
    public static class SortedSetExtensions
    {
        public static CustomerScore? Find(this SortedSet<CustomerScore> set, long customerId)
        {
            foreach (var score in set)
            {
                if (score.CustomerId == customerId)
                {
                    return score;
                }
            }

            return null;
        }
    }


    public class CustomerScore : IComparable<CustomerScore>
    { 
        public long CustomerId { get; set; }
        public decimal Score { get; set; }

        public int CompareTo(CustomerScore other)
        {
            int result = Score.CompareTo(other.Score);
            if (result == 0)
                result = CustomerId.CompareTo(other.CustomerId);
            return result;
        }
    }

    public class ScoreUpdateService : BackgroundService
    {
        private readonly SortedSet<CustomerScore> _sortedLeaderboard = new SortedSet<CustomerScore>();

        private readonly ConcurrentQueue<CustomerScore> _queue = new ConcurrentQueue<CustomerScore>();
       
        public ScoreUpdateService()
        {
            InitDataBase();
        }

        private void InitDataBase()
        {
            UpdateCustomerInfo(76786448, 78);
            UpdateCustomerInfo(254814111, 65);
            UpdateCustomerInfo(53274324, 64);
            UpdateCustomerInfo(6144320, 32);
            UpdateCustomerInfo(7777777, 298);
            UpdateCustomerInfo(54814111, 301);
            UpdateCustomerInfo(7786448, 313);
            UpdateCustomerInfo(96144320, 298);
            UpdateCustomerInfo(16144320, 270);
            UpdateCustomerInfo(2000437, 239);
        }

        public void EnqueueScoreUpdate(long customerId, decimal score)
        {
            _queue.Enqueue(new CustomerScore { CustomerId = customerId, Score = score });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_queue.TryDequeue(out var c))
                {
                    UpdateCustomerInfo(c.CustomerId, c.Score);
                    
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private void UpdateCustomerInfo(long customerId, decimal score)
        {
            var c = _sortedLeaderboard.Find(customerId);
            if (c == null)
            {
                _sortedLeaderboard.Add(new CustomerScore { CustomerId = customerId, Score = score });
            }
            else
            {
                _sortedLeaderboard.Remove(c);
                _sortedLeaderboard.Add(new CustomerScore { CustomerId = customerId, Score = c.Score + score });
            }
        }

        public IEnumerable<Tuple<long, decimal, int>> GetCustomersByRank(int start, int end)
        {
            start = Math.Max(1, start);
            end = Math.Min(_sortedLeaderboard.Count, end);

            return _sortedLeaderboard.Skip(start - 1).Take(end - start + 1)
                .Select((c, i) => Tuple.Create(c.CustomerId, c.Score, start + i));
        }

        public IEnumerable<Tuple<long, decimal, int>> GetCustomerById(long customerId, int high, int low)
        {
            var target = _sortedLeaderboard.Find(customerId);
            if (target != null)
            {
                int index = _sortedLeaderboard.TakeWhile(x => x.CompareTo(target) < 0).Count();
                
                return GetCustomersByRank(index - high + 1, index + low + 1);
            }
            else
            {
                return Array.Empty<Tuple<long, decimal, int>>();
            }
        }
    }
}
