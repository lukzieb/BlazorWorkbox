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
        private readonly IEnumerable<int> _pageSizeOptions = [10, 25, 50];
        private readonly Dictionary<string, (bool Successful, string ErrorMessage)> _commandExecutionResults = [];

        private bool _showSubmit = true;
        private bool _isLoading;
        private string _comments;

        [Parameter]
        public IList<WorkboxItem> Items { get; set; }

        [Parameter]
        public Guid CommandId { get; set; }

        [Parameter]
        public Guid WorkflowStateId { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }


        private async Task OnSubmit()
        {
            _isLoading = true;

            foreach (WorkboxItem item in Items)
            {
                Uri uri = new(item.ItemUri);
                NameValueCollection parameters = HttpUtility.ParseQueryString(uri.Query);

                GraphQLResponse<ExecuteWorkflowCommandResponse> commandResponse = await GraphQLClient.SendQueryAsync<ExecuteWorkflowCommandResponse>(ExecuteWorkflowCommandRequest.Create(CommandId, uri.LocalPath.Trim('/'), int.Parse(parameters["ver"]), parameters["lang"], uri.Host, _comments));

                ExecuteWorkflowCommand executeWorkflowCommandResult = commandResponse.Data.ExecuteWorkflowCommand;

                _commandExecutionResults.Add(item.ItemUri,
                    new(executeWorkflowCommandResult.Successful, executeWorkflowCommandResult.Error));
            }

            _showSubmit = false;
            _isLoading = false;
        }

        private void OnCancel()
        {
            DialogService.Close(Enumerable.Empty<WorkboxItem>());
        }

        private void OnDone()
        {
            IEnumerable<WorkboxItem> itemsProcessed = Items.Where(x =>
            {
                if (_commandExecutionResults.TryGetValue(x.ItemUri, out var value))
                {
                    return value.Successful;
                }
                return false;
            });

            DialogService.Close(itemsProcessed);
        }
    }
}
