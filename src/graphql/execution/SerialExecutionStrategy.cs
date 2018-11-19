﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fugu.graphql.error;
using fugu.graphql.type;
using GraphQLParser.AST;

namespace fugu.graphql.execution
{
    public class SerialExecutionStrategy : ExecutionStrategyBase
    {
        public override async Task<IDictionary<string, object>> ExecuteGroupedFieldSetAsync(
            IExecutorContext context,
            Dictionary<string, List<GraphQLFieldSelection>> groupedFieldSet,
            ObjectType objectType, object objectValue,
            Dictionary<string, object> coercedVariableValues)
        {
            var responseMap = new Dictionary<string, object>();

            foreach (var fieldGroup in groupedFieldSet)
            {
                var responseKey = fieldGroup.Key;

                try
                {
                    var result = await ExecuteFieldGroupAsync(
                        context,
                        objectType,
                        objectValue,
                        coercedVariableValues,
                        fieldGroup).ConfigureAwait(false);

                    responseMap[responseKey] = result;
                }
                catch (GraphQLError e)
                {
                    responseMap[responseKey] = null;
                    context.AddError(e);
                }
            }

            return responseMap;
        }
    }
}