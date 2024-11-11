namespace BlazorWorkbox.GraphQL.Responses
{
    public record WorkboxItemsSearchResponse(
        Search Search
        );

    public record Search(
        int TotalCount,
        IEnumerable<SearchResults> Results,
        IEnumerable<Facet> Facets
        );

    public record SearchResults(
        string Path,
        string Name,
        string ItemId,
        string UpdatedDate,
        string UpdatedBy,
        Language Language,
        int Version,
        string TemplateName,
        InnerItem InnerItem,
        string Uri
        );

    public record InnerItem(
         Workflow Workflow
    );

    public record Workflow(
        WorkflowState WorkflowState
    );

    public record WorkflowState(
        Guid StateId
    );

    public record Language(
        string Name
        );

    public record Facet(
        string Name,
        IEnumerable<FacetItem> Facets
    );

    public record FacetItem(
        string Name
    );
}
