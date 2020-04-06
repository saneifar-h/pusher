using System.Collections.Generic;
using System.Data.SqlClient;
using PeriodicalChangePusher.Core;

namespace PeriodicalChangePusher.Sql
{
    public class SqlPushSubscriber : IPushSubscriber
    {
        private readonly ISqlConnectionProvider sqlConnectionProvider;
        private SqlConnection sqlConnection;

        public SqlPushSubscriber(ISqlConnectionProvider sqlConnectionProvider)
        {
            this.sqlConnectionProvider = sqlConnectionProvider;
        }
        public async void OnPush(string topic, IReadOnlyList<KeyValuePair<string, string>> changeValues)
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