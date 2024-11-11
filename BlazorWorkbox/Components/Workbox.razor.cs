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
        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }

        [Inject]
        private IOptions<AppSettings> AppSettings { get; set; }

        private IEnumerable<WorkboxItem> Items = new List<WorkboxItem>();

        private IList<WorkboxItem> SelectedItems = new List<WorkboxItem>();
        private int SelectedItemsCount;
        public bool ShowOnlySelectedItems;
        private string StateDisplayName;

        private int TotalRecordCount;
        private readonly IEnumerable<int> PageSizeOptions = new int[] { 10, 25, 50 };

        private int PageIndex;
        private int PageSize;

        private string Path;
        private string Name;
        private string Language;
        private string Version;
        private string TemplateName;
        private string UpdatedBy;
        private DateTime? UpdatedFrom;
        private DateTime? UpdatedTo;

        private Guid WorkflowValue;
        private Guid WorkflowStateValue;
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
        private bool IsLoading = true;
        protected override async Task OnInitializedAsync()
        {
            PageSize = 10;

            await LoadWorkflows();
            await LoadWorkboxData();
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
            WorkflowValue = WorkflowsData.FirstOrDefault().Key;
            StateData = StatesForWorkflow[WorkflowValue];
            WorkflowStateValue = StateData.FirstOrDefault().Key;

            CommandData = CommandsForState[WorkflowStateValue];

            StateDisplayName = StateData.FirstOrDefault().Value;
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

        private async Task LoadWorkboxData()
        {
            IsLoading = true;
            UpdateFilters();

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
                ItemUri = x.Uri
            });

            TemplateNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_templatename")?.Facets.OrderBy(x => x.Name);
            UpdatedByNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "parsedupdatedby")?.Facets.OrderBy(x => x.Name)
                  .Select(x => new KeyValuePair<string, string>(x.Name, x.Name.Replace("sitecore", "sitecore/")));
            Languages = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_language")?.Facets.OrderBy(x => x.Name);

            IsLoading = false;
        }
    }
}
