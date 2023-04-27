using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Concurrent;

namespace Customer.WebApi
{
    public class CustomerScore : IComparable<CustomerScore>
    { 
        public long CustomerId { get; set; }
        public decimal Score { get; set; }
        public int Rank { get; set; }

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

        private readonly ConcurrentQueue<KeyValuePair<long, decimal>> _queue = new ConcurrentQueue<KeyValuePair<long, decimal>>();
       
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
            _queue.Enqueue(new KeyValuePair<long, decimal>(customerId, score));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (_queue.TryDequeue(out var entry))
                {
                    var (customerId, score) = entry;

                    UpdateCustomerInfo(customerId,score);
                    
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        private void UpdateCustomerInfo(long customerId, decimal score)
        {
            var c = _sortedLeaderboard.FirstOrDefault(x => x.CustomerId == customerId);
            if (c == null)
            {
                _sortedLeaderboard.Add(new CustomerScore { CustomerId = customerId, Score = score });
            }
            else
            {
                _sortedLeaderboard.Remove(c);
                _sortedLeaderboard.Add(new CustomerScore { CustomerId = customerId, Score = c.Score + score });
            }

            int rank = 1;
            foreach (var item in _sortedLeaderboard)
            {
                item.Rank = rank++;
            }
        }

        public IEnumerable<CustomerScore> GetCustomersByRank(int start, int end)
        {
            return _sortedLeaderboard.TakeWhile(x => x.Rank >= start && x.Rank <= end);
        }

        public IEnumerable<CustomerScore> GetCustomerById(long customerId, int high, int low)
        {
            var target = _sortedLeaderboard.FirstOrDefault(x => x.CustomerId == customerId);
            if (target != null)
            {
                var before = _sortedLeaderboard.TakeWhile(x => x.CompareTo(target) < 0).Reverse().Take(high);
                var after = _sortedLeaderboard.SkipWhile(x => x.CompareTo(target) <= 0).Take(low);
                return before.Reverse().Concat(new[] { target }).Concat(after);
            }
            else
            {
                return Array.Empty<CustomerScore>();
            }
        }
    }
}
