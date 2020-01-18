using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public interface IDatabaseService
    {
        Task Read(string commandString, Action<DbDataReader> processRow);

        Task Write(string commandString);

        Task BulkWrite(string commandString, IEnumerable<Dictionary<string, object>> parameters);
    }
}
