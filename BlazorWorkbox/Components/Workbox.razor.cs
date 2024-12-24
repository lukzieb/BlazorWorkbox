using BlazorWorkbox.GraphQL.Requests;
using BlazorWorkbox.GraphQL.Responses;
using BlazorWorkbox.Models;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorWorkbox.Components
{
    public partial class Workbox : ComponentBase
    {
        private readonly Guid _stateID = Guid.Parse("190b1c84f1be47edaa41f42193d9c8fc");
        private readonly IEnumerable<int> _pageSizeOptions = [10, 25, 50];

        private IEnumerable<WorkboxItem> _items = [];

        private int _totalRecordCount;

        private int _pageIndex;
        private int _pageSize;

        [Inject]
        private IGraphQLClient GraphQLClient { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _pageSize = 10;

            await LoadWorkboxData();
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

        private async Task LoadWorkboxData()
        {
            GraphQLRequest request = WorkboxItemsSearchRequest.Create(_stateID, _pageSize, _pageIndex);
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
        }
    }
}
