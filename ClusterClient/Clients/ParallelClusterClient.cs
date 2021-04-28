using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class ParallelClusterClient : ClusterClientBase
    {
        public ParallelClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = ReplicaAddresses.Select(replica =>
            {
                return Task.Run(async () =>
                {
                    var webRequest = CreateRequest(replica + "?query=" + query);
                    Log.InfoFormat($"Processing {webRequest.RequestUri}");
                    var requestAsync = ProcessRequestAsync(webRequest);
                    await Task.WhenAny(requestAsync, Task.Delay(timeout));
                    if (requestAsync.IsCompletedSuccessfully)
                        return requestAsync.Result;
                    return requestAsync.IsFaulted ? "" : null;
                });
            }).ToList();
            var result = Task.FromResult("");
            while (tasks.Count != 0 && result.Result == "")
            {
                result = await Task.WhenAny(tasks);
                tasks = tasks.Where(i => i.Id != result.Id).ToList();
            }
            if (result.Result == null)
                throw new TimeoutException();
            if (result.Result.Length == 0)
                throw new Exception("Error with code 500");
            return result.Result;
        }

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));
    }
}
