using System.Collections.Generic;
using System.Linq;

namespace ClusterClient.QueryTimeStatistics
{
    public class QueryTimeStatistic
    {
        private readonly Dictionary<string, List<long>> _replicasAndStatistic = new();
        private readonly int _sampleSize;
        private readonly object _lockObject = new();

        public int AddedReplicasCount { get; private set; }

        public QueryTimeStatistic(int sampleSize)
        {
            _sampleSize = sampleSize;
        }

        public void SetQueryTime(string replica, long timeFromMilliseconds)
        {
            lock (_lockObject)
            {
                if (_replicasAndStatistic.ContainsKey(replica))
                {
                    if (_replicasAndStatistic[replica].Count == _sampleSize)
                        _replicasAndStatistic[replica] = _replicasAndStatistic[replica].Skip(1).ToList();
                    _replicasAndStatistic[replica].Add(timeFromMilliseconds);
                }
                else
                {
                    AddedReplicasCount++;
                    _replicasAndStatistic[replica] = new List<long> {timeFromMilliseconds};
                }
            }
        }

        public string[] GetReplicasInOrderBySpeed()
        {
            lock (_lockObject)
            {
                return _replicasAndStatistic
                    .OrderBy(replicaAndStatistic =>
                        replicaAndStatistic.Value.Sum() / replicaAndStatistic.Value.Count)
                    .Select(replicaAndStatistic => replicaAndStatistic.Key)
                    .ToArray();
            }
        }
    }
}