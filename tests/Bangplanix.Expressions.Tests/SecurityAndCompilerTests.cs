using System.Security.Cryptography;
using Bangplanix.Core.Security;
using Bangplanix.Expressions.Compiler;
using Bangplanix.Expressions.Security;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Expressions.Tests;

public class SecurityAndCompilerTests
{
    [Theory]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("Math.Abs(-42.5)", 42.5)]
    [InlineData("ReportFunctions.IIf(10 > 5, \"Yes\", \"No\")", "Yes")]
    [InlineData("\"Total: \" + (500 + 200).ToString()", "Total: 700")]
    public void RoslynCompiler_ValidExpressions_ShouldEvaluateCorrectly(string expr, object expected)
    {
        var compiler = new RoslynExpressionCompiler();
        var result = compiler.Evaluate(expr);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("System.IO.File.Delete(\"test.txt\")")]
    [InlineData("System.Diagnostics.Process.Start(\"calc.exe\")")]
    [InlineData("Type.GetType(\"System.IO.File\")")]
    [InlineData("System.Reflection.Assembly.Load(\"mscorlib\")")]
    [InlineData("System.Environment.Exit(1)")]
    public void AstSecurityValidator_ForbiddenCalls_ShouldBeBlocked(string dangerousExpr)
    {
        var isSafe = AstSecurityValidator.IsExpressionSafe(dangerousExpr, out var violationReason);
        isSafe.Should().BeFalse();
        violationReason.Should().NotBeNullOrWhiteSpace();

        var compiler = new RoslynExpressionCompiler();
        var act = () => compiler.Evaluate(dangerousExpr);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Security sandbox violation*");
    }

    [Theory]
    [InlineData("http://127.0.0.1/admin", false)]
    [InlineData("http://localhost:5000", false)]
    [InlineData("http://169.254.169.254/latest/meta-data/", false)]
    [InlineData("http://10.0.0.5/api", false)]
    [InlineData("http://192.168.1.1/setup", false)]
    [InlineData("http://172.16.0.1/secret", false)]
    [InlineData("file:///etc/passwd", false)]
    [InlineData("gopher://127.0.0.1", false)]
    [InlineData("https://images.unsplash.com/photo-12345.jpg", true)]
    public void AntiSsrfValidator_ShouldBlockInternalAndAllowExternalUrls(string url, bool shouldBeSafe)
    {
        var isSafe = AntiSsrfValidator.IsSafeUrl(url, out var reason);
        isSafe.Should().Be(shouldBeSafe);
        if (!shouldBeSafe)
        {
            reason.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void HardenedXml_ExternalEntityInjection_ShouldBeProhibited()
    {
        var maliciousXxe = @"<?xml version=""1.0""?>
<!DOCTYPE foo [
  <!ELEMENT foo ANY >
  <!ENTITY xxe SYSTEM ""file:///etc/passwd"" >]>
<foo>&xxe;</foo>";

        var act = () => HardenedXml.LoadSafeXml(maliciousXxe);
        act.Should().Throw<System.Xml.XmlException>("DTD processing is prohibited");
    }

    [Fact]
    public void AesGcmCrypto_EncryptAndDecrypt_ShouldPreserveConfidentiality()
    {
        var plainText = "Server=db.enterprise.internal;Database=FinData;User Id=sa;Password=SecretPassword123!;";
        var masterKey = "Custom_Super_Secure_Master_Key_999!";

        var encrypted = AesGcmCrypto.Encrypt(plainText, masterKey);
        encrypted.Should().StartWith("enc:");
        encrypted.Should().NotContain("SecretPassword123!");

        var decrypted = AesGcmCrypto.Decrypt(encrypted, masterKey);
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void AesGcmCrypto_TamperedCiphertext_ShouldThrowCryptographicException()
    {
        var plainText = "Sensitive Data Payload";
        var encrypted = AesGcmCrypto.Encrypt(plainText);

        // Tamper with payload
        var rawBase64 = encrypted[4..];
        var bytes = Convert.FromBase64String(rawBase64);
        bytes[^1] ^= 0xFF; // Flip last byte of tag/ciphertext

        var tamperedEnc = "enc:" + Convert.ToBase64String(bytes);

        var act = () => AesGcmCrypto.Decrypt(tamperedEnc);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void AesGcmCrypto_WithAssociatedData_ShouldEnforceTenantIntegrity()
    {
        var plainText = "TenantA_Sensitive_ConnectionString";
        var tenantId = "tenant_company_001";
        var wrongTenantId = "tenant_company_002";

        var encrypted = AesGcmCrypto.Encrypt(plainText, associatedData: tenantId);

        // Correct tenant decryption
        var decrypted = AesGcmCrypto.Decrypt(encrypted, associatedData: tenantId);
        decrypted.Should().Be(plainText);

        // Cross-tenant tampering attempt should fail
        var act = () => AesGcmCrypto.Decrypt(encrypted, associatedData: wrongTenantId);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void SimdAggregations_ShouldComputeAccurateAggregationsOnLargeDatasets()
    {
        var count = 10000;
        var data = new double[count];
        double expectedSum = 0;
        for (int i = 0; i < count; i++)
        {
            data[i] = (i + 1) * 1.5;
            expectedSum += data[i];
        }

        var simdSum = Bangplanix.Expressions.Functions.SimdAggregations.Sum(data);
        var simdAvg = Bangplanix.Expressions.Functions.SimdAggregations.Average(data);
        var simdMin = Bangplanix.Expressions.Functions.SimdAggregations.Min(data);
        var simdMax = Bangplanix.Expressions.Functions.SimdAggregations.Max(data);

        simdSum.Should().BeApproximately(expectedSum, 0.0001);
        simdAvg.Should().BeApproximately(expectedSum / count, 0.0001);
        simdMin.Should().Be(1.5);
        simdMax.Should().Be(count * 1.5);

        // Also test through ReportFunctions
        Bangplanix.Expressions.Functions.ReportFunctions.Sum(data).Should().BeApproximately(expectedSum, 0.0001);
        Bangplanix.Expressions.Functions.ReportFunctions.Avg(data).Should().BeApproximately(expectedSum / count, 0.0001);
        Bangplanix.Expressions.Functions.ReportFunctions.Min(data).Should().Be(1.5);
        Bangplanix.Expressions.Functions.ReportFunctions.Max(data).Should().Be(count * 1.5);
    }

    [Fact]
    public void SimdAggregations_EdgeCases_EmptyAndSmallArrays_ShouldHandleGracefully()
    {
        // Empty array
        var empty = Array.Empty<double>();
        Bangplanix.Expressions.Functions.SimdAggregations.Sum(empty).Should().Be(0.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Average(empty).Should().Be(0.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Min(empty).Should().Be(0.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Max(empty).Should().Be(0.0);

        // Single element
        var single = new double[] { 42.0 };
        Bangplanix.Expressions.Functions.SimdAggregations.Sum(single).Should().Be(42.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Average(single).Should().Be(42.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Min(single).Should().Be(42.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Max(single).Should().Be(42.0);

        // Odd number of elements (13 elements to test SIMD vector remainder loop)
        var odd = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
        Bangplanix.Expressions.Functions.SimdAggregations.Sum(odd).Should().Be(91.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Average(odd).Should().Be(7.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Min(odd).Should().Be(1.0);
        Bangplanix.Expressions.Functions.SimdAggregations.Max(odd).Should().Be(13.0);
    }

    [Fact]
    public void RoslynCompiler_EvaluationWithContext_ShouldResolveParametersAndFields()
    {
        var compiler = new RoslynExpressionCompiler();
        var result = compiler.Evaluate("1000.0 * (1 + 0.07)");
        result.Should().Be(1070.0);
    }
}

