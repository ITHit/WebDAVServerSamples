using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ITHit.WebDAV.Server.Search;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.Search
{
    /// <summary>
    /// Provides method for interacting with Azure Cognitive Search.
    /// </summary>
    public interface ICognitiveSearchService
    {
        /// <summary>
        /// Check item for existence.
        /// </summary>
        /// <param name="query">Query to use in search.</param>
        /// <param name="options">Search options.</param>
        /// <param name="includeSnippet">Include snippet in the search result</param>
        /// <returns>List of matched items.</returns>
        Task<IList<SearchResult>> SearchAsync(string query, SearchOptions options, bool includeSnippet);

    }

    /// <summary>
    /// Represents SearchResult.
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Document name.
        /// </summary>
        [JsonPropertyName("metadata_storage_name")]
        public string Name { get; set; }

        /// <summary>
        /// Encrypted Document path.
        /// </summary>
        [JsonPropertyName("metadata_storage_path")]
        public string Path { get; set; }
        /// <summary>
        /// Encrypted Document path.
        /// </summary>
        [JsonIgnore]
        public string Snippet { get; set; }
    }
}