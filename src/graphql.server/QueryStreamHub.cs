﻿using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace tanka.graphql.server
{
    public class QueryStreamHub : Hub
    {
        private readonly QueryStreamService _queryStreamService;

        public QueryStreamHub(QueryStreamService queryStreamService)
        {
            _queryStreamService = queryStreamService;
        }

        [HubMethodName("query")]
        public async Task<ChannelReader<ExecutionResult>> QueryAsync(
            QueryRequest query,
            CancellationToken cancellationToken)
        {
            var queryResult = await _queryStreamService.QueryAsync(query, cancellationToken);
            var channel = queryResult.Channel;
            return channel.Reader;
        }
    }
}