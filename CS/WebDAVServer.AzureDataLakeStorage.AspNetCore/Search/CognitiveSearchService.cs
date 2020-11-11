using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Config;
using DavSearchOptions = ITHit.WebDAV.Server.Search.SearchOptions;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.Search
{
    public class CognitiveSearchService : ICognitiveSearchService
    {
        private readonly SearchClient searchClient;
        private readonly string contextPath;

        /// <summary>
        /// Initializes new instance of CognitiveSearchService.
        /// </summary>
        /// <param name="searchConfig">Search configuration.</param>
        /// <param name="davConfig">Context configuration.</param>
        public CognitiveSearchService(IOptions<SearchConfig> searchConfig, IOptions<DavContextConfig> davConfig)
        {
            SearchConfig config = searchConfig.Value;
            DavContextConfig dcConfig = davConfig.Value;
            string searchServiceUri = "https://" + config.ServiceName + ".search.windows.net";
            searchClient = new SearchClient(new Uri(searchServiceUri), config.IndexName,
                new AzureKeyCredential(config.ApiKey));
            contextPath = "https://" + dcConfig.AzureStorageAccountName + ".blob.core.windows.net" + "/" + dcConfig.DataLakeContainerName;
        }
        /// <summary>
        /// Check item for existence.
        /// </summary>
        /// <param name="query">Query to use in search.</param>
        /// <param name="searchOptions">Search options.</param>
        /// <param name="includeSnippet">Include snippet in the search result</param>
        /// <returns>List of matched items.</returns>
        public async Task<IList<SearchResult>> SearchAsync(string query, DavSearchOptions searchOptions,
            bool includeSnippet)
        {
            if (query.EndsWith("%"))
            {
                query = query.Remove(query.Length - 1, 1) + "*";
            }
            SearchOptions options = new SearchOptions();
            if (includeSnippet)
            {
                options = new SearchOptions
                {
                    HighlightFields = {"content"}, HighlightPreTag = "<b>", HighlightPostTag = "</b>"
                };
                options.Select.Add("metadata_storage_name");
                options.Select.Add("metadata_storage_path");
            }
            if (searchOptions.SearchName)
            {
                options.SearchFields.Add("metadata_storage_name");
            }
            if (searchOptions.SearchContent)
            {
                options.SearchFields.Add("content");
            }
            SearchResults<SearchResult> response = searchClient.Search<SearchResult>(query, options);
            List<SearchResult> list = new List<SearchResult>();
            foreach (var result in response.GetResults())
            {
                string encodedPath = result.Document.Path[0..^1];
                byte[] bytes = WebEncoders.Base64UrlDecode(encodedPath);
                string path = HttpUtility.UrlDecode(bytes, Encoding.UTF8);
                result.Document.Path = path.Replace(contextPath, "");
                if (includeSnippet)
                {
                    if (result.Highlights?["content"] != null)
                    {
                        result.Document.Snippet = result.Highlights["content"][0];
                    }
                }
                list.Add(result.Document);
            }
            return list;
        }
    }
}