using CircularBuffer;
using Microsoft.AspNetCore.Mvc;
using RP.Prober.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

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


            if (headers != null)
                _headers = headerType == HeaderType.Row ? (new List<string>() { "Fields" }).Concat(headers).ToList() : headers;
            else
                _headers = new List<string>();

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

            var convert = Convert;

            if (convert != null)
            {
                foreach (var item in items)
                {
                    if (_headerType == HeaderType.Row)
                        res.Add(new List<string>() { "Values" }.Concat(Convert(item)).ToList());
                    else
                        res.Add(Convert(item));
                }

                if (_headerType == HeaderType.Row)
                    res = Transpose(res);
            }          

            return res;
        }

        private static List<List<string>> Transpose(List<List<string>> input)
        {
            if (input == null || input.Count == 0)
                return new List<List<string>>();

            int rowCount = input.Count;
            int colCount = input[0].Count;

            var result = new List<List<string>>(colCount);

            for (int col = 0; col < colCount; col++)
            {
                var newRow = new List<string>(rowCount);
                for (int row = 0; row < rowCount; row++)
                {
                    newRow.Add(input[row][col]);
                }
                result.Add(newRow);
            }

            return result;
        }

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
