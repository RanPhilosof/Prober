using Microsoft.Extensions.Logging;
using RP.Infra;
using RP.Prober.DataTypes;
using RP.Prober.Singleton;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace Prober.Consumer.Service
{    
    public class ProberCacheMonitoringService
    {
        private Dictionary<string, HttpClient> appsThatSupportMonitoring = new Dictionary<string, HttpClient>();
        private ILogger<ProberCacheMonitoringService> _logger;
        public ProberCacheMonitoringService(IServicesInfo servicesInfo, ILogger<ProberCacheMonitoringService> logger)
        {
            _logger = logger;

            try
            {
                foreach (var service in servicesInfo.GetAll())
                {
                    try
                    {
                        if (service.Value.SupportPublisherCacheMonitors)
                        {
                            var handler = new HttpClientHandler()
                            {
                                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                            };

                            appsThatSupportMonitoring.Add(service.Key, new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5), BaseAddress = new Uri($"{(service.Value.RestApiSecured ? "https" : "http")}://{service.Value.Ip}:{service.Value.RestApiPort}") });
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }
        }

        public async Task<List<Tuple<string, List<ExtendedTableInfo>>>> GetAllAppsTableInfoAsync()
        {                        
            var result = new List<Tuple<string, List<ExtendedTableInfo>>>();            
            try
            {
                var tasks = new List<Tuple<string, Task<List<ExtendedTableInfo>>>>();

                foreach (var app in appsThatSupportMonitoring)
                {
                    try
                    {
                        var appTableInfo = GetAppTableInfo(app.Value);

                        tasks.Add(Tuple.Create(app.Key, appTableInfo));
                    }
                    catch (Exception ex)
                    {
                        LogError(ex.ToString());
                    }
                }

                foreach (var task in tasks)
                {
                    try
                    {
                        result.Add(Tuple.Create(task.Item1, await task.Item2));
                    }
                    catch (Exception ex)
                    {
                        LogError(ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }

            return result;
        }

        public async Task<List<ExtendedTableInfo>> GetAppTableInfo(HttpClient http)
        {
            var result = new List<ExtendedTableInfo>();

            try
            {
                result = await http.GetFromJsonAsync<List<ExtendedTableInfo>>($"api/ProberCacheMonitoring/CachedTablesNames");
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }

            return result;
        }

        public async Task<List<Table>> GetAppTableDataAsync(string appName, List<Guid> guids)
        {
            var res = new List<Table>();

            try
            {
                res = await GetAppTableData(appsThatSupportMonitoring[appName], guids);
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }

            return res;
        }

        public async Task<List<Table>> GetAppTableData(HttpClient client, List<Guid> guids)
        {
            var result = new List<Table>();

            try
            {
                var response = await client.PostAsJsonAsync($"api/ProberCacheMonitoring/CachedTables", guids);

                if (response.IsSuccessStatusCode)
                {
                    var res = await response.Content.ReadFromJsonAsync<List<Table>>();

                    return res ?? new List<Table>();
                }
            }
            catch (HttpRequestException httpExcetption)
            {
                LogError($"Target refused, {client.BaseAddress.Host}:{client.BaseAddress.Port}");
                //LogError(httpExcetption.ToString());
                //LogError($"Lala11 + {DateTime.Now.Second}");
            }
            catch (Exception ex)
            {
                LogError(ex.ToString());
            }

            return result;
        }

        private readonly ConcurrentQueue<Tuple<long, string>> _logHistory = new();
        private const int MaxLogLines = 100;
        private long _logSequence = 0;
        public long CurrentLogSequence => _logSequence;

        private void LogError(string message)
        {
            if (_logger != null)
            {
                var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
                _logger?.LogInformation(line);

                var lineNum = Interlocked.Increment(ref _logSequence); // ensure thread safety
                _logHistory.Enqueue(Tuple.Create<long, string>(lineNum, line));
                

                // Keep only the last N entries
                while (_logHistory.Count > MaxLogLines)
                {
                    _logHistory.TryDequeue(out _);
                }
            }
        }

        public List<Tuple<long, string>> GetLogHistoryAndClear()
        {
            var logs = new List<Tuple<long, string>>(_logHistory.Count);

            while (_logHistory.TryDequeue(out Tuple<long, string>  log)) 
                logs.Add(log);
            
            return logs;
        }

        public List<Tuple<long, string>> GetLogHistory() => _logHistory.ToList();
    }
}
