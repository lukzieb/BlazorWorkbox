using System.Diagnostics.CodeAnalysis;
using GraphQL;

namespace BlazorWorkbox.GraphQL.Requests
{
    public static class WorkflowCommandsRequest
    {
        [StringSyntax("GraphQL")]
        private static readonly string Query = """
            query WorkflowCommands($workflowId: String!, $stateId: String!) {
                workflow(where: { item: { itemId: $workflowId } }) {
                  commands(query: { stateId: $stateId }) {
                    nodes {
                      displayName
                      commandId
                    }
                  }
                }
            }
            """;

        public static GraphQLRequest Create(Guid workflowId, Guid stateId)
        {
            return new GraphQLRequest()
            {
                Query = Query,
                OperationName = "WorkflowCommands",
                Variables = new
                {
                    workflowId,
                    stateId,
                }
            };
        }
    }
}
