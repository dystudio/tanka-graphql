﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using GraphQLParser.AST;
using tanka.graphql.execution;
using tanka.graphql.type;

namespace tanka.graphql.validation
{
    public static class ExecutionRules
    {
        public static CreateRule[] All =
        {
            R511ExecutableDefinitions(),
            R5211OperationNameUniqueness(),
            R5221LoneAnonymousOperation(),
            /*R5511FragmentNameUniqueness(),
            R5512FragmentSpreadTypeExistence(),
            R5513FragmentsOnCompositeTypes(),
            R5514FragmentsMustBeUsed(),*/
            R5231SingleRootField(),
            R531FieldSelections(),
            R533LeafFieldSelections(),
            /*R541ArgumentNames(),
            R542ArgumentUniqueness(),
            R5421RequiredArguments()*/
        };


        /// <summary>
        ///     Formal Specification
        ///     For each definition definition in the document.
        ///     definition must be OperationDefinition or FragmentDefinition (it must not be TypeSystemDefinition).
        /// </summary>
        public static CreateRule R511ExecutableDefinitions()
        {
            return context => new RuleVisitor
            {
                Document = new NodeVisitor<GraphQLDocument>()
                {
                    Enter = document =>
                    {
                        foreach (var definition in document.Definitions)
                        {
                            var valid = definition.Kind == ASTNodeKind.OperationDefinition
                                        || definition.Kind == ASTNodeKind.FragmentDefinition;

                            if (!valid)
                                context.Error(
                                    ValidationErrorCodes.R511ExecutableDefinitions,
                                    "GraphQL execution will only consider the " +
                                    "executable definitions Operation and Fragment. " +
                                    "Type system definitions and extensions are not " +
                                    "executable, and are not considered during execution.",
                                    definition);
                        }
                    }
                }
            };
        }


        /// <summary>
        ///     Formal Specification
        ///     For each operation definition operation in the document.
        ///     Let operationName be the name of operation.
        ///     If operationName exists
        ///     Let operations be all operation definitions in the document named operationName.
        ///     operations must be a set of one.
        /// </summary>
        public static CreateRule R5211OperationNameUniqueness()
        {
            return context =>
            {
                var known = new List<string>();
                return new RuleVisitor
                {
                    OperationDefinition = new NodeVisitor<GraphQLOperationDefinition>()
                    {
                        Enter = definition =>
                        {
                            var operationName = definition.Name?.Value;

                            if (string.IsNullOrWhiteSpace(operationName))
                                return;

                            if (known.Contains(operationName))
                                context.Error(ValidationErrorCodes.R5211OperationNameUniqueness,
                                    "Each named operation definition must be unique within a " +
                                    "document when referred to by its name.",
                                    definition);

                            known.Add(operationName);
                        }
                    }
                };
            };
        }

        /// <summary>
        ///     Let operations be all operation definitions in the document.
        ///     Let anonymous be all anonymous operation definitions in the document.
        ///     If operations is a set of more than 1:
        ///     anonymous must be empty.
        /// </summary>
        public static CreateRule R5221LoneAnonymousOperation()
        {
            return context =>
            {
                return new RuleVisitor
                {
                    Document = new NodeVisitor<GraphQLDocument>()
                    {
                        Enter = document =>
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
                };
            };
        }

        /// <summary>
        ///     For each subscription operation definition subscription in the document
        ///     Let subscriptionType be the root Subscription type in schema.
        ///     Let selectionSet be the top level selection set on subscription.
        ///     Let variableValues be the empty set.
        ///     Let groupedFieldSet be the result of CollectFields(subscriptionType, selectionSet, variableValues).
        ///     groupedFieldSet must have exactly one entry.
        /// </summary>
        public static CreateRule R5231SingleRootField()
        {
            return context => new RuleVisitor
            {
                Document = new NodeVisitor<GraphQLDocument>()
                {
                    Enter = document =>
                    {
                        var subscriptions = document.Definitions
                            .OfType<GraphQLOperationDefinition>()
                            .Where(op => op.Operation == OperationType.Subscription)
                            .ToList();

                        if (!subscriptions.Any())
                            return;

                        var schema = context.Schema;
                        //todo(pekka): should this report error?
                        if (schema.Subscription == null)
                            return;

                        var subscriptionType = schema.Subscription;
                        foreach (var subscription in subscriptions)
                        {
                            var selectionSet = subscription.SelectionSet;
                            var variableValues = new Dictionary<string, object>();

                            var groupedFieldSet = SelectionSets.CollectFields(
                                schema,
                                context.Document,
                                subscriptionType,
                                selectionSet,
                                variableValues);

                            if (groupedFieldSet.Count != 1)
                                context.Error(
                                    ValidationErrorCodes.R5231SingleRootField,
                                    "Subscription operations must have exactly one root field.",
                                    subscription);
                        }
                    }
                }
            };
        }

        /// <summary>
        ///     For each selection in the document.
        ///     Let fieldName be the target field of selection
        ///     fieldName must be defined on type in scope
        /// </summary>
        public static CreateRule R531FieldSelections()
        {
            return context => new RuleVisitor
            {
                FieldSelection = new NodeVisitor<GraphQLFieldSelection>()
                {
                    Enter = selection =>
                    {
                        var fieldName = selection.Name.Value;

                        if (fieldName == "__typename")
                            return;

                        if (context.Tracker.GetFieldDef() == null)
                            context.Error(
                                ValidationErrorCodes.R531FieldSelections,
                                "The target field of a field selection must be defined " +
                                "on the scoped type of the selection set. There are no " +
                                "limitations on alias names.",
                                selection);
                    }
                }
            };
        }

        /// <summary>
        ///     For each selection in the document
        ///     Let selectionType be the result type of selection
        ///     If selectionType is a scalar or enum:
        ///     The subselection set of that selection must be empty
        ///     If selectionType is an interface, union, or object
        ///     The subselection set of that selection must NOT BE empty
        /// </summary>
        public static CreateRule R533LeafFieldSelections()
        {
            return context => new RuleVisitor
            {
                FieldSelection = new NodeVisitor<GraphQLFieldSelection>()
                {
                    Enter = selection =>
                    {
                        var fieldName = selection.Name.Value;

                        if (fieldName == "__typename")
                            return;

                        var field = context.Tracker.GetFieldDef();

                        if (field != null)
                        {
                            var selectionType = field.Value.Field.Type;
                            var hasSubSelection = selection.SelectionSet?.Selections?.Any();

                            if (selectionType is ScalarType && hasSubSelection == true)
                                context.Error(
                                    ValidationErrorCodes.R533LeafFieldSelections,
                                    "Field selections on scalars or enums are never " +
                                    "allowed, because they are the leaf nodes of any GraphQL query.",
                                    selection);

                            if (selectionType is EnumType && hasSubSelection == true)
                                context.Error(
                                    ValidationErrorCodes.R533LeafFieldSelections,
                                    "Field selections on scalars or enums are never " +
                                    "allowed, because they are the leaf nodes of any GraphQL query.",
                                    selection);

                            if (selectionType is ComplexType && hasSubSelection == null)
                                context.Error(
                                    ValidationErrorCodes.R533LeafFieldSelections,
                                    "Leaf selections on objects, interfaces, and unions " +
                                    "without subfields are disallowed.",
                                    selection);

                            if (selectionType is UnionType && hasSubSelection == null)
                                context.Error(
                                    ValidationErrorCodes.R533LeafFieldSelections,
                                    "Leaf selections on objects, interfaces, and unions " +
                                    "without subfields are disallowed.",
                                    selection);
                        }
                    }
                }
            };
        }

        /// <summary>
        ///     For each argument in the document
        ///     Let argumentName be the Name of argument.
        ///     Let argumentDefinition be the argument definition provided by the parent field or definition named argumentName.
        ///     argumentDefinition must exist.
        /// </summary>
        public static CreateRule R541ArgumentNames()
        {
            return context => new RuleVisitor
            {
                Argument = new NodeVisitor<GraphQLArgument>()
                {
                    Enter = argument =>
                    {
                        if (context.Tracker.GetArgument() == null)
                            context.Error(
                                ValidationErrorCodes.R541ArgumentNames,
                                "Every argument provided to a field or directive " +
                                "must be defined in the set of possible arguments of that " +
                                "field or directive.",
                                argument);
                    }
                }
            };
        }

        /// <summary>
        ///     For each Field or Directive in the document.
        ///     Let arguments be the arguments provided by the Field or Directive.
        ///     Let argumentDefinitions be the set of argument definitions of that Field or Directive.
        ///     For each argumentDefinition in argumentDefinitions:
        ///     - Let type be the expected type of argumentDefinition.
        ///     - Let defaultValue be the default value of argumentDefinition.
        ///     - If type is Non‐Null and defaultValue does not exist:
        ///     - Let argumentName be the name of argumentDefinition.
        ///     - Let argument be the argument in arguments named argumentName
        ///     argument must exist.
        ///     - Let value be the value of argument.
        ///     value must not be the null literal.
        /// </summary>
        public static CreateRule R5421RequiredArguments()
        {
            return context => new RuleVisitor
            {
                FieldSelection = new NodeVisitor<GraphQLFieldSelection>()
                {
                    Enter = field =>
                    {
                        var args = field.Arguments.ToList();
                        var argumentDefinitions = GetArgumentDefinitions(context);

                        //todo: should this produce error?
                        if (argumentDefinitions == null)
                            return;

                        ValidateArguments(argumentDefinitions, args, context);
                    }
                },
                Directive = new NodeVisitor<GraphQLDirective>()
                {
                    Enter = directive =>
                    {
                        var args = directive.Arguments.ToList();
                        var argumentDefinitions = GetArgumentDefinitions(context);

                        //todo: should this produce error?
                        if (argumentDefinitions == null)
                            return;

                        ValidateArguments(argumentDefinitions, args, context);
                    }
                }
            };

            IEnumerable<KeyValuePair<string, Argument>> GetArgumentDefinitions(IRuleVisitorContext context)
            {
                var definitions = context.Tracker.GetDirective()?.Arguments
                                  ?? context.Tracker.GetFieldDef()?.Field.Arguments;

                return definitions;
            }

            void ValidateArguments(
                IEnumerable<KeyValuePair<string, Argument>> argumentDefinitions,
                List<GraphQLArgument> graphQLArguments,
                IRuleVisitorContext ruleVisitorContext)
            {
                foreach (var argumentDefinition in argumentDefinitions)
                {
                    var type = argumentDefinition.Value.Type;
                    var defaultValue = argumentDefinition.Value.DefaultValue;

                    if (!(type is NonNull nonNull) || defaultValue != null)
                        continue;

                    var argumentName = argumentDefinition.Key;
                    var argument = graphQLArguments.SingleOrDefault(a => a.Name.Value == argumentName);

                    if (argument == null)
                    {
                        ruleVisitorContext.Error(
                            ValidationErrorCodes.R5421RequiredArguments,
                            "Arguments is required. An argument is required " +
                            "if the argument type is non‐null and does not have a default " +
                            "value. Otherwise, the argument is optional. " +
                            $"Argument {argumentName} not given",
                            graphQLArguments);

                        return;
                    }

                    // We don't want to throw error here due to non-null so we use the WrappedType directly
                    var argumentValue =
                        Values.CoerceValue(ruleVisitorContext.Schema, argument.Value, nonNull.WrappedType);
                    if (argumentValue == null)
                        ruleVisitorContext.Error(
                            ValidationErrorCodes.R5421RequiredArguments,
                            "Arguments is required. An argument is required " +
                            "if the argument type is non‐null and does not have a default " +
                            "value. Otherwise, the argument is optional. " +
                            $"Value of argument {argumentName} cannot be null",
                            graphQLArguments);
                }
            }
        }

        /// <summary>
        ///     For each argument in the Document.
        ///     Let argumentName be the Name of argument.
        ///     Let arguments be all Arguments named argumentName in the Argument Set which contains argument.
        ///     arguments must be the set containing only argument.
        /// </summary>
        public static CreateRule R542ArgumentUniqueness()
        {
            return context =>
            {
                var knownArgs = new List<string>();
                return new RuleVisitor
                {
                    Argument = new NodeVisitor<GraphQLArgument>()
                    {
                        Enter = argument =>
                        {
                            if (knownArgs.Contains(argument.Name.Value))
                                context.Error(
                                    ValidationErrorCodes.R542ArgumentUniqueness,
                                    "Fields and directives treat arguments as a mapping of " +
                                    "argument name to value. More than one argument with the same " +
                                    "name in an argument set is ambiguous and invalid.",
                                    argument);

                            knownArgs.Add(argument.Name.Value);
                        }
                    }
                };
            };
        }

        /// <summary>
        ///     For each fragment definition fragment in the document
        ///     Let fragmentName be the name of fragment.
        ///     Let fragments be all fragment definitions in the document named fragmentName.
        ///     fragments must be a set of one.
        /// </summary>
        public static CreateRule R5511FragmentNameUniqueness()
        {
            return context =>
            {
                var knownFragments = new List<string>();
                return new RuleVisitor
                {
                    FragmentDefinition = new NodeVisitor<GraphQLFragmentDefinition>()
                    {
                        Enter = fragment =>
                        {
                            if (knownFragments.Contains(fragment.Name.Value))
                                context.Error(
                                    ValidationErrorCodes.R5511FragmentNameUniqueness,
                                    "Fragment definitions are referenced in fragment spreads by name. To avoid " +
                                    "ambiguity, each fragment’s name must be unique within a document.",
                                    fragment);

                            knownFragments.Add(fragment.Name.Value);
                        }
                    }
                };
            };
        }

        /// <summary>
        ///     For each named spread namedSpread in the document
        ///     Let fragment be the target of namedSpread
        ///     The target type of fragment must be defined in the schema
        /// </summary>
        public static CreateRule R5512FragmentSpreadTypeExistence()
        {
            return context => new RuleVisitor
            {
                FragmentDefinition = new NodeVisitor<GraphQLFragmentDefinition>()
                {
                    Enter = node =>
                    {
                        var type = context.Tracker.GetCurrentType();

                        if (type == null)
                            context.Error(
                                ValidationErrorCodes.R5512FragmentSpreadTypeExistence,
                                "Fragments must be specified on types that exist in the schema. This " +
                                "applies for both named and inline fragments. ",
                                node);
                    }
                },
                InlineFragment = new NodeVisitor<GraphQLInlineFragment>()
                {
                    Enter = node =>
                    {
                        var type = context.Tracker.GetCurrentType();

                        if (type == null)
                            context.Error(
                                ValidationErrorCodes.R5512FragmentSpreadTypeExistence,
                                "Fragments must be specified on types that exist in the schema. This " +
                                "applies for both named and inline fragments. ",
                                node);
                    }
                }
            };
        }

        /// <summary>
        ///     For each fragment defined in the document.
        ///     The target type of fragment must have kind UNION, INTERFACE, or OBJECT.
        /// </summary>
        public static CreateRule R5513FragmentsOnCompositeTypes()
        {
            return context => new RuleVisitor
            {
                FragmentDefinition = new NodeVisitor<GraphQLFragmentDefinition>()
                {
                    Enter = node =>
                    {
                        var type = context.Tracker.GetCurrentType();

                        if (type is ComplexType)
                            return;

                        if (type is UnionType)
                            return;

                        context.Error(
                            ValidationErrorCodes.R5513FragmentsOnCompositeTypes,
                            "Fragments can only be declared on unions, interfaces, and objects",
                            node);
                    }
                },
                InlineFragment = new NodeVisitor<GraphQLInlineFragment>()
                {
                    Enter = node =>
                    {
                        var type = context.Tracker.GetCurrentType();

                        if (type is ComplexType)
                            return;

                        if (type is UnionType)
                            return;

                        context.Error(
                            ValidationErrorCodes.R5513FragmentsOnCompositeTypes,
                            "Fragments can only be declared on unions, interfaces, and objects",
                            node);
                    }
                }
            };
        }

        /// <summary>
        ///     For each fragment defined in the document.
        ///     fragment must be the target of at least one spread in the document
        /// </summary>
        public static CreateRule R5514FragmentsMustBeUsed()
        {
            return context =>
            {
                var fragments = new Dictionary<string, GraphQLFragmentDefinition>();
                var fragmentSpreads = new List<string>();

                return new RuleVisitor
                {
                    FragmentDefinition = new NodeVisitor<GraphQLFragmentDefinition>()
                    {
                        Enter = fragment =>
                        {
                            fragments.Add(fragment.Name.Value, fragment);
                        }
                    },
                    FragmentSpread = new NodeVisitor<GraphQLFragmentSpread>()
                    {
                        Enter = spread =>
                        {
                            fragmentSpreads.Add(spread.Name.Value);
                        }
                    },
                    Document = new NodeVisitor<GraphQLDocument>()
                    {
                        Leave = document =>
                        {
                            foreach (var fragment in fragments)
                            {
                                var name = fragment.Key;
                                if (!fragmentSpreads.Contains(name))
                                    context.Error(
                                        ValidationErrorCodes.R5514FragmentsMustBeUsed,
                                        "Defined fragments must be used within a document.",
                                        fragment.Value);
                            }
                        }
                    }
                };
            };
        }
    }
}