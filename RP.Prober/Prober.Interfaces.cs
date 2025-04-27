using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RP.Prober.Interfaces
{
    public interface IName
    {
        string TableName { get; }
    }

    public interface IGuid
    {
        Guid TableGuid { get; }
    }

    public interface IProberCacheMonitoring : IName, IGuid
    {
        bool Available { get; }
        List<List<string>> GetInnerCachedTable();
    }
}
