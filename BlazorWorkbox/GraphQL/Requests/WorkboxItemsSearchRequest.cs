using System.Diagnostics.CodeAnalysis;
using GraphQL;

namespace BlazorWorkbox.GraphQL.Requests
{
    public static class WorkboxItemsSearchRequest
    {
        [StringSyntax("GraphQL")]
        private const string Query = """
            query WorkboxItems($workflowState: String!, $pageSize: Int!, $pageIndex: Int!)  {
              search(
                query: {
                  index: "sitecore_master_index"
                  latestVersionOnly: false
                  paging: {
                    pageSize: $pageSize
                    pageIndex: $pageIndex
                  }
                  filterStatement: {
                    criteria: [
                      { criteriaType: SEARCH, field: "__workflow_state", value: $workflowState }    
                    ]
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
              }
            }
            """;

        public static GraphQLRequest Create(Guid workflowState, int pageSize, int pageIndex)
        {
            return new GraphQLRequest()
            {
                Query = Query,
                OperationName = "WorkboxItems",
                Variables = new
                {
                    workflowState = workflowState.ToString("N"),
                    pageSize,
                    pageIndex
                }
            };
        }
    }
}
