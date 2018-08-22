using System.Collections.Generic;
using System.Threading.Tasks;
using fugu.graphql.type;
using fugu.graphql.tests.data;
using fugu.graphql.tools;
using Xunit;
using static fugu.graphql.Executor;
using static fugu.graphql.Parser;
using static fugu.graphql.resolvers.Resolve;
using static fugu.graphql.type.ScalarType;

namespace fugu.graphql.tests
{
    public class ErrorsFacts
    {
        public ErrorsFacts()
        {
            var nested = new ObjectType(
                "Nest",
                new Fields
                {
                    ["nestedNonNull"] = new Field(NonNullString)
                });

            _schema = new Schema(
                new ObjectType(
                    "Query",
                    new Fields
                    {
                        ["nonNull"] = new Field(NonNullString),
                        ["nonNullNested"] = new Field(new NonNull(nested)),
                        ["nonNullListItem"] = new Field(new List(NonNullString)),
                        ["nonNullList"] = new Field(new NonNull(new List(String))),
                        ["nullableNested"] = new Field(nested),
                        ["nullable"] = new Field(String)
                    }));

            var nestedNonNullData = new Dictionary<string, string>
            {
                ["nestedNonNull"] = null
            };

            _resolvers = new ResolverMap
            {
                ["Query"] = new FieldResolverMap
                {
                    {"nonNull", context => Task.FromResult(As(null))},
                    {"nonNullNested", context => Task.FromResult(As(nestedNonNullData))},
                    {"nonNullListItem", context => Task.FromResult(As(new[] {"str", null, "str"}))},
                    {"nonNullList", context => Task.FromResult(As(null))},
                    {"nullableNested", context => Task.FromResult(As(nestedNonNullData))},
                    {"nullable", context => Task.FromResult(As("hello"))}
                },

                ["Nest"] = new FieldResolverMap
                {
                    {"nestedNonNull", PropertyOf<Dictionary<string, string>>(d => d["nestedNonNull"])}
                }
            };

            _executable = SchemaTools.MakeExecutableSchemaAsync(
                _schema,
                _resolvers).Result;
        }

        private readonly Schema _schema;
        private readonly IResolverMap _resolvers;
        private readonly ISchema _executable;


        [Fact]
        public async Task Nested_NonNull_should_produce_error_and_set_graph_to_null()
        {
            /* Given */
            var query = @"
{
    nonNullNested {
       nestedNonNull 
    }
}";

            /* When */
            var result = await ExecuteAsync(new ExecutionOptions
            {
                Schema = _executable,
                Document = ParseDocument(query)
            }).ConfigureAwait(false);


            /* Then */
            result.ShouldMatchJson(@"
{
  ""errors"": [
    {
      ""message"": ""'Query.nonNullNested: Nest!' is non-null field and cannot be set to null. ""
    },
    {
      ""message"": ""'Nest.nestedNonNull: String!' is non-null field and cannot be set to null. Type 'String!' cannot be null""
    }
  ],
  ""data"": null
}");
        }

        [Fact]
        public async Task Nested_Nullable_NonNull_and_nullable_should_produce_error()
        {
            /* Given */
            var query = @"
{
    nullable
    nullableNested {
        nestedNonNull
    }
}";

            /* When */
            var result = await ExecuteAsync(new ExecutionOptions
            {
                Schema = _executable,
                Document = ParseDocument(query)
            }).ConfigureAwait(false);


            /* Then */
            result.ShouldMatchJson(@"
{
  ""errors"": [
    {
      ""message"": ""'Nest.nestedNonNull: String!' is non-null field and cannot be set to null. Type 'String!' cannot be null""
    }
  ],
  ""data"": {
    ""nullable"": ""hello"",
    ""nullableNested"": null
  }
}");
        }

        [Fact]
        public async Task Nested_Nullable_NonNull_should_produce_error()
        {
            /* Given */
            var query = @"
{
    nullableNested {
       nestedNonNull 
    }
}";

            /* When */
            var result = await ExecuteAsync(new ExecutionOptions
            {
                Schema = _executable,
                Document = ParseDocument(query)
            }).ConfigureAwait(false);


            /* Then */
            result.ShouldMatchJson(@"
{
  ""errors"": [
    {
      ""message"": ""'Nest.nestedNonNull: String!' is non-null field and cannot be set to null. Type 'String!' cannot be null""
    }
  ],
  ""data"": {
    ""nullableNested"": null
  }
}");
        }

        [Fact]
        public async Task NonNull_should_produce_error()
        {
            /* Given */
            var query = @"
{
    nonNull
}";

            /* When */
            var result = await ExecuteAsync(new ExecutionOptions
            {
                Schema = _executable,
                Document = ParseDocument(query)
            }).ConfigureAwait(false);


            /* Then */
            result.ShouldMatchJson(@"
{
  ""errors"": [
    {
      ""message"": ""'Query.nonNull: String!' is non-null field and cannot be set to null. Type 'String!' cannot be null""
    }
  ],
  ""data"": null
}");
        }
    }
}