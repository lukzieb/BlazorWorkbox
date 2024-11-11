# BlazorWorkbox

A Sitecore items workflow management Blazor WebAsseembly app.

Key features:
- filtering and sorting by item path, language, version, last modification date, author user name, template name
- selecting multiple items for bulk processing by specific workflow action
- utilizes Sitecore Authoring and Management GraphQL API queries to fetch items and mutations to process trough workflows
- runs as a standalone application secured by Sitecore Identity Server authentication

  ![image](https://github.com/user-attachments/assets/a84d96c3-8657-459f-b1a9-58f73118fd6a)
  ![image](https://github.com/user-attachments/assets/6e4642df-b05a-4611-8acc-74cfa494c067)
  ![image](https://github.com/user-attachments/assets/b26b4a91-748a-4797-a2d7-265f8ad5a745)


# Setup

1. Define BlazorClient in ID Server configuration - see 'ID Server Setup
' https://sitecoregroove.hashnode.dev/blazor-workbox-project-and-id-server-setup#heading-id-server-setup
2. Set "IdentityAuthorityBaseUrl" and "ContentManagementInstanceBaseUrl" in appsettings.json to point to ID Server and Content Management instancees
3. Setup CORS headers for CM instance - see 'CORS setup' https://sitecoregroove.hashnode.dev/blazor-workbox-radzen-datagrid-and-graphql-authoring-api?source=more_series_bottom_blogs#heading-cors-setup
4. Run the app in Visual Studio or Publish to target environment - see https://sitecoregroove.hashnode.dev/blazor-workbox-publishing-the-app

   
