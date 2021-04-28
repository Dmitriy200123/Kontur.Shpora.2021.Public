using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClusterClient.QueryTimeStatistics;
using log4net;

namespace ClusterClient.Clients
{
    public class SmartClusterClient : ClusterClientBase
    {
        private string[] _replicaAddress;
        private readonly QueryTimeStatistic _queryTimeStatistic = new(10);

        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
            _replicaAddress = replicaAddresses;
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var stopWatch = Stopwatch.StartNew();
            if (_queryTimeStatistic.AddedReplicasCount == _replicaAddress.Length)
                _replicaAddress = _queryTimeStatistic.GetReplicasInOrderBySpeed();
            var timeoutForReplica = timeout / _replicaAddress.Length;
            var notVisitedReplicsCount = _replicaAddress.Length;
            var requestsTasks = new List<Task<string>>();
            foreach (var replica in _replicaAddress)
            {
                var timeQueryForReplica = Stopwatch.StartNew();
                var webRequest = CreateRequest(replica + "?query=" + query);
                Log.InfoFormat($"Processing {webRequest.RequestUri}");
                requestsTasks.Add(ProcessRequestAsync(webRequest));
                var task = Task.WhenAny(requestsTasks);
                await Task.WhenAny(task, Task.Delay(timeoutForReplica));
                if (task.IsCompletedSuccessfully && task.Result.IsCompletedSuccessfully)
                {
                    timeQueryForReplica.Stop();
                    _queryTimeStatistic.SetQueryTime(replica, timeQueryForReplica.ElapsedMilliseconds);
                    return task.Result.Result;
                }

                requestsTasks = requestsTasks.Where(t => !t.IsFaulted && !t.IsCanceled).ToList();
                notVisitedReplicsCount--;
                if (notVisitedReplicsCount > 0)
                    timeoutForReplica = timeout.Subtract(stopWatch.Elapsed) / notVisitedReplicsCount;
                timeQueryForReplica.Stop();
                _queryTimeStatistic.SetQueryTime(replica, timeQueryForReplica.ElapsedMilliseconds);
            }
            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}