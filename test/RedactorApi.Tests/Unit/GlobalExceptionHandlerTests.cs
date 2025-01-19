using Moq;

namespace RedactorApi.Tests.Unit
{
    public class GlobalExceptionHandlerTests
    {
        [Fact]
        public async Task TryHandleAsync_LogsErrorAndReturnsTrue()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
            var exceptionHandler = new GlobalExceptionHandler(loggerMock.Object);
            var httpContext = new DefaultHttpContext();
            var exception = new Exception("Test exception");
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await exceptionHandler.TryHandleAsync(httpContext, exception, cancellationToken);

            // Assert
            Assert.True(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        }
    }
}
