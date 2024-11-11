using BlazorWorkbox.GraphQL.Requests;
using BlazorWorkbox.GraphQL.Responses;
using BlazorWorkbox.Models;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace BlazorWorkbox.Components
{
    public partial class Workbox : ComponentBase
    {
        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }

        private Guid WorkflowStateValue = Guid.Parse("190b1c84f1be47edaa41f42193d9c8fc");

        private IEnumerable<WorkboxItem> Items = new List<WorkboxItem>();

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
            this.PageSize = 10;

            await LoadWorkboxData();
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
                TemplateName = x.TemplateName
            });

            TemplateNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_templatename")?.Facets.OrderBy(x => x.Name);
            UpdatedByNames = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "parsedupdatedby")?.Facets.OrderBy(x => x.Name)
                  .Select(x => new KeyValuePair<string, string>(x.Name, x.Name.Replace("sitecore", "sitecore/")));
            Languages = result.Data.Search.Facets.FirstOrDefault(x => x.Name == "_language")?.Facets.OrderBy(x => x.Name);

            IsLoading = false;
        }
    }
}
