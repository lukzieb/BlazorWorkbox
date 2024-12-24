using BlazorWorkbox.GraphQL.Requests;
using BlazorWorkbox.GraphQL.Responses;
using BlazorWorkbox.Models;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor;

namespace BlazorWorkbox.Components
{
    public partial class Workbox : ComponentBase
    {
        private readonly IEnumerable<int> _pageSizeOptions = [10, 25, 50];
        private readonly Dictionary<string, object> _filterValues = [];

        private IEnumerable<WorkboxItem> _items = [];

        private int _totalRecordCount;

        private int _pageIndex;
        private int _pageSize;

        private string _path;
        private string _name;
        private string _language;
        private string _version;
        private string _templateName;
        private string _updatedBy;
        private DateTime? _updatedFrom;
        private DateTime? _updatedTo;

        private string _sortBy = nameof(WorkboxItem.Updated);
        private SortOrder _sortOrder = SortOrder.Descending;
        private SortOrder? _defaultSortOrder;

        private IEnumerable<FacetItem> _templateNames = [];
        private IEnumerable<KeyValuePair<string, string>> _updatedByNames = [];
        private IEnumerable<FacetItem> _languages = [];

        private Guid _workflowId;
        private Guid _stateId;
        private Guid _commandId;

        private IEnumerable<KeyValuePair<Guid, string>> _commandData;
        private IEnumerable<KeyValuePair<Guid, string>> _stateData;
        private IEnumerable<KeyValuePair<Guid, string>> _workflowsData;

        Dictionary<Guid, IEnumerable<KeyValuePair<Guid, string>>> _commandsForState = [];
        Dictionary<Guid, IEnumerable<KeyValuePair<Guid, string>>> _statesForWorkflow = [];

        private bool _isLoading = true;

        private RadzenPager _pager;

        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }

        [Inject]
        private IOptions<AppSettings> AppSettings { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _pageSize = 10;

            await LoadWorkflows();
            await LoadWorkboxData();
        }

        private async Task LoadWorkflows()
        {
            IEnumerable<Task<GraphQLResponse<WorkflowStatesResponse>>> stateRequests = AppSettings.Value.Workflows.Select(async x => await GraphQLClient.SendQueryAsync<WorkflowStatesResponse>(WorkflowStatesRequests.Create(x)));

            GraphQLResponse<WorkflowStatesResponse>[] stateResponses = await Task.WhenAll(stateRequests);

            _statesForWorkflow = stateResponses
                .Select(x => new KeyValuePair<Guid, IEnumerable<KeyValuePair<Guid, string>>>(x.Data.Workflow.WorkflowId, x.Data.Workflow.States.Nodes.Select(s => new KeyValuePair<Guid, string>(s.StateId, s.DisplayName)))).ToDictionary();

            IEnumerable<(Guid WorkflowId, Guid StatedId)> stateAndWorkflows = stateResponses.SelectMany(x => x.Data.Workflow.States.Nodes.Select(z => (x.Data.Workflow.WorkflowId, z.StateId)));

            IEnumerable<Task<(Guid StatedId, GraphQLResponse<WorkflowCommandsResponse>)>> commandsRequests = stateAndWorkflows
                .Select(async x => (x.StatedId, await GraphQLClient.SendQueryAsync<WorkflowCommandsResponse>(WorkflowCommandsRequest.Create(x.WorkflowId, x.StatedId))));

            (Guid StatedId, GraphQLResponse<WorkflowCommandsResponse> Response)[] commandsRespones = await Task.WhenAll(commandsRequests);

            _commandsForState = commandsRespones
                .Select(x => new KeyValuePair<Guid, IEnumerable<KeyValuePair<Guid, string>>>(x.StatedId, x.Response.Data.Workflow.Commands.Nodes.Where(y => !y.DisplayName.StartsWith('_')).Select(z => new KeyValuePair<Guid, string>(z.CommandId, z.DisplayName)))).ToDictionary();

            _workflowsData = stateResponses.Select(x => new KeyValuePair<Guid, string>(x.Data.Workflow.WorkflowId, x.Data.Workflow.DisplayName));
            _workflowId = _workflowsData.FirstOrDefault().Key;
            _stateData = _statesForWorkflow[_workflowId];
            _stateId = _stateData.FirstOrDefault().Key;

            _commandData = _commandsForState[_stateId];
        }

        private async Task OnWorkflowValueChanged(object value)
        {
            _stateData = _statesForWorkflow[_workflowId];
            _stateId = _stateData.FirstOrDefault().Key;

            await OnWorkflowStateChanged(_stateId);
        }

        private async Task OnWorkflowStateChanged(object value)
        {
            _commandData = _commandsForState[_stateId];

            await _pager.FirstPage(true);
        }


        private async Task OnPageChanged(PagerEventArgs args)
        {
            _pageIndex = args.PageIndex;

            await LoadWorkboxData();
        }

        private async Task OnPageSizeChanged(int pageSize)
        {
            _pageSize = pageSize;

            await LoadWorkboxData();
        }

        private async Task OnFilterChanged(object value)
        {
            await _pager.FirstPage(true);
        }

        private async Task OnFilterChanged(DateTime? value)
        {
            await _pager.FirstPage(true);
        }

        private async Task OnSort(DataGridColumnSortEventArgs<WorkboxItem> args)
        {
            _sortBy = args.Column.Property;
            _sortOrder = args.SortOrder ?? SortOrder.Ascending;

            _defaultSortOrder = args.Column.Property == nameof(WorkboxItem.Updated) ? _sortOrder : null;

            await _pager.GoToPage(_pageIndex, true);

            args.Column.SortOrder = _sortOrder;
        }

        private void UpdateFilters()
        {
            _filterValues[nameof(WorkboxItem.Path)] = _path;
            _filterValues[nameof(WorkboxItem.Language)] = _language;
            _filterValues[nameof(WorkboxItem.Version)] = _version;
            _filterValues[nameof(WorkboxItem.TemplateName)] = _templateName;
            _filterValues[nameof(WorkboxItem.UpdatedBy)] = _updatedBy;
            _filterValues[nameof(WorkboxItem.Updated)] = new DateTime?[] { _updatedFrom, _updatedTo };
        }


        private async Task LoadWorkboxData()
        {
            _isLoading = true;
            UpdateFilters();

            GraphQLRequest request = WorkboxItemsSearchRequest.Create(_stateId, _pageSize, _pageIndex, _filterValues, _sortBy, _sortOrder);
              GraphQLResponse<WorkboxItemsSearchResponse> result = await GraphQLClient.SendQueryAsync<WorkboxItemsSearchResponse>(request);

            if (result.Data == null)
            {
                return;
            }

            _totalRecordCount = result.Data.Search.TotalCount;
            _items = result.Data.Search.Results.Select(x => new WorkboxItem
            {
                Path = x.Path,
                Name = x.Name,
                Updated = DateTime.Parse(x.UpdatedDate),
                UpdatedBy = x.UpdatedBy.Replace("sitecore", "sitecore/"),
                Language = x.Language.Name,
                Version = x.Version,
                TemplateName = x.TemplateName
            });

            _templateNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_templatename")?.Facets.OrderBy(x => x.Name);
            _updatedByNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "parsedupdatedby")?.Facets.OrderBy(x => x.Name)
                  .Select(x => new KeyValuePair<string, string>(x.Name, x.Name.Replace("sitecore", "sitecore/")));
            _languages = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_language")?.Facets.OrderBy(x => x.Name);

            _isLoading = false;
        }
    }
}
