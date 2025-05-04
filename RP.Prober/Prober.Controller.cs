using Microsoft.AspNetCore.Mvc;
using RP.Prober.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using static System.Runtime.InteropServices.Marshalling.IIUnknownCacheStrategy;

namespace RP.Prober.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProberCacheMonitoringController : ControllerBase
    {
        [HttpGet("CachedTablesNames")]
        public ActionResult<List<ExtendedTableInfo>> GetCachedTablesNames()
        {
            return ProberCacheClub.ProberCacheClubSingleton.GetCachedTablesNames();
        }

        [HttpPost("CachedTables")]
        public ActionResult<List<Table>> GetCachedTables([FromBody] List<Guid> tablesGuid)
        {
            return ProberCacheClub.ProberCacheClubSingleton.GetCachedTables(tablesGuid);
        }
    }
}
