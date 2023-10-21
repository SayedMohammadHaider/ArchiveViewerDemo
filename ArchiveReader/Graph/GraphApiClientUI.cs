using Microsoft.Graph;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Archive_Reader.Graph
{
    public class GraphApiClient
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly string graphApiBaseUrl = "https://graph.microsoft.com/v1.0/";
        public GraphApiClient(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task<User> GetGraphApiUser()
        {
            return await _graphServiceClient.Me.Request()
             .GetAsync().ConfigureAwait(false);
        }

        public async Task<string[]> GetLoggedInUserGroupList()
        {
            var data = await _graphServiceClient.Me.TransitiveMemberOf.Request().GetAsync();
            return data.Select(x => x.Id).ToArray();
        }

        public async Task<List<GroupDetails>> GetGroup(string select, string filter)
        {
            try
            {
                string url = graphApiBaseUrl + "groups";
                if (!string.IsNullOrEmpty(select))
                {
                    url += "?$select=" + select;
                }
                if (!string.IsNullOrEmpty(filter))
                {
                    url += "&$filter=" + filter;

                }
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await _graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(request);
                var response = await _graphServiceClient.HttpProvider.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                var groups = JsonConvert.DeserializeObject<GraphApiResponse>(content);
                return groups.value.Select(x => new GroupDetails { id = x.id, displayName = x.displayName }).ToList();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<Members>> GetGroupMembers(string groupId, string select)
        {
            try
            {
                string url = graphApiBaseUrl + "groups/" + groupId + "/members";
                if (!string.IsNullOrEmpty(select))
                {
                    url += "?$select=" + select;
                }
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await _graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(request);
                var response = await _graphServiceClient.HttpProvider.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                var members = JsonConvert.DeserializeObject<GraphApiResponse>(content);
                return members.value.Select(x => new Members { id = x.id, mail = x.mail, displayName = x.displayName, userPrincipalName = x.userPrincipalName }).ToList();
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }

    public class GraphApiResponse
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        public List<Value> value { get; set; }
    }

    public class Value
    {
        [JsonProperty("@odata.type")]
        public string odatatype { get; set; }
        public string id { get; set; }
        public string mail { get; set; }
        public string userPrincipalName { get; set; }
        public string displayName { get; set; }
    }

    public class Members
    {
        public string id { get; set; }
        public string mail { get; set; }
        public string userPrincipalName { get; set; }
        public string displayName { get; set; }
    }

    public class GroupDetails
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }
}
