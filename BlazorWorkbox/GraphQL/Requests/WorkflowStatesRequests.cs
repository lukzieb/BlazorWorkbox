using System.Diagnostics.CodeAnalysis;
using GraphQL;

namespace BlazorWorkbox.GraphQL.Requests
{
    public static class WorkflowStatesRequests
    {
        [StringSyntax("GraphQL")]
        private static readonly string Query = """
            query WorkflowStates($workflowId: String!) {
              workflow(where: { item: { itemId: $workflowId } }) {
                workflowId
                displayName
                states {
                  nodes {
                    stateId
                    displayName
                  }
                }
              }
            }
            """;

        public static GraphQLRequest Create(Guid workflowId)
        {
            return new GraphQLRequest()
            {
                Query = Query,
                OperationName = "WorkflowStates",
                Variables = new
                {
                    workflowId,
                }
            };
        }
    }
}
