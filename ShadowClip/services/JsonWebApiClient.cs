using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ShadowClip.services
{
    public interface IJsonWebApiClient
    {
        Task<dynamic> Get(string url);
    }

    public class JsonWebApiClient : IJsonWebApiClient
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public async Task<dynamic> Get(string url)
        {
            var result = await _httpClient.GetAsync(url);
            var json = await result.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }
    }
}