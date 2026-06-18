namespace Nestled.Data.Vault
{
    public class VaultBiometricCredential
    {
        public Guid   Id               { get; set; }
        public string UserId           { get; set; } = "";
        public byte[] CredentialId     { get; set; } = [];
        public byte[] PublicKey        { get; set; } = [];
        public uint   SignatureCounter { get; set; }
        public string DeviceName       { get; set; } = "This device";
        public DateTime  RegisteredAt  { get; set; }
        public DateTime? LastUsedAt    { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
