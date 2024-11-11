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

        private async Task LoadWorkboxData()
        {
            GraphQLRequest request = WorkboxItemsSearchRequest.Create(WorkflowStateValue, PageSize, PageIndex);
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
        }
    }
}
