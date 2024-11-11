namespace BlazorWorkbox.GraphQL.Responses
{
    public record WorkboxItemsSearchResponse(
        Search Search
        );

    public record Search(
        int TotalCount,
        IEnumerable<SearchResults> Results
        );

    public record SearchResults(
        string Path,
        string Name,
        string ItemId,
        string UpdatedDate,
        string UpdatedBy,
        Language Language,
        int Version,
        string TemplateName
        );

    public record Language(
        string Name
        );
}
