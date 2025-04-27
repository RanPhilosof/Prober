using CircularBuffer;
using Microsoft.AspNetCore.Mvc;
using RP.Prober.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace RP.Prober.Singleton
{
    public enum HeaderType
    {
        Column,
        Row,
    }

    public class CyclicCacheProbing<T> : IProberCacheMonitoring, IDisposable
    {
        public bool Available => true;
        public string TableName { get; set; }
        public Guid TableGuid { get; set; }

        private CircularBuffer<T> buffer;

        List<string> _headers;
        HeaderType _headerType;

        public CyclicCacheProbing(
            int cacheSize, 
            string name, 
            List<string> headers,
            HeaderType headerType)
        {
            TableName = name;
            TableGuid = Guid.NewGuid();

            buffer = new CircularBuffer<T>(cacheSize);

            _headers = headers ?? new List<string>();
            _headerType = headerType;

            ProberCacheClub.ProberCacheClubSingleton.Register(this);
        }

        

        public void EnqueueCyclic(T t)
        {
            lock (buffer)
            {
                buffer.PushBack(t);
            }
        }

        public Func<T, List<string>> Convert;

        public List<List<string>> GetInnerCachedTable()
        {
            var res = new List<List<string>>() { _headers };
            var items = new List<T>();

            lock (buffer)
                items = buffer.ToList();

            if (_headerType == HeaderType.Column)
            {
                foreach (var item in items)
                    res.Add(Convert(item));
            }

            return res;
        }

        //private Queue<List<string>> buffer = new Queue<List<string>>();
        
        private static int counter = 0;


        public static List<string> Headers = new List<string>() { "Index", "N1", "N2", "N3", "State" };

        public List<string> GenerateNewLine()
        {
            counter++;

            return new List<string>() { $"{counter}", $"{Random.Shared.Next(1, 10)}", $"{Random.Shared.Next(11, 20)}", $"{Random.Shared.Next(21, 30)}", $"{strs[Random.Shared.Next(0, 4)]}"  };
        }
        
        public static List<string> strs = new List<string>() { "T1T1", "T3T1", "Clear", "Min", "Mean" };

        public void Dispose()
        {
            ProberCacheClub.ProberCacheClubSingleton.Unregister(this);
        }

        
    }


    public class TableInfo
    {
        public string Name { get; set; }
        public Guid Guid { get; set; }
    }

    public class ExtendedTableInfo : TableInfo
    {
        public bool Available { get; set; }
    }

    public class Table
    {
        public TableInfo TableInfo { get; set; }

        public List<List<string>> TableData { get; set; }
    }

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
            var tablesGuidHashset = tablesGuid.ToHashSet();

            var tables = new List<Table>();
            
            lock (locker)
            {
                foreach (var tablePublisherCacheMonitoring in tablePublisherCacheMonitorings)
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
            }

            return tables;
        }
    }
}
