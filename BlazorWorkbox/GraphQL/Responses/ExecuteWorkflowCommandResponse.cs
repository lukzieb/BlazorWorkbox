namespace BlazorWorkbox.GraphQL.Responses
{
    public record ExecuteWorkflowCommandResponse(
        ExecuteWorkflowCommand ExecuteWorkflowCommand
        );

    public record ExecuteWorkflowCommand(
        bool Successful,
        string Error
    );
}