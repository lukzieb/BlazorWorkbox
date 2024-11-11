namespace BlazorWorkbox.GraphQL.Responses
{
    public record WorkflowStatesResponse(
        WorkflowStates Workflow
        );

    public record WorkflowStates(
        States States,
        Guid WorkflowId,
        string DisplayName
        );

    public record States(
          IEnumerable<State> Nodes
          );

    public record State(
            string DisplayName,
            Guid StateId
        );
}
