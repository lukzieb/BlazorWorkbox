namespace BlazorWorkbox.GraphQL.Responses
{
    public record WorkflowCommandsResponse(
       WorkflowCommands Workflow
    );

    public record WorkflowCommands(
       Commands Commands
    );

    public record Commands(
        IEnumerable<Command> Nodes
        );

    public record Command(
            string DisplayName,
            Guid CommandId
        );
}
