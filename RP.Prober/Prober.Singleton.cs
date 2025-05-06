using RP.Prober.Interfaces;
using RP.Prober.DataTypes;

namespace RP.Prober.Singleton
{
    public class ProberCacheClub
    {
        public static ProberCacheClub ProberCacheClubSingleton = new ProberCacheClub();

        private ProberCacheClub() { }

        private object locker = new object();

        public List<IProberCacheMonitoring> tablePublisherCacheMonitorings = new List<IProberCacheMonitoring>();
        public void Register(IProberCacheMonitoring tablePublisherCacheMonitoring)
        {
            lock (locker)
            {
                tablePublisherCacheMonitorings.Add(tablePublisherCacheMonitoring);
            }
        }

        public void Unregister(IProberCacheMonitoring tablePublisherCacheMonitoring)
        {
            lock (locker)
            {
                tablePublisherCacheMonitorings.Remove(tablePublisherCacheMonitoring);
            }
        }

        public List<ExtendedTableInfo> GetCachedTablesNames()
        {
            var tables = new List<ExtendedTableInfo>();

            lock (locker)
            {
                foreach (var tablePublisherCacheMonitoring in tablePublisherCacheMonitorings)
                {
                    var tableInfo = new ExtendedTableInfo();
                    
                    tableInfo.Name = tablePublisherCacheMonitoring.TableName;
                    tableInfo.Guid = tablePublisherCacheMonitoring.TableGuid;
                    tableInfo.Available = tablePublisherCacheMonitoring.Available;

                    tables.Add(tableInfo);
                }
            }

            return tables;
        }

        public List<Table> GetCachedTables(List<Guid> tablesGuid)
        {
            var tables = new List<Table>();

            try
            {
                var tablesGuidHashset = tablesGuid.ToHashSet();

                lock (locker)
                {
                    foreach (var tablePublisherCacheMonitoring in tablePublisherCacheMonitorings)
                    {
                        try
                        {
                            if (!tablesGuidHashset.Contains(tablePublisherCacheMonitoring.TableGuid))
                                continue;

                            var table = new Table();

                            if (tablePublisherCacheMonitoring.Available)
                            {
                                table.TableInfo = new TableInfo();
                                table.TableInfo.Name = tablePublisherCacheMonitoring.TableName;
                                table.TableInfo.Guid = tablePublisherCacheMonitoring.TableGuid;

                                table.TableData = tablePublisherCacheMonitoring?.GetInnerCachedTable() ?? new List<List<string>>();
                            }

                            tables.Add(table);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return tables;
        }
    }
}
