using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RP.Prober.DataTypes
{
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
}
