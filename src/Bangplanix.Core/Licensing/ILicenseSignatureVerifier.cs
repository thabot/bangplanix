namespace Bangplanix.Core.Licensing;

/// <summary>
/// Cryptographic signature verification contract supporting quantum-ready algorithm agility.
/// </summary>
public interface ILicenseSignatureVerifier
{
    /// <summary>
    /// Algorithm name identifier (e.g. "Ed25519", "ML-DSA-65", "Hybrid-Dilithium").
    /// </summary>
    string AlgorithmName { get; }

    /// <summary>
    /// Verifies that the raw payload bytes match the digital signature against the public verification key.
    /// </summary>
    bool VerifySignature(byte[] payloadBytes, byte[] signatureBytes, byte[] publicKeyBytes);
}
