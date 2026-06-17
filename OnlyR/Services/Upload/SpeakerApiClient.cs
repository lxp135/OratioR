using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace OnlyR.Services.Upload;

public sealed class SpeakerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiToken;

    public SpeakerApiClient(HttpClient httpClient, string baseUrl, string apiToken)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _apiToken = apiToken;
    }

    public async Task<UploadResponse> UploadAsync(UploadRequest request)
    {
        try
        {
            var content = new MultipartFormDataContent();
            var fileStream = File.OpenRead(request.FilePath);
            var fileContent = new StreamContent(fileStream);
            content.Add(fileContent, "file", Path.GetFileName(request.FilePath));

            if (request.Title != null)
            {
                content.Add(new StringContent(request.Title), "title");
            }

            if (request.MeetingDate != null)
            {
                content.Add(new StringContent(request.MeetingDate.Value.ToString("O")), "meeting_date");
            }

            if (request.FolderId != null)
            {
                content.Add(new StringContent(request.FolderId), "folder_id");
            }

            var url = $"{_baseUrl}/api/v1/recordings/upload";
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {_apiToken}");

            Log.Information("Uploading to {Url}, file={File}, size={Size}", url, request.FilePath, new FileInfo(request.FilePath).Length);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();
            Log.Information("Speaker response: {StatusCode} - {Body}", (int)response.StatusCode, responseBody);
            return new UploadResponse { StatusCode = (int)response.StatusCode };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Speaker upload exception");
            return new UploadResponse { StatusCode = 0, ErrorMessage = ex.Message };
        }
    }
}
