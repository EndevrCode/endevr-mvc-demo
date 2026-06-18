using System.Security.Cryptography;

public static class EncryptionHelper
{
    private const int NonceSize = 12;
    private const int TagSize   = 16;

    public static byte[] EncryptData(byte[] data, byte[] key)
    {
        var nonce      = new byte[NonceSize];
        var ciphertext = new byte[data.Length];
        var tag        = new byte[TagSize];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, data, ciphertext, tag);

        // Layout: [nonce(12)] [tag(16)] [ciphertext]
        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(result.AsSpan(0));
        tag.CopyTo(result.AsSpan(NonceSize));
        ciphertext.CopyTo(result.AsSpan(NonceSize + TagSize));
        return result;
    }

    public static byte[] DecryptData(byte[] encryptedData, byte[] key)
    {
        var nonce      = encryptedData[..NonceSize];
        var tag        = encryptedData[NonceSize..(NonceSize + TagSize)];
        var ciphertext = encryptedData[(NonceSize + TagSize)..];
        var plaintext  = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static byte[] GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public static byte[] GenerateRandomKey(int size)
    {
        var key = new byte[size];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
