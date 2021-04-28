using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ClusterClient.QueryTimeStatistics;
using log4net;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        private string[] _replicaAddress;
        private readonly QueryTimeStatistic _queryTimeStatistic = new(10);

        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            _replicaAddress = ReplicaAddresses;
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var stopWatch = Stopwatch.StartNew();
            if (_queryTimeStatistic.AddedReplicasCount == _replicaAddress.Length)
                    _replicaAddress = _queryTimeStatistic.GetReplicasInOrderBySpeed();
            var timeoutForReplica = timeout / ReplicaAddresses.Length;
            var notVisitedReplicsCount = _replicaAddress.Length;
            foreach (var replica in _replicaAddress)
            {
                var timeQueryForReplica = Stopwatch.StartNew();
                var webRequest = CreateRequest(replica + "?query=" + query);
                Log.InfoFormat($"Processing {webRequest.RequestUri}");
                var response = ProcessRequestAsync(webRequest);
                await Task.WhenAny(response, Task.Delay(timeoutForReplica));
                if (response.IsCompletedSuccessfully)
                {
                    timeQueryForReplica.Stop();
                    _queryTimeStatistic.SetQueryTime(replica, timeQueryForReplica.ElapsedMilliseconds);
                    return response.Result;
                }
                notVisitedReplicsCount--;
                if (notVisitedReplicsCount > 0)
                    timeoutForReplica = (timeout.Subtract(stopWatch.Elapsed)) / notVisitedReplicsCount;
                timeQueryForReplica.Stop();
                _queryTimeStatistic.SetQueryTime(replica, timeQueryForReplica.ElapsedMilliseconds);
            }
            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClusterClient));
    }
}