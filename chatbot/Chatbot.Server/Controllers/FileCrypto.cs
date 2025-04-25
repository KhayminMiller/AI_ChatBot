using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace ChatBot.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileCrypto : ControllerBase
    {
        [HttpPost("process")]
        public async Task<IActionResult> ProcessFile([FromForm] IFormFile file, [FromForm] string password, [FromForm] string mode)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var inputStream = file.OpenReadStream();
            using var outputStream = new MemoryStream();

            try
            {
                if (mode == "encrypt")
                    await AesEncryptAsync(inputStream, outputStream, password);
                else if (mode == "decrypt")
                    await AesDecryptAsync(inputStream, outputStream, password);
                else
                    return BadRequest("Invalid mode.");

                outputStream.Seek(0, SeekOrigin.Begin);
                var resultFileName = $"{mode}ed_{file.FileName}";
                return File(outputStream.ToArray(), "application/octet-stream", resultFileName);
            }
            catch
            {
                return BadRequest("Failed to process file. Ensure password is correct.");
            }
        }

        private static async Task AesEncryptAsync(Stream input, Stream output, string password)
        {
            using var aes = Aes.Create();
            var key = GetKey(password, aes.KeySize / 8);
            aes.Key = key;
            aes.GenerateIV();
            await output.WriteAsync(aes.IV);

            using var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await input.CopyToAsync(cryptoStream);
            await cryptoStream.FlushAsync();
        }

        private static async Task AesDecryptAsync(Stream input, Stream output, string password)
        {
            using var aes = Aes.Create();
            var key = GetKey(password, aes.KeySize / 8);
            aes.Key = key;

            byte[] iv = new byte[aes.BlockSize / 8];
            await input.ReadAsync(iv, 0, iv.Length);
            aes.IV = iv;

            using var cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            await cryptoStream.CopyToAsync(output);
        }

        private static byte[] GetKey(string password, int keySize)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return hash.Take(keySize).ToArray();
        }
    }
}
