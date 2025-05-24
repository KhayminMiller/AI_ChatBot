using System.Security.Cryptography;
using System.Text;

namespace ChatBot.Server.Services
{
    public class FileEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public FileEncryptionService(IConfiguration config)
        {
            // Load key and IV from configuration
            _key = Encoding.UTF8.GetBytes(config["Encryption:Key"]!);
            _iv = Encoding.UTF8.GetBytes(config["Encryption:IV"]!);
        }

        public async Task<byte[]> EncryptAsync(Stream inputStream)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

            await inputStream.CopyToAsync(cryptoStream);
            await cryptoStream.FlushAsync();
            cryptoStream.Close();

            return memoryStream.ToArray();
        }
    }
}
