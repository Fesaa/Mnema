using System.Net.Http.Json;
using System.Text.Json;

namespace Mnema.Common.Extensions;

public static class HttpClientExtensions
{

    extension(HttpClient httpClient)
    {

        public async Task<Result<TResult, HttpRequestException>> GetAsync<TResult>(string url, JsonSerializerOptions jsonSerializerOptions, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    return Result<TResult, HttpRequestException>.Err(new HttpRequestException(
                        $"Request failed with status {response.StatusCode}: {errorContent}",
                        null,
                        response.StatusCode));
                }

                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var json = await JsonSerializer.DeserializeAsync<TResult>(stream, jsonSerializerOptions, cancellationToken);
                
                if (json == null)
                {
                    return Result<TResult, HttpRequestException>.Err(
                        new HttpRequestException("Failed to deserialize response"));
                }
                
                return Result<TResult, HttpRequestException>.Ok(json);
            }
            catch (HttpRequestException ex)
            {
                return Result<TResult, HttpRequestException>.Err(ex);
            }
            catch (JsonException ex)
            {
                return Result<TResult, HttpRequestException>.Err(
                    new HttpRequestException("JSON deserialization failed", ex));
            }
        }
        
        public async Task<Result<TResult, HttpRequestException>> PostAsync<TResult>(
            string url, object body, JsonSerializerOptions  jsonSerializerOptions, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync(url, body, jsonSerializerOptions, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    return Result<TResult, HttpRequestException>.Err(new HttpRequestException(
                        $"Request failed with status {response.StatusCode}: {errorContent}",
                        null,
                        response.StatusCode));
                }

                var json = await response.Content.ReadFromJsonAsync<TResult>(cancellationToken);
                if (json == null)
                {
                    return Result<TResult, HttpRequestException>.Err(
                        new HttpRequestException("Failed to deserialize response"));
                }
                
                return Result<TResult, HttpRequestException>.Ok(json);
            }
            catch (HttpRequestException ex)
            {
                return Result<TResult, HttpRequestException>.Err(ex);
            }
            catch (JsonException ex)
            {
                return Result<TResult, HttpRequestException>.Err(
                    new HttpRequestException("JSON serialization/deserialization failed", ex));
            }
        }
        
    }
    
}