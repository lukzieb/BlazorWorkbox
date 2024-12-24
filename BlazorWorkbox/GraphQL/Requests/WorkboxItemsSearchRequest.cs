using System.Diagnostics.CodeAnalysis;
using GraphQL;
using BlazorWorkbox.Models;
using Radzen;

namespace BlazorWorkbox.GraphQL.Requests
{
    public static class WorkboxItemsSearchRequest
    {
        private const string SolrDateFormat = "yyyy-MM-ddTHH:mm:ssZ";

        [StringSyntax("GraphQL")]
        private const string Query = """
         query WorkboxItems($workflowState: String!, $pageSize: Int!, $pageIndex: Int!, $fullPath: String!, $name: String!, $version: String!, $language: String!, $templateName: String!, $updatedBy: String!, $updated: String!, $orderBy: String!, $orderingDirection: String!)  {
           search(
             query: {
               facetOnFields: ["_templatename", "parsedupdatedby_s", "_language"]
               index: "sitecore_master_index"
               paging: {
                 pageSize: $pageSize
                 pageIndex: $pageIndex
               }
               latestVersionOnly: false
               filterStatement: {
                 criteria: [
                   { criteriaType: EXACT, operator: MUST, field: "__workflow_state", value: $workflowState },   
                   { criteriaType: WILDCARD, operator: MUST, field: "_fullpath", value: $fullPath },
                   { criteriaType: WILDCARD, operator: MUST, field: "_name", value: $name },
                   { criteriaType: WILDCARD, operator: MUST, field: "_version", value: $version },
                   { criteriaType: WILDCARD, operator: MUST, field: "_language", value: $language },
                   { criteriaType: WILDCARD, operator: MUST, field: "_templatename", value: $templateName },
                   { criteriaType: WILDCARD, operator: MUST, field: "parsedupdatedby_s", value: $updatedBy },
                   { criteriaType: RANGE, operator: MUST, field: "__smallupdateddate_tdt", value: $updated }
                 ]
               }
               sort: {
                   field: $orderBy
                   direction: $orderingDirection
              }
             }
           )
          {
               totalCount
             results {    
               path
               name
               itemId
               createdDate
               updatedDate
               updatedBy
               language{
                 name
               }
               version
               templateName
             }
           facets{
             name
             facets{
               name
               count
             }
           }
           }
         }
         """;


        private static readonly Dictionary<string, string> OrderingMapping = new()
     {
         { nameof(WorkboxItem.Path), "_fullpath" },
         { nameof(WorkboxItem.Name), "_name" },
         { nameof(WorkboxItem.TemplateName), "_templatename" },
         { nameof(WorkboxItem.Updated), "__smallupdateddate_tdt" },
         { nameof(WorkboxItem.UpdatedBy), "parsedupdatedby_s" },
         { nameof(WorkboxItem.Version), "_version" },
     };

        public static GraphQLRequest Create(Guid workflowState, int pageSize, int pageIndex, Dictionary<string, object> filters, string orderingProperty, SortOrder? sortOrder)
        {
            string fullPath = GetFilterStringValue(nameof(WorkboxItem.Path), filters, x => $"*{x}*");
            string name = GetFilterStringValue(nameof(WorkboxItem.Name), filters);
            string version = GetFilterStringValue(nameof(WorkboxItem.Version), filters);
            string language = GetFilterStringValue(nameof(WorkboxItem.Language), filters);
            string templateName = GetFilterStringValue(nameof(WorkboxItem.TemplateName), filters);
            string updatedBy = GetFilterStringValue(nameof(WorkboxItem.UpdatedBy), filters);

            DateTime?[] updatedRange = filters.GetValueOrDefault(nameof(WorkboxItem.Updated)) as DateTime?[];
            DateTime updatedFrom = updatedRange?[0] ?? DateTime.MinValue;
            DateTime updatedTo = updatedRange?[1] ?? DateTime.MaxValue;
            string updated = $"[{updatedFrom.ToString(SolrDateFormat)} TO {updatedTo.ToString(SolrDateFormat)}]";

            string orderBy = OrderingMapping.GetValueOrDefault(orderingProperty ?? nameof(WorkboxItem.UpdatedBy)) ?? "parsedupdatedby_s";
            string orderingDirection = sortOrder == SortOrder.Descending ? "DESCENDING" : "ASCENDING";

            return new GraphQLRequest()
            {
                Query = Query,
                OperationName = "WorkboxItems",
                Variables = new
                {
                    workflowState = workflowState.ToString("N"),
                    pageSize,
                    pageIndex,
                    fullPath,
                    name,
                    version,
                    language,
                    templateName,
                    updatedBy,
                    updated,
                    orderBy,
                    orderingDirection
                }
            };
        }

        private static string GetFilterStringValue(string key, Dictionary<string, object> filters, Func<string, string> transormQuery = null)
        {
            string? value = filters.GetValueOrDefault(key) as string;

            if (string.IsNullOrEmpty(value))
            {
                return "*";
            }

            if (transormQuery != null)
            {
                value = transormQuery(value);
            }

            return value;
        }
    }
}
