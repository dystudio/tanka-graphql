using GraphQLParser.AST;

namespace tanka.graphql.validation
{
    public delegate void NodeAction<in T>(T node) where T : ASTNode;

    public class NodeVisitor<T> where T : ASTNode
    {
        public NodeAction<T> Enter { get; set; }

        public NodeAction<T> Leave { get; set; }

        public static NodeVisitor<T> operator +(NodeVisitor<T> a, NodeVisitor<T> b)
        {
            if (a.Enter == null)
                a.Enter = b.Enter;
            else
                a.Enter += b.Enter;

            if (a.Leave == null)
                a.Leave = b.Leave;
            else
                a.Leave += b.Leave;

            if (a.Enter == null)
                a.Enter = node => { };

            if (a.Leave == null)
                a.Leave = node => { };

            return a;
        }
    }
}