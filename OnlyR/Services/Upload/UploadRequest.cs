using System;

namespace OnlyR.Services.Upload;

public sealed class UploadRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime? MeetingDate { get; set; }
    public string? FolderId { get; set; }
}

public sealed class UploadResponse
{
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => StatusCode == 202;
    public bool IsTokenInvalid => StatusCode == 401;
    public bool IsRetryable => StatusCode >= 500 || StatusCode == 429;
}
