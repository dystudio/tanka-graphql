using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using GraphQLParser.AST;
using tanka.graphql.language;
using tanka.graphql.type;

namespace tanka.graphql.validation
{
    public class RulesWalker : Visitor, IRuleVisitorContext
    {
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public RulesWalker(
            CreateRule[] rules,
            ISchema schema,
            GraphQLDocument document,
            Dictionary<string, object> variableValues = null)
        {
            Schema = schema;
            Document = document;
            VariableValues = variableValues;
            _nodeVisitors = CreateVisitors(rules).ToArray();

            //todo: this will break
            Tracker = _nodeVisitors[0] as TypeTracker;
        }

        private readonly RuleVisitor[] _nodeVisitors;

        protected RuleVisitor[] CreateVisitors(CreateRule[] rules)
        {
            return new[]
            {
                CreateCombinedRuleVisitor(rules)
            };
        }

        private RuleVisitor CreateCombinedRuleVisitor(CreateRule[] rules)
        {
            var result = new TypeTracker(Schema);

            foreach (var createRule in rules)
            {
                var rule = createRule(this);

                if (rule.Document?.Enter != null)
                {
                    result.Document.Enter += rule.Document.Enter;
                }

                if (rule.Document?.Leave != null)
                {
                    result.Document.Leave += rule.Document.Leave;
                }

                if (rule.OperationDefinition?.Enter != null)
                    result.OperationDefinition.Enter += rule.OperationDefinition.Enter;

                if (rule.OperationDefinition?.Leave != null)
                    result.OperationDefinition.Leave += rule.OperationDefinition.Leave;

                if (rule.FieldSelection?.Enter != null)
                    result.FieldSelection.Enter += rule.FieldSelection.Enter;

                if (rule.FieldSelection?.Leave != null)
                    result.FieldSelection.Leave += rule.FieldSelection.Leave;
            }

            return result;
        }

        public GraphQLDocument Document { get; }

        public IDictionary<string, object> VariableValues { get; }

        public TypeTracker Tracker { get; }

        public ISchema Schema { get; }

        public void Error(string code, string message, params ASTNode[] nodes)
        {
            _errors.Add(new ValidationError(code, message, nodes));
        }

        public void Error(string code, string message, ASTNode node)
        {
            _errors.Add(new ValidationError(code, message, node));
        }

        public void Error(string code, string message, IEnumerable<ASTNode> nodes)
        {
            _errors.Add(new ValidationError(code, message, nodes));
        }

        public ValidationResult Validate()
        {
            Visit(Document);
            return BuildResult();
        }

        private NodeVisitor<T>[] GetVisitors<T>(Func<RuleVisitor, NodeVisitor<T>> selector) where T: ASTNode
        {
            var nodeVisitors = _nodeVisitors
                .Select(selector);

            return nodeVisitors.ToArray();
        }

        public override void Visit(GraphQLDocument document)
        {
            var visitors = _nodeVisitors;
            foreach (var visitor in visitors)
            {
                visitor?.Document.Enter?.Invoke(document);
            }

            base.Visit(document);

            foreach (var visitor in visitors)
            {
                visitor?.Document.Leave?.Invoke(document);
            }
        }

        public override GraphQLName BeginVisitAlias(GraphQLName alias)
        {
            foreach (var visitor in GetVisitors(r => r.Alias))
            {
                visitor?.Enter?.Invoke(alias);
            }

            return base.BeginVisitAlias(alias);
        }

        public override GraphQLArgument BeginVisitArgument(GraphQLArgument argument)
        {
            foreach (var visitor in GetVisitors(r => r.Argument))
            {
                visitor?.Enter?.Invoke(argument);
            }

            return base.BeginVisitArgument(argument);
        }

        public override GraphQLScalarValue BeginVisitBooleanValue(
            GraphQLScalarValue value)
        {
            foreach (var visitor in GetVisitors(r => r.BooleanValue))
            {
                visitor?.Enter?.Invoke(value);
            }

            return base.BeginVisitBooleanValue(value);
        }

        public override GraphQLDirective BeginVisitDirective(GraphQLDirective directive)
        {
            var visitors = GetVisitors(r => r.Directive);

            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(directive);
            }

            var _ = base.BeginVisitDirective(directive);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(directive);
            }

            return _;
        }

        public override GraphQLScalarValue BeginVisitEnumValue(GraphQLScalarValue value)
        {
            var visitors = GetVisitors(r => r.EnumValue);
            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(value);
            }

            var _ = base.BeginVisitEnumValue(value);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(value);
            }

            return _;
        }

        public override GraphQLFieldSelection BeginVisitFieldSelection(
            GraphQLFieldSelection selection)
        {
            foreach (var visitor in GetVisitors(r => r.FieldSelection))
            {
                visitor?.Enter?.Invoke(selection);
            }

            return base.BeginVisitFieldSelection(selection);
        }

        public override GraphQLScalarValue BeginVisitFloatValue(
            GraphQLScalarValue value)
        {
            foreach (var visitor in GetVisitors(r => r.FloatValue))
            {
                visitor?.Enter?.Invoke(value);
            }

            return base.BeginVisitFloatValue(value);
        }

        public override GraphQLFragmentDefinition BeginVisitFragmentDefinition(
            GraphQLFragmentDefinition node)
        {
            var visitors = GetVisitors(r => r.FragmentDefinition);
            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(node);
            }

            var result = base.BeginVisitFragmentDefinition(node);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(node);
            }

            return result;
        }

        public override GraphQLFragmentSpread BeginVisitFragmentSpread(
            GraphQLFragmentSpread fragmentSpread)
        {
            foreach (var visitor in GetVisitors(r => r.FragmentSpread))
            {
                visitor?.Enter?.Invoke(fragmentSpread);
            }

            return base.BeginVisitFragmentSpread(fragmentSpread);
        }

        public override GraphQLInlineFragment BeginVisitInlineFragment(
            GraphQLInlineFragment inlineFragment)
        {
            var visitors = GetVisitors(r => r.InlineFragment);
            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(inlineFragment);
            }

            var _ = base.BeginVisitInlineFragment(inlineFragment);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(inlineFragment);
            }

            return _;
        }

        public override GraphQLScalarValue BeginVisitIntValue(GraphQLScalarValue value)
        {
            foreach (var visitor in GetVisitors(r => r.IntValue))
            {
                visitor?.Enter?.Invoke(value);
            }

            return base.BeginVisitIntValue(value);
        }

        public override GraphQLName BeginVisitName(GraphQLName name)
        {
            foreach (var visitor in GetVisitors(r => r.Name))
            {
                visitor?.Enter?.Invoke(name);
            }

            return base.BeginVisitName(name);
        }

        public override GraphQLNamedType BeginVisitNamedType(
            GraphQLNamedType typeCondition)
        {
            foreach (var visitor in GetVisitors(r => r.NamedType))
            {
                visitor?.Enter?.Invoke(typeCondition);
            }

            return base.BeginVisitNamedType(typeCondition);
        }

        public override GraphQLOperationDefinition BeginVisitOperationDefinition(
            GraphQLOperationDefinition definition)
        {
            foreach (var visitor in GetVisitors(r => r.OperationDefinition))
            {
                visitor?.Enter?.Invoke(definition);
            }

            return base.BeginVisitOperationDefinition(definition);
        }

        public override GraphQLOperationDefinition EndVisitOperationDefinition(
            GraphQLOperationDefinition definition)
        {
            foreach (var visitor in GetVisitors(r => r.OperationDefinition))
            {
                visitor?.Leave?.Invoke(definition);
            }

            return base.EndVisitOperationDefinition(definition);
        }

        public override GraphQLSelectionSet BeginVisitSelectionSet(
            GraphQLSelectionSet selectionSet)
        {
            var visitors = GetVisitors(r => r.SelectionSet);
            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(selectionSet);
            }

            var _ = base.BeginVisitSelectionSet(selectionSet);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(selectionSet);
            }

            return _;
        }

        public override GraphQLScalarValue BeginVisitStringValue(
            GraphQLScalarValue value)
        {
            foreach (var visitor in GetVisitors(r => r.StringValue))
            {
                visitor?.Enter?.Invoke(value);
            }

            return base.BeginVisitStringValue(value);
        }

        public override GraphQLVariable BeginVisitVariable(GraphQLVariable variable)
        {
            foreach (var visitor in GetVisitors(r => r.Variable))
            {
                visitor?.Enter?.Invoke(variable);
            }

            return base.BeginVisitVariable(variable);
        }

        public override GraphQLVariableDefinition BeginVisitVariableDefinition(
            GraphQLVariableDefinition node)
        {
            var visitors = GetVisitors(r => r.VariableDefinition);
            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(node);
            }

            var _ = base.BeginVisitVariableDefinition(node);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(node);
            }

            return _;
        }

        public override GraphQLArgument EndVisitArgument(GraphQLArgument argument)
        {
            foreach (var visitor in GetVisitors(r => r.Argument))
            {
                visitor?.Leave?.Invoke(argument);
            }

            return base.EndVisitArgument(argument);
        }

        public override GraphQLFieldSelection EndVisitFieldSelection(
            GraphQLFieldSelection selection)
        {
            foreach (var visitor in GetVisitors(r => r.FieldSelection))
            {
                visitor?.Leave?.Invoke(selection);
            }

            return base.EndVisitFieldSelection(selection);
        }

        public override GraphQLVariable EndVisitVariable(GraphQLVariable variable)
        {
            foreach (var visitor in GetVisitors(r => r.Variable))
            {
                visitor?.Leave?.Invoke(variable);
            }

            return base.EndVisitVariable(variable);
        }

        public override GraphQLObjectField BeginVisitObjectField(
            GraphQLObjectField node)
        {
            var visitors = GetVisitors(r => r.ObjectField);
            foreach (var visitor in visitors)
            {
                visitor?.Enter?.Invoke(node);
            }

            var _ = base.BeginVisitObjectField(node);

            foreach (var visitor in visitors)
            {
                visitor?.Leave?.Invoke(node);
            }

            return _;
        }

        public override GraphQLObjectValue BeginVisitObjectValue(
            GraphQLObjectValue node)
        {
            foreach (var visitor in GetVisitors(r => r.ObjectValue))
            {
                visitor?.Enter?.Invoke(node);
            }

            return base.BeginVisitObjectValue(node);
        }

        public override GraphQLObjectValue EndVisitObjectValue(GraphQLObjectValue node)
        {
            foreach (var visitor in GetVisitors(r => r.ObjectValue))
            {
                visitor?.Leave?.Invoke(node);
            }

            return base.EndVisitObjectValue(node);
        }

        /*public override ASTNode BeginVisitNode(ASTNode node)
        {
            foreach (var visitor in GetVisitors(r => r.Node))
            {
                visitor?.Enter?.Invoke(node);
            }

            return base.BeginVisitNode(node);
        }*/

        public override GraphQLListValue BeginVisitListValue(GraphQLListValue node)
        {
            foreach (var visitor in GetVisitors(r => r.ListValue))
            {
                visitor?.Enter?.Invoke(node);
            }

            return base.BeginVisitListValue(node);
        }

        public override GraphQLListValue EndVisitListValue(GraphQLListValue node)
        {
            foreach (var visitor in GetVisitors(r => r.ListValue))
            {
                visitor?.Leave?.Invoke(node);
            }

            return base.EndVisitListValue(node);
        }

        private ValidationResult BuildResult()
        {
            return new ValidationResult
            {
                Errors = _errors
            };
        }
    }
}