using System.Diagnostics.CodeAnalysis;
using GraphQL;

namespace BlazorWorkbox.GraphQL.Requests
{
    public static class ExecuteWorkflowCommandRequest
    {
        [StringSyntax("GraphQL")]
        private static readonly string Query = """
                mutation ExecuteWorkflowCommand(
                  $commandId: String!
                  $itemId: String!
                  $version: Int
                  $language: String
                  $database: String
                  $comments: String
                ) {
                  executeWorkflowCommand(
                    input: {
                      comments: $comments
                      commandId: $commandId
                      item: {
                        database: $database
                        version: $version
                        language: $language
                        itemId: $itemId
                      }
                    }
                  ) {
                    successful
                    error
                  }
                }
                """;

        public static GraphQLRequest Create(Guid commandId, string itemId, int version, string language, string database, string comments)
        {
            return new GraphQLRequest()
            {
                Query = Query,
                OperationName = "ExecuteWorkflowCommand",
                Variables = new
                {
                    commandId,
                    itemId,
                    version,
                    language,
                    database,
                    comments
                }
            };
        }
    }
}