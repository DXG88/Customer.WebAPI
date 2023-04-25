using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Concurrent;

namespace Customer.WebApi
{
    public class ScoreUpdateService : BackgroundService
    {
        private readonly List<KeyValuePair<long, decimal>> _sortedLeaderboard = new List<KeyValuePair<long, decimal>>();

        private readonly ConcurrentQueue<KeyValuePair<long, decimal>> _queue = new ConcurrentQueue<KeyValuePair<long, decimal>>();
       
        public ScoreUpdateService()
        {
            InitDataBase();
        }

        private void InitDataBase()
        {
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(76786448, 78));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(254814111, 65));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(53274324, 64));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(6144320, 32));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(7777777, 298));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(54814111, 301));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(7786448, 313));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(96144320, 298));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(16144320, 270));
            _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(2000437, 239));
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
            int index = _sortedLeaderboard.FindIndex(x => x.Key == customerId);
            if (index != -1)
            {
                _sortedLeaderboard[index] = new KeyValuePair<long, decimal>(customerId, score);
            }
            else
            {
                _sortedLeaderboard.Add(new KeyValuePair<long, decimal>(customerId, score));
            }

            _sortedLeaderboard.Sort((x, y) =>
            {
                int result = y.Value.CompareTo(x.Value);
                if (result == 0)
                {
                    result = x.Key.CompareTo(y.Key);
                }
                return result;
            });
        }

        public IEnumerable<Tuple<long,decimal,int>> GetCustomersByRank(int start, int end)
        {
             return _sortedLeaderboard.Skip(start - 1).Take(end - start + 1)
                .Select((c, i) => Tuple.Create(c.Key,c.Value, start + i));
        }

        public IEnumerable<Tuple<long, decimal, int>> GetCustomerById(long customerId, int high, int low)
        {
            var sortedLeaderboard = _sortedLeaderboard;
            var customerIndex = sortedLeaderboard.FindIndex(entry => entry.Key == customerId);

            if (customerIndex == -1)
                return Array.Empty<Tuple<long, decimal, int>>();

            var startIndex = Math.Max(0, customerIndex - high);
            var endIndex = Math.Min(sortedLeaderboard.Count - 1, customerIndex + low);

            var result = sortedLeaderboard.Skip(startIndex)
                .Take(endIndex - startIndex + 1)
                .Select((c, i) => Tuple.Create(c.Key, c.Value, startIndex + i));

            return result;
        }
    }
}
