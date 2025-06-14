using CircularBuffer;
using RP.Prober.Interfaces;
using RP.Prober.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RP.Prober.CyclicCacheProbing
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

        protected CircularBuffer<T> buffer;

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

        protected virtual List<T> GetItems()
        {
            return buffer.ToList();
        }

        public List<List<string>> GetInnerCachedTable()
        {
            var res = new List<List<string>>() { _headers };
            var items = new List<T>();

            lock (buffer)
                items = GetItems();

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

    public class KeyedCacheProbing<TKey, TValue> : IProberCacheMonitoring, IDisposable
    {
        public bool Available => true;
        public string TableName { get; set; }
        public Guid TableGuid { get; set; }

        private Dictionary<TKey, TValue> dictionary;

        private List<string> _headers;

        private bool _transpose;

        public KeyedCacheProbing(
            string name,
            List<string> headers,
            bool transpose)
        {
            _transpose = transpose;

            TableName = name;
            TableGuid = Guid.NewGuid();

            dictionary = new Dictionary<TKey, TValue>();


            _headers = headers ?? new List<string>();

            ProberCacheClub.ProberCacheClubSingleton.Register(this);
        }

        public void SetKeyValue(TKey key, TValue value)
        {
            lock (dictionary) 
                dictionary[key] = value;
        }

        public void RemoveKey(TKey key)
        {
            lock (dictionary)
                dictionary.Remove(key);
        }

        public Func<TKey, TValue, List<string>> Convert;

        public List<List<string>> GetInnerCachedTable()
        {
            var res = new List<List<string>>();
            var items = new List<Tuple<TKey, TValue>>();

            lock (dictionary)
                items = dictionary.Select(x => Tuple.Create(x.Key, x.Value)).ToList();

            var convert = Convert;

            if (convert != null)
            {
                foreach (var item in items)
                {
                    res.Add(Convert(item.Item1, item.Item2));
                }
            }

            res = res.OrderBy(x => x.First()).ToList();

            res.Insert(0, _headers);


            return _transpose ? Transpose(res) : res;
        }

        static List<List<string>> Transpose(List<List<string>> source)
        {
            if (source == null || source.Count == 0)
                return new List<List<string>>();

            int rowCount = source.Count;
            int colCount = source[0].Count;

            return Enumerable.Range(0, colCount)
                             .Select(col => Enumerable.Range(0, rowCount)
                                                      .Select(row => source[row][col])
                                                      .ToList())
                             .ToList();
        }

        public void Dispose()
        {
            ProberCacheClub.ProberCacheClubSingleton.Unregister(this);
        }
    }

    public class TableCacheProbing<T> : IProberCacheMonitoring, IDisposable
    {
        private static readonly List<List<string>> EMPTY_TABLE = new(0);
        private readonly List<List<string>> _headersOnlyTable;

        public bool Available => true;
        public string TableName { get; set; }
        public Guid TableGuid { get; set; }

        private T table;
        private List<string> _headers;

        private object locker = new object();

        public TableCacheProbing(string name, List<string> headers = null!)
        {
            _headers = headers;
            _headersOnlyTable = true == _headers?.Any() ? new List<List<string>>() { _headers } : EMPTY_TABLE;

            TableName = name;
            TableGuid = Guid.NewGuid();

            ProberCacheClub.ProberCacheClubSingleton.Register(this);
        }

        public void SetTableData(T t)
        {
            lock (locker)
                table = t;
        }

        public Func<T, List<List<string>>> Convert;

        public List<List<string>> GetInnerCachedTable()
        {
            List<List<string>> res = null!;

            T localTable;

            lock (locker)
                localTable = table;

            var convert = Convert;
            if (convert != null && localTable != null)
            {
                res = convert(localTable);
                if (true == _headers?.Any())
                {
                    res.Insert(0, _headers);
                }
            }
            else if (true == _headers?.Any())
            {
                res = _headersOnlyTable;
            }

            return res ?? EMPTY_TABLE;
        }

        public void Dispose() => ProberCacheClub.ProberCacheClubSingleton.Unregister(this);
    }

    public class ExpendableCyclicCacheProbing<T> : CyclicCacheProbing<T>
    {
        public ExpendableCyclicCacheProbing(
            int cacheSize,
            string name,
            List<string> headers,
            HeaderType headerType) : base(cacheSize, name, headers, headerType) { }

        protected override List<T> GetItems()
        {
            var values = new List<T>();
            var elementsToPop = buffer.Count();

            for (int i = 0; i < elementsToPop; i++)
            {
                values.Add(buffer.Back());
                buffer.PopBack();
            }

            return values;
        }
    }
}
