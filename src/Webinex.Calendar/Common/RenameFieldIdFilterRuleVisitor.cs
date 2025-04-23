using Webinex.Asky;

namespace Webinex.Calendar;

public class RenameFieldIdFilterRuleVisitor : FilterRuleVisitor
{
    private Func<string, string?> Rename { get; }

    public RenameFieldIdFilterRuleVisitor(Func<string, string?> rename)
    {
        Rename = rename ?? throw new ArgumentNullException(nameof(rename));
    }

    public override FilterRule? Visit(ValueFilterRule valueFilterRule)
    {
        var renamed = Rename(valueFilterRule.FieldId);
        if (renamed == null) return valueFilterRule;
        
        return new ValueFilterRule(
            renamed,
            valueFilterRule.Operator,
            valueFilterRule.Value);
    }

    public override FilterRule? Visit(ChildCollectionFilterRule childCollectionFilterRule)
    {
        var renamed = Rename(childCollectionFilterRule.FieldId);
        if (renamed == null) return childCollectionFilterRule;

        return new ChildCollectionFilterRule(
            childCollectionFilterRule.Operator,
            renamed,
            childCollectionFilterRule.Rule);
    }

    public override FilterRule? Visit(CollectionFilterRule collectionFilterRule)
    {
        var renamed = Rename(collectionFilterRule.FieldId);
        if (renamed == null) return collectionFilterRule;

        return new CollectionFilterRule(
            renamed,
            collectionFilterRule.Operator,
            collectionFilterRule.Values);
    }
}