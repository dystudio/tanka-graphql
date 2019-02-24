﻿using System.Collections.Generic;
using System.Linq;
using GraphQLParser.AST;

namespace tanka.graphql.validation.rules2
{
    /// <summary>
    ///     Let operations be all operation definitions in the document.
    ///     Let anonymous be all anonymous operation definitions in the document.
    ///     If operations is a set of more than 1:
    ///     anonymous must be empty.
    /// </summary>
    public class R5221LoneAnonymousOperation : RuleBase
    {
        public override IEnumerable<ASTNodeKind> AppliesToNodeKinds => new[]
        {
            ASTNodeKind.Document
        };

        public override void Visit(GraphQLDocument document, IValidationContext context)
        {
            var operations = document.Definitions
                .OfType<GraphQLOperationDefinition>()
                .ToList();

            var anonymous = operations
                .Count(op => string.IsNullOrEmpty(op.Name?.Value));

            if (operations.Count() > 1)
                if (anonymous > 0)
                    context.Error(
                        ValidationErrorCodes.R5221LoneAnonymousOperation,
                        "GraphQL allows a short‐hand form for defining " +
                        "query operations when only that one operation exists in " +
                        "the document.",
                        operations);
        }
    }
}