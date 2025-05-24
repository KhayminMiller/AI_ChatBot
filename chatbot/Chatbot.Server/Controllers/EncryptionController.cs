using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Chatbot.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
/*
 * The way the encryption and decryption works is a 256 bit AES key and
 * an 128 bit AES IV (initialzation vector) is randomly generated
 * 
 * The same key is used for both encryption and decryption.
 * 
 * The IV allows repeated encryption of the same file producing
 *  different ciphertexts each time
 * 
 * Encryption:
 *  A random AES key and IV are generated for the file, 
 *      the file is uploaded, the file and filestream are both 
 *      encrypted using the key and IV, uploads the file to azure blob storage,
 *          *azure blob is a local cloud storage
 *
 * Decryption:
 *  downloads the encryptes file from the storage, reads the first
 *      16 bytes as the IV, uses the key and IV to decrypte the rest of the 
 *      file, and downloads it to the local machine. 
 * 
 * 
 */
[ApiController]
[Route("api/files")]
public class EncryptionController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly byte[] _aesKey;
    private readonly byte[] _aesIV;
    private readonly string _containerName = "encryptedfiles";

    // AES Key & IV generated once on startup – in production, store securely
    private static readonly byte[] AesKey;
    private static readonly byte[] AesIV;

    static EncryptionController()
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        aes.GenerateIV();

        AesKey = aes.Key;
        AesIV = aes.IV;

        // Log Base64 key/iv to console (DO NOT DO THIS IN PROD)
        Console.WriteLine($"🔐 AES Key (Base64): {Convert.ToBase64String(AesKey)}");
        Console.WriteLine($"🔐 AES IV  (Base64): {Convert.ToBase64String(AesIV)}");
    }

    public EncryptionController(BlobServiceClient blobServiceClient, IOptions<EncryptionSettings> encryptionSettings)
    {
        _blobServiceClient = blobServiceClient;

        _aesKey = Encoding.UTF8.GetBytes(encryptionSettings.Value.Key);
        _aesIV = Encoding.UTF8.GetBytes(encryptionSettings.Value.IV);

        if (_aesKey.Length != 32 || _aesIV.Length != 16)
            throw new InvalidOperationException("AES Key must be 32 bytes and IV must be 16 bytes.");
    }

    [HttpPost("encrypt")]
    public async Task<IActionResult> EncryptAndUpload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();

        using var msInput = file.OpenReadStream();
        using var msEncrypted = new MemoryStream();

        using (var aes = Aes.Create())
        {
            aes.Key = AesKey;
            aes.IV = AesIV;
            using (var cryptoStream = new CryptoStream(msEncrypted, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
            {
                await msInput.CopyToAsync(cryptoStream);
            }
        }

        msEncrypted.Position = 0; // Reset position to start for upload
        var blob = container.GetBlobClient(file.FileName);
        await blob.UploadAsync(msEncrypted, overwrite: true);

        return Ok("✅ File encrypted and uploaded.");
    }

    [HttpGet("decrypt/{fileName}")]
    public async Task<IActionResult> DownloadAndDecrypt(string fileName)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blob = container.GetBlobClient(fileName);

        if (!await blob.ExistsAsync())
            return NotFound("File not found.");

        var blobDownloadInfo = await blob.DownloadAsync();

        using var msDecrypted = new MemoryStream();
        using (var aes = Aes.Create())
        {
            aes.Key = AesKey;
            aes.IV = AesIV;
            using (var cryptoStream = new CryptoStream(blobDownloadInfo.Value.Content, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                await cryptoStream.CopyToAsync(msDecrypted);
            }
        }
        msDecrypted.Position = 0;
        return File(msDecrypted.ToArray(), "application/octet-stream", fileName);
    }

    [HttpGet("list")]
    public async Task<IActionResult> ListEncryptedFiles()
    {
        var container = _blobServiceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync();

        var fileNames = new List<string>();
        await foreach (BlobItem blob in container.GetBlobsAsync())
        {
            fileNames.Add(blob.Name);
        }

        return Ok(fileNames);
    }
}
