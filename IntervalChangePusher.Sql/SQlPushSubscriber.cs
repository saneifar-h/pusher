using System.Collections.Generic;
using System.Data.SqlClient;
using IntervalChangePusherLib;

namespace IntervalChangePusherSql
{
    public class SqlPushSubscriber : IPushSubscriber
    {
        private readonly ISqlConnectionProvider sqlConnectionProvider;
        private SqlConnection sqlConnection;

        public SqlPushSubscriber(ISqlConnectionProvider sqlConnectionProvider)
        {
            this.sqlConnectionProvider = sqlConnectionProvider;
        }

        public async void OnPush(string topic, IReadOnlyList<KeyValuePair<string, object>> changeValues)
        {
        }

        public void Initialize()
        {
            if (sqlConnection != null) return;
            sqlConnection = new SqlConnection(sqlConnectionProvider.Provide());
            sqlConnection.Open();
        }
    }
}