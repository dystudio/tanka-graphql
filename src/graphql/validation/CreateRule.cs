namespace tanka.graphql.validation
{
    public delegate RuleVisitor CreateRule(IRuleVisitorContext context);

    public delegate RuleVisitor CombineRules(
        IRuleVisitorContext context, 
        RuleVisitor rule);
}