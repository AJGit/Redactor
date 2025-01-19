using RedactorApi.Analyzer;
using Xunit;

namespace RedactorApi.Tests.Unit
{
    public class CryptoHelperTests
    {
        [Fact]
        public void EncryptString_DecryptString_ShouldReturnOriginalPlainText()
        {
            // Arrange
            // var plainText = "This is a test string.";
            var plainText = new string('A', 20);
            var secret = "YourStrongPasswordOrKey";

            // Act
            var encryptedText = CryptoHelper.EncryptString(plainText, secret);
            var decryptedText = CryptoHelper.DecryptString(encryptedText, secret);

            // Assert
            Assert.Equal(plainText, decryptedText);
        }
    }
}
