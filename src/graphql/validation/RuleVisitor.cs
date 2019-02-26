using GraphQLParser.AST;

namespace tanka.graphql.validation
{
    public class RuleVisitor
    {
        public NodeVisitor<GraphQLName> Alias { get; set; }

        public NodeVisitor<GraphQLArgument> Argument { get; set;  }

        public NodeVisitor<GraphQLScalarValue> BooleanValue { get; set;  }

        public NodeVisitor<GraphQLDirective> Directive { get; set;  }

        public NodeVisitor<GraphQLDocument> Document { get; set;  }

        public NodeVisitor<GraphQLScalarValue> EnumValue { get; set;  }

        public NodeVisitor<GraphQLFieldSelection> FieldSelection { get; set;  }

        public NodeVisitor<GraphQLScalarValue> FloatValue { get; set;  }

        public NodeVisitor<GraphQLFragmentDefinition> FragmentDefinition { get; set;  }

        public NodeVisitor<GraphQLFragmentSpread> FragmentSpread { get; set;  }

        public NodeVisitor<GraphQLInlineFragment> InlineFragment { get; set;  }

        public NodeVisitor<GraphQLScalarValue> IntValue { get; set;  }

        public NodeVisitor<GraphQLListValue> ListValue { get; set;  }

        public NodeVisitor<GraphQLName> Name { get; set; }

        public NodeVisitor<GraphQLNamedType> NamedType{ get; set;  }

        public NodeVisitor<GraphQLObjectField> ObjectField{ get; set;  }

        public NodeVisitor<GraphQLObjectValue> ObjectValue{ get; set;  }

        public NodeVisitor<GraphQLOperationDefinition> OperationDefinition{ get; set;  }

        public NodeVisitor<GraphQLSelectionSet> SelectionSet{ get; set;  }

        public NodeVisitor<GraphQLScalarValue> StringValue{ get; set;  }

        public NodeVisitor<GraphQLVariable> Variable{ get; set;  }

        public NodeVisitor<GraphQLVariableDefinition> VariableDefinition{ get; set;  }
    }
}