using System.Net.Http;
using System.Threading.Tasks;
using LEG.SwissTopo.Abstractions;

namespace LEG.SwissTopo.Client.SwissTopo
{
    public static class MaddApiClient
    {
        private static readonly HttpClient httpClient = new();

        public static async Task<RecordMaddBuildingProperties?> FetchMaddBuildingPropertiesAsync(string egid)
        {
            if (string.IsNullOrEmpty(egid))
                return null;

            var url = $"https://madd.bfs.admin.ch/eCH-0206?egid={egid}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseString = await response.Content.ReadAsStringAsync();
            return MapperMaddBuildingProperties.Parse(responseString);
        }
    }
}