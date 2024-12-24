using Blazored.LocalStorage;
using BlazorWorkbox.GraphQL.Requests;
using BlazorWorkbox.GraphQL.Responses;
using BlazorWorkbox.Models;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Radzen;
using Radzen.Blazor;
using System;

namespace BlazorWorkbox.Components
{
    public partial class Workbox : ComponentBase
    {
        private const string SelectedItemsLocalStorageKey = "selectedItems";

        private readonly IEnumerable<int> _pageSizeOptions = [10, 25, 50];
        private readonly Dictionary<string, object> _filterValues = [];

        private IEnumerable<WorkboxItem> _items = [];
        private List<WorkboxItem> _selectedItems = [];

        private int _selectedItemsCount;
        private bool _showOnlySelectedItems;
        private int _totalRecordCount;

        private string _sortBy = nameof(WorkboxItem.Updated);
        private SortOrder _sortOrder = SortOrder.Descending;
        private SortOrder? _defaultSortOrder;

        private string _name;

        private IEnumerable<FacetItem> _templateNames = [];
        private IEnumerable<KeyValuePair<string, string>> _updatedByNames = [];
        private IEnumerable<FacetItem> _languages = [];

        private Guid _workflowId;
        private Guid _commandId;
        private string _stateDisplayName;

        private IEnumerable<KeyValuePair<Guid, string>> _commandData;
        private IEnumerable<KeyValuePair<Guid, string>> _stateData;
        private IEnumerable<KeyValuePair<Guid, string>> _workflowsData;

        private Dictionary<Guid, IEnumerable<KeyValuePair<Guid, string>>> _commandsForState = [];
        private Dictionary<Guid, IEnumerable<KeyValuePair<Guid, string>>> _statesForWorkflow = [];

        private bool _isLoading = true;

        private RadzenPager _pager;
        RadzenDropDown<Guid> _commandsDropDown;

        [SupplyParameterFromQuery]
        private int PageIndex { get; set; }

        [SupplyParameterFromQuery]
        public int PageSize { get; set; }

        [SupplyParameterFromQuery]
        private string Path { get; set; }

        [SupplyParameterFromQuery]
        private string Language { get; set; }

        [SupplyParameterFromQuery]
        private string Version { get; set; }

        [SupplyParameterFromQuery]
        private string TemplateName { get; set; }

        [SupplyParameterFromQuery]
        private string UpdatedBy { get; set; }

        [SupplyParameterFromQuery]
        private DateTime? UpdatedFrom { get; set; }

        [SupplyParameterFromQuery]
        private DateTime? UpdatedTo { get; set; }

        [SupplyParameterFromQuery]
        private Guid StateId { get; set; }

        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }

        [Inject]
        private IOptions<AppSettings> AppSettings { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        [Inject]
        private ILocalStorageService LocalStorgeService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            PageSize = 10;

            await LoadWorkflows();
            await LoadWorkboxData();

            await LoadState();
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
            Guid resolvedWorkflow = _statesForWorkflow.FirstOrDefault(x => x.Value.Any(y => y.Key == StateId)).Key;
            _workflowId = _workflowsData.Any(x => x.Key == resolvedWorkflow) ? resolvedWorkflow : _workflowsData.FirstOrDefault().Key;

            _stateData = _statesForWorkflow[_workflowId];
            StateId = _stateData.Any(x => x.Key == StateId) ? StateId : _stateData.FirstOrDefault().Key;

            _commandData = _commandsForState[StateId];

            _stateDisplayName = _stateData.FirstOrDefault(x => x.Key == StateId).Value ?? _stateData.FirstOrDefault().Value;
        }

        private async Task OnWorkflowValueChanged(object value)
        {
            _stateData = _statesForWorkflow[_workflowId];
            StateId = _stateData.FirstOrDefault().Key;

            await OnWorkflowStateChanged(StateId);
        }

        private async Task OnWorkflowStateChanged(object value)
        {
            _commandData = _commandsForState[StateId];
            _stateDisplayName = _stateData.FirstOrDefault(x => x.Key == StateId).Value;

            RecalculateSelectedItems();

            await _pager.FirstPage(true);
        }

        private async Task OnWorkflowCommandChanged(object value)
        {
            string commandDisplayName = _commandData.FirstOrDefault(x => x.Key == _commandId).Value;

            IEnumerable<WorkboxItem> itemsProcessed = await DialogService.OpenAsync<WorkflowProcessingDialog>(
               $"Execute Workflow Command: {commandDisplayName}",
               new Dictionary<string, object>
               {
                    { nameof(WorkflowProcessingDialog.Items), _selectedItems.Where(x => x.WorkflowStateId == StateId).OrderBy(x => x.Path).ToList() },
                    { nameof(WorkflowProcessingDialog.WorkflowStateId), StateId },
                    { nameof(WorkflowProcessingDialog.CommandId), _commandId }
               },
               new DialogOptions { Width = "100%", Draggable = true, Resizable = true, ShowClose = false });

            if (itemsProcessed.Any())
            {
                _selectedItems = _selectedItems.Where(x => !itemsProcessed.Any(y => y.ItemUri == x.ItemUri)).ToList();
                RecalculateSelectedItems();
                await SaveSelectedItems();

                await _pager.FirstPage(true);
            }

            await _commandsDropDown.SelectItem(null, false);
        }

        private async Task OnShowSelectedItemsChanged(bool selected)
        {
            _showOnlySelectedItems = selected;

            await _pager.FirstPage(true);
        }

        private async Task OnSelectAllItemsChanged(bool selected)
        {
            if (selected)
            {
                _selectedItems = _items.Where(x => x.IsUpToDate).UnionBy(_selectedItems, x => x.ItemUri).ToList();
            }
            else
            {
                _selectedItems = _selectedItems.Where(x => !_items.Any(y => x.ItemUri == y.ItemUri)).ToList();
            }

            RecalculateSelectedItems();

            await SaveSelectedItems();
        }

        private async Task OnSelectItemChanged(bool selected, WorkboxItem item)
        {
            if (selected)
            {
                _selectedItems.Add(item);
            }
            else
            {
                _selectedItems = _selectedItems.Where(x => x.ItemUri != item.ItemUri).ToList();
            }

            RecalculateSelectedItems();

            await SaveSelectedItems();
        }

        private void RecalculateSelectedItems()
        {
            _selectedItemsCount = _selectedItems.Where(x => x.WorkflowStateId == StateId).Count();
        }

        private async Task OnPageChanged(PagerEventArgs args)
        {
            PageIndex = args.PageIndex;

            if (_showOnlySelectedItems)
            {
                _items = _selectedItems.Where(x => x.WorkflowStateId == StateId).Skip(PageIndex * PageSize).Take(PageSize).ToList();
                _totalRecordCount = _selectedItemsCount;

                if (_selectedItemsCount > 0 && !_items.Any())
                {
                    await _pager.GoToPage(PageIndex - 1);
                }
            }
            else
            {
                await LoadWorkboxData();
            }
        }

        private async Task OnPageSizeChanged(int pageSize)
        {
            PageSize = pageSize;

            await _pager.FirstPage(true);
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

            await _pager.GoToPage(PageIndex, true);

            args.Column.SortOrder = _sortOrder;
        }

        private void UpdateFilters()
        {
            _filterValues[nameof(WorkboxItem.Path)] = Path;
            _filterValues[nameof(WorkboxItem.Language)] = Language;
            _filterValues[nameof(WorkboxItem.Version)] = Version;
            _filterValues[nameof(WorkboxItem.Name)] = TemplateName;
            _filterValues[nameof(WorkboxItem.UpdatedBy)] = UpdatedBy;
            _filterValues[nameof(WorkboxItem.Updated)] = new DateTime?[] { UpdatedFrom, UpdatedTo };
        }

        private void UpdateUrl()
        {
            Dictionary<string, object> queryStrings = new(_filterValues)
               {
                   { nameof(PageSize), PageSize },
                   { nameof(PageIndex), PageIndex },
                   { nameof(StateId), StateId }
               };

            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters(queryStrings));
        }

        private async Task SaveSelectedItems()
        {
            await LocalStorgeService.SetItemAsync(SelectedItemsLocalStorageKey, _selectedItems);
        }

        private async Task LoadState()
        {
            IList<WorkboxItem> selectedItemsFromLocalStorage = await LocalStorgeService.GetItemAsync<IList<WorkboxItem>>(SelectedItemsLocalStorageKey);

            if (selectedItemsFromLocalStorage != null)
            {
                _selectedItems = selectedItemsFromLocalStorage;
                RecalculateSelectedItems();
            }
        }

        private async Task LoadWorkboxData()
        {
            _isLoading = true;

            UpdateFilters();
            UpdateUrl();

            GraphQLRequest request = WorkboxItemsSearchRequest.Create(StateId, PageSize, PageIndex, _filterValues, _sortBy, _sortOrder);
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
                TemplateName = x.TemplateName,
                WorkflowStateId = x.InnerItem.Workflow.WorkflowState.StateId,
                ItemUri = x.Uri,
                IsUpToDate = x.InnerItem.Workflow.WorkflowState.StateId == StateId
            });

            _templateNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_templatename")?.Facets.OrderBy(x => x.Name);
            _updatedByNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "parsedupdatedby")?.Facets.OrderBy(x => x.Name)
                  .Select(x => new KeyValuePair<string, string>(x.Name, x.Name.Replace("sitecore", "sitecore/")));
            _languages = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_language")?.Facets.OrderBy(x => x.Name);

            _isLoading = false;
        }
    }
}