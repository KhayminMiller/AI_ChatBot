using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly BlobStorage _blobService;

    public FileController(BlobStorage blobService)
    {
        _blobService = blobService;
    }

    [HttpPost("encrypt")]
    public async Task<IActionResult> EncryptAndUpload([FromForm] IFormFile file)
    {
        if (file == null) return BadRequest("No file provided.");

        using var inputStream = file.OpenReadStream();
        using var outputStream = new MemoryStream();
        using var aes = Aes.Create();

        var key = aes.Key;
        var iv = aes.IV;

        using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await inputStream.CopyToAsync(cryptoStream);
        await cryptoStream.FlushAsync();

        outputStream.Position = 0;
        await _blobService.UploadFileAsync(outputStream, file.FileName + ".enc");

        return Ok(new { File = file.FileName + ".enc", Key = Convert.ToBase64String(key), IV = Convert.ToBase64String(iv) });
    }

    [HttpPost("decrypt")]
    public async Task<IActionResult> DownloadAndDecrypt([FromBody] DecryptionRequest request)
    {
        var stream = await _blobService.DownloadFileAsync(request.FileName);

        using var outputStream = new MemoryStream();
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(request.Key);
        aes.IV = Convert.FromBase64String(request.IV);

        using var cryptoStream = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(outputStream);
        outputStream.Position = 0;

        return File(outputStream.ToArray(), "application/octet-stream", request.FileName.Replace(".enc", ".decrypted"));
    }

    public class DecryptionRequest
    {
        public string FileName { get; set; }
        public string Key { get; set; }
        public string IV { get; set; }
    }
}
