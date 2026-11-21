using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskFocusAPI.Library.DataAccess;

namespace TaskFocusAPI.Data
{
    internal sealed class SqlConnectionRetryInterceptor : DbConnectionInterceptor
    {
        public override DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result)
        {
            SqlConnectionRetryProvider.Instance.Configure((SqlConnection)result);
            return result;
        }
    }
}
