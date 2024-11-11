using System.Collections.Specialized;
using System.Web;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.AspNetCore.Components;
using Radzen;
using BlazorWorkbox.GraphQL.Responses;
using BlazorWorkbox.GraphQL.Requests;
using BlazorWorkbox.Models;

namespace BlazorWorkbox.Components
{
    public partial class WorkflowProcessingDialog : ComponentBase
    {
        private bool ShowSubmit = true;
        private bool IsLoading;
        private string Comments;

        private IEnumerable<int> PageSizeOptions = new[] { 10, 30, 50 };

        [Parameter]
        public IList<WorkboxItem> Items { get; set; }

        [Parameter]
        public Guid CommandId { get; set; }

        [Parameter]
        public Guid WorkflowStateId { get; set; }

        Dictionary<string, (bool Successful, string ErrorMessage)> CommandExecutionResults = new Dictionary<string, (bool Successful, string ErrorMessage)>();

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }


        private async Task OnSubmit()
        {
            IsLoading = true;

            foreach (var item in Items)
            {
                Uri uri = new Uri(item.ItemUri);
                NameValueCollection parameters = HttpUtility.ParseQueryString(uri.Query);

                GraphQLResponse<ExecuteWorkflowCommandResponse> commandResponse = await GraphQLClient.SendQueryAsync<ExecuteWorkflowCommandResponse>(ExecuteWorkflowCommandRequest.Create(CommandId, uri.LocalPath.Trim('/'), int.Parse(parameters["ver"]), parameters["lang"], uri.Host, Comments));

                ExecuteWorkflowCommand executeWorkflowCommandResult = commandResponse.Data.ExecuteWorkflowCommand;

                CommandExecutionResults.Add(item.ItemUri,
                    new(executeWorkflowCommandResult.Successful, executeWorkflowCommandResult.Error));
            }

            ShowSubmit = false;
            IsLoading = false;
        }

        private void OnCancel()
        {
            DialogService.Close(Enumerable.Empty<WorkboxItem>());
        }

        private void OnDone()
        {
            IEnumerable<WorkboxItem> itemsProcessed = Items.Where(x =>
            {
                if (CommandExecutionResults.TryGetValue(x.ItemUri, out var value))
                {
                    return value.Successful;
                }
                return false;
            });

            DialogService.Close(itemsProcessed);
        }
    }
}
