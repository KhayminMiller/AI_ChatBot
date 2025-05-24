using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

[ApiController]
[Route("api/chatbot")]
public class ChatbotController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName = "encrypted-files";
    private readonly byte[] _aesKey = Encoding.UTF8.GetBytes("Your32ByteLongSecretKeyGoesHere!"); // 32 bytes for AES-256

    public ChatbotController(IHttpClientFactory httpClientFactory, BlobServiceClient blobServiceClient)
    {
        _httpClientFactory = httpClientFactory;
        _blobServiceClient = blobServiceClient;
    }

    [HttpGet("ping")]
    public IActionResult Ping() => Ok("pong");

    [HttpPost("chat/stream")]
    public async Task StreamChat([FromBody] ChatRequest request)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var ollamaRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
        {
            Content = JsonContent.Create(new
            {
                model = "mistral:7b-instruct",
                prompt = request.Prompt,
                stream = true
            })
        };

        var response = await httpClient.SendAsync(ollamaRequest, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
        response.EnsureSuccessStatusCode();

        HttpContext.Response.ContentType = "text/event-stream";
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(responseStream);
        var writer = HttpContext.Response.BodyWriter;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("{"))
            {
                try
                {
                    var json = JsonDocument.Parse(line);
                    if (json.RootElement.TryGetProperty("response", out var chunk))
                    {
                        var data = chunk.GetString();
                        if (!string.IsNullOrEmpty(data))
                        {
                            var sse = $"data: {data}\n\n";
                            var bytes = Encoding.UTF8.GetBytes(sse);
                            await writer.WriteAsync(bytes, HttpContext.RequestAborted);
                            await writer.FlushAsync(HttpContext.RequestAborted);
                        }
                    }
                }
                catch { }
            }
        }

        await writer.CompleteAsync();
    }

    [HttpPost("encrypt")]
    public async Task<IActionResult> EncryptAndUpload([FromForm] IFormFile file)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();

        using var inputStream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        using (var aes = Aes.Create())
        {
            aes.Key = _aesKey;
            aes.GenerateIV();
            memoryStream.Write(aes.IV, 0, aes.IV.Length); // Prepend IV

            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await inputStream.CopyToAsync(cryptoStream);
        }

        memoryStream.Position = 0;
        var blobClient = container.GetBlobClient(file.FileName + ".aes");
        await blobClient.UploadAsync(memoryStream, overwrite: true);

        return Ok("File encrypted and uploaded.");
    }

    [HttpGet("files")]
    public async Task<IActionResult> ListEncryptedFiles()
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        var results = new List<string>();

        await foreach (BlobItem item in container.GetBlobsAsync())
        {
            results.Add(item.Name);
        }

        return Ok(results);
    }

    [HttpGet("decrypt")]
    public async Task<IActionResult> DownloadAndDecrypt([FromQuery] string fileName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = container.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
            return NotFound("File not found.");

        await using var encryptedStream = new MemoryStream();
        await blobClient.DownloadToAsync(encryptedStream);
        encryptedStream.Position = 0;

        using var decryptedStream = new MemoryStream();

        using (var aes = Aes.Create())
        {
            var iv = new byte[16];
            await encryptedStream.ReadAsync(iv, 0, iv.Length);
            aes.Key = _aesKey;
            aes.IV = iv;

            using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await cryptoStream.CopyToAsync(decryptedStream);
        }

        decryptedStream.Position = 0;
        return File(decryptedStream.ToArray(), "application/octet-stream", fileName.Replace(".aes", ""));
    }
}
public class ChatRequest
{
    public string Prompt { get; set; }
}
