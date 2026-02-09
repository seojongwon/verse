using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace RetreatVerses.App.Data
{
    public sealed class KomoranService : IMorphologyService
    {
        private readonly HttpClient _client;
        private readonly MorphologyServiceOptions _options;

        public KomoranService(HttpClient client, IOptions<MorphologyServiceOptions> options)
        {
            _client = client;
            _options = options.Value ?? new MorphologyServiceOptions();

            if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                _client.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
            }
        }

        public async Task<MorphologyResult> CheckNounAsync(string word)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                return new MorphologyResult(false, "명사 판별 서비스가 설정되지 않았습니다.");
            }

            var response = await _client.PostAsJsonAsync("api/noun", new NounRequest { Text = word });
            if (!response.IsSuccessStatusCode)
            {
                return new MorphologyResult(false, "명사 판별에 실패했습니다.");
            }

            var result = await response.Content.ReadFromJsonAsync<NounResponse>();
            if (result == null)
            {
                return new MorphologyResult(false, "명사 판별 응답을 읽을 수 없습니다.");
            }

            return new MorphologyResult(result.IsNoun, result.Message);
        }

        private sealed class NounRequest
        {
            public string Text { get; set; } = string.Empty;
        }

        private sealed class NounResponse
        {
            public bool IsNoun { get; set; }
            public string? Message { get; set; }
        }
    }
}
