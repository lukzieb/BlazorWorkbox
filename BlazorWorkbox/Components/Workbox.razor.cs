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

namespace BlazorWorkbox.Components
{
    public partial class Workbox : ComponentBase
    {
        private const string SelectedItemsLocalStorageKey = "selectedItems";

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

        private IEnumerable<WorkboxItem> Items = new List<WorkboxItem>();

        private IList<WorkboxItem> SelectedItems = new List<WorkboxItem>();
        private int SelectedItemsCount;
        public bool ShowOnlySelectedItems;
        private string StateDisplayName;

        private int TotalRecordCount;
        private readonly IEnumerable<int> PageSizeOptions = new int[] { 1, 25, 50 };

        [SupplyParameterFromQuery]
        private int PageIndex { get; set; }

        [SupplyParameterFromQuery]
        public int PageSize { get; set; }

        [SupplyParameterFromQuery]
        private string Path { get; set; }

        private string Name;

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
        private Guid WorkflowValue { get; set; }

        [SupplyParameterFromQuery]
        private Guid WorkflowStateValue { get; set; }

        private Guid CommandValue;

        private IEnumerable<KeyValuePair<Guid, string>> CommandData;
        private IEnumerable<KeyValuePair<Guid, string>> StateData;
        private IEnumerable<KeyValuePair<Guid, string>> WorkflowsData;

        Dictionary<Guid, IEnumerable<KeyValuePair<Guid, string>>> CommandsForState = new();
        Dictionary<Guid, IEnumerable<KeyValuePair<Guid, string>>> StatesForWorkflow = new();

        private readonly Dictionary<string, object> FilterValues = new();

        private string SortBy = nameof(WorkboxItem.Updated);
        private SortOrder SortOrder = SortOrder.Descending;
        private SortOrder? DefaultSortOrder;

        private IEnumerable<FacetItem> TemplateNames = Enumerable.Empty<FacetItem>();
        private IEnumerable<KeyValuePair<string, string>> UpdatedByNames = Enumerable.Empty<KeyValuePair<string, string>>();
        private IEnumerable<FacetItem> Languages = Enumerable.Empty<FacetItem>();

        private RadzenPager Pager;
        RadzenDropDown<Guid> CommandsDropDown;

        private bool IsLoading = true;
        protected override async Task OnInitializedAsync()
        {
            PageSize = 10;

            await LoadWorkflows();
            await LoadWorkboxData();

            await LoadStateAsync();
        }

        private async Task LoadWorkflows()
        {
            IEnumerable<Task<GraphQLResponse<WorkflowStatesResponse>>> stateRequests = AppSettings.Value.Workflows.Select(async x => await GraphQLClient.SendQueryAsync<WorkflowStatesResponse>(WorkflowStatesRequests.Create(x)));

            GraphQLResponse<WorkflowStatesResponse>[] stateResponses = await Task.WhenAll(stateRequests);

            StatesForWorkflow = stateResponses
                .Select(x => new KeyValuePair<Guid, IEnumerable<KeyValuePair<Guid, string>>>(x.Data.Workflow.WorkflowId, x.Data.Workflow.States.Nodes.Select(s => new KeyValuePair<Guid, string>(s.StateId, s.DisplayName)))).ToDictionary();

            IEnumerable<(Guid WorkflowId, Guid StatedId)> stateAndWorkflows = stateResponses.SelectMany(x => x.Data.Workflow.States.Nodes.Select(z => (x.Data.Workflow.WorkflowId, z.StateId)));

            IEnumerable<Task<(Guid StatedId, GraphQLResponse<WorkflowCommandsResponse>)>> commandsRequests = stateAndWorkflows
                .Select(async x => (x.StatedId, await GraphQLClient.SendQueryAsync<WorkflowCommandsResponse>(WorkflowCommandsRequest.Create(x.WorkflowId, x.StatedId))));

            (Guid StatedId, GraphQLResponse<WorkflowCommandsResponse> Response)[] commandsRespones = await Task.WhenAll(commandsRequests);

            CommandsForState = commandsRespones
                .Select(x => new KeyValuePair<Guid, IEnumerable<KeyValuePair<Guid, string>>>(x.StatedId, x.Response.Data.Workflow.Commands.Nodes.Where(y => !y.DisplayName.StartsWith("_")).Select(z => new KeyValuePair<Guid, string>(z.CommandId, z.DisplayName)))).ToDictionary();

            WorkflowsData = stateResponses.Select(x => new KeyValuePair<Guid, string>(x.Data.Workflow.WorkflowId, x.Data.Workflow.DisplayName));
            WorkflowValue = WorkflowsData.Any(x => x.Key == WorkflowValue) ? WorkflowValue : WorkflowsData.FirstOrDefault().Key;
            StateData = StatesForWorkflow[WorkflowValue];
            WorkflowStateValue = StateData.Any(x => x.Key == WorkflowStateValue) ? WorkflowStateValue : StateData.FirstOrDefault().Key;

            CommandData = CommandsForState[WorkflowStateValue];

            StateDisplayName = StateData.FirstOrDefault(x => x.Key == WorkflowStateValue).Value ?? StateData.FirstOrDefault().Value;

        }

        private async Task OnWorkflowValueChanged(object value)
        {
            StateData = StatesForWorkflow[(Guid)value];
            WorkflowStateValue = StateData.FirstOrDefault().Key;

            await OnWorkflowStateChanged(WorkflowStateValue);
        }

        private async Task OnWorkflowStateChanged(object value)
        {
            CommandData = CommandsForState[(Guid)value];
            StateDisplayName = StateData.FirstOrDefault(x => x.Key == (Guid)value).Value;

            if (ShowOnlySelectedItems)
            {
                Items = SelectedItems.Where(x => x.WorkflowStateId == WorkflowStateValue).ToList();
            }
            else
            {
                await Pager.FirstPage(true);
            }

            RecalculateSelectedItems();
        }

        private async Task OnWorkflowCommandChanged(object value)
        {
            string commandDisplayName = CommandData.FirstOrDefault(x => x.Key == (Guid)value).Value;

            IEnumerable<WorkboxItem> itemsProcessed = await DialogService.OpenAsync<WorkflowProcessingDialog>(
               $"Execute Workflow Command: {commandDisplayName}",
               new Dictionary<string, object>
               {
                    { nameof(WorkflowProcessingDialog.Items), SelectedItems.Where(x => x.WorkflowStateId == WorkflowStateValue).OrderBy(x => x.Path).ToList() },
                    { nameof(WorkflowProcessingDialog.WorkflowStateId), WorkflowStateValue },
                    { nameof(WorkflowProcessingDialog.CommandId), (Guid)value }
               },
               new DialogOptions { Width = "100%", Draggable = true, Resizable = true, ShowClose = false });

            if (itemsProcessed.Any())
            {
                SelectedItems = SelectedItems.Where(x => !itemsProcessed.Any(y => y.ItemUri == x.ItemUri)).ToList();
                RecalculateSelectedItems();
                await SaveSelectedItemsAsync();

                if (ShowOnlySelectedItems)
                {
                    Items = SelectedItems.Where(x => x.WorkflowStateId == WorkflowStateValue).ToList();
                }
                else
                {
                    await Pager.FirstPage(true);
                }
            }

            await CommandsDropDown.SelectItem(null, false);
        }

        private async Task OnShowSelectedItemsChanged(bool selected)
        {
            ShowOnlySelectedItems = selected;

            if (ShowOnlySelectedItems)
            {
                Items = SelectedItems.Where(x => x.WorkflowStateId == WorkflowStateValue).ToList();
                TotalRecordCount = SelectedItemsCount;
            }
            else
            {
                await Pager.FirstPage(true);
            }
        }

        private async Task OnSelectAllItemsChanged(bool selected)
        {
            if (selected)
            {
                SelectedItems = Items.UnionBy(SelectedItems, x => x.ItemUri).ToList();
            }
            else
            {
                SelectedItems = SelectedItems.Where(x => !Items.Any(y => x.ItemUri == y.ItemUri)).ToList();
            }

            RecalculateSelectedItems();

            await SaveSelectedItemsAsync();
        }

        private async Task OnSelectItemChanged(bool selected, WorkboxItem item)
        {
            if (selected)
            {
                SelectedItems.Add(item);
            }
            else
            {
                SelectedItems = SelectedItems.Where(x => x.ItemUri != item.ItemUri).ToList();
            }

            RecalculateSelectedItems();

            await SaveSelectedItemsAsync();
        }

        private void RecalculateSelectedItems()
        {
            SelectedItemsCount = SelectedItems.Where(x => x.WorkflowStateId == WorkflowStateValue).Count();
        }

        private async Task OnPageChanged(PagerEventArgs args)
        {
            PageIndex = args.PageIndex;

            await LoadWorkboxData();
        }

        private async Task OnPageSizeChanged(int pageSize)
        {
            PageSize = pageSize;

            await LoadWorkboxData();
        }

        private async Task OnFilterChanged(object value)
        {
            await Pager.FirstPage(true);
        }

        private async Task OnFilterChanged(DateTime? value)
        {
            await Pager.FirstPage(true);
        }

        private async Task OnSort(DataGridColumnSortEventArgs<WorkboxItem> args)
        {
            SortBy = args.Column.Property;
            SortOrder = args.SortOrder ?? SortOrder.Ascending;

            DefaultSortOrder = args.Column.Property == nameof(WorkboxItem.Updated) ? SortOrder : null;

            await Pager.GoToPage(PageIndex, true);

            args.Column.SortOrder = SortOrder;
        }

        private void UpdateFilters()
        {
            FilterValues[nameof(TemplateName)] = TemplateName;
            FilterValues[nameof(Path)] = Path;
            FilterValues[nameof(Name)] = Name;
            FilterValues[nameof(Version)] = Version;
            FilterValues[nameof(Language)] = Language;
            FilterValues[nameof(UpdatedBy)] = UpdatedBy;
            FilterValues[nameof(UpdatedFrom)] = UpdatedFrom;
            FilterValues[nameof(UpdatedTo)] = UpdatedTo;
        }

        private void UpdateUrl()
        {
            Dictionary<string, object> queryStrings = new(FilterValues)
               {
                   { nameof(PageSize), PageSize },
                   { nameof(PageIndex), PageIndex },
                   { nameof(WorkflowValue), WorkflowValue },
                   { nameof(WorkflowStateValue), WorkflowStateValue}
               };

            NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameters(queryStrings));
        }

        private async Task SaveSelectedItemsAsync()
        {
            await LocalStorgeService.SetItemAsync(SelectedItemsLocalStorageKey, SelectedItems);
        }

        private async Task LoadStateAsync()
        {
            IList<WorkboxItem> selectedItemsFromLocalStorage = await LocalStorgeService.GetItemAsync<IList<WorkboxItem>>(SelectedItemsLocalStorageKey);

            if (selectedItemsFromLocalStorage != null)
            {
                SelectedItems = selectedItemsFromLocalStorage;
                RecalculateSelectedItems();
            }
        }

        private async Task LoadWorkboxData()
        {
            IsLoading = true;

            UpdateFilters();
            UpdateUrl();

            GraphQLRequest request = WorkboxItemsSearchRequest.Create(WorkflowStateValue, PageSize, PageIndex, FilterValues, SortBy, SortOrder);
            GraphQLResponse<WorkboxItemsSearchResponse> result = await GraphQLClient.SendQueryAsync<WorkboxItemsSearchResponse>(request);

            if (result.Data == null)
            {
                return;
            }

            TotalRecordCount = result.Data.Search.TotalCount;
            Items = result.Data.Search.Results.Select(x => new WorkboxItem
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
                IsUpToDate = x.InnerItem.Workflow.WorkflowState.StateId == WorkflowStateValue
            });

            TemplateNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_templatename")?.Facets.OrderBy(x => x.Name);
            UpdatedByNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "parsedupdatedby")?.Facets.OrderBy(x => x.Name)
                  .Select(x => new KeyValuePair<string, string>(x.Name, x.Name.Replace("sitecore", "sitecore/")));
            Languages = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_language")?.Facets.OrderBy(x => x.Name);

            IsLoading = false;
        }
    }
}
