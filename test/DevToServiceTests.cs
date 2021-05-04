using Xunit;
using HugoCrossPoster.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Moq;
using Moq.Protected;
using System.Threading;
using System.Net;
using Polly;
using Polly.Extensions.Http;
using System;
using Polly.CircuitBreaker;
using Microsoft.Extensions.Logging;

namespace HugoCrossPoster.Tests
{
    public class DevToServiceTests
    {
        private bool _isCircuitBroken;
        private bool _isCircuitRestored;
        private bool _isRetryCalled;
        private int _retryCount;
        private readonly Mock<IHttpClientFactory> mockFactory = new Mock<IHttpClientFactory>();
        private readonly Mock<ILogger<DevToService>> mockLogger = new Mock<ILogger<DevToService>>();
        private readonly Mock<HttpMessageHandler> mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        [Fact]
        public async Task AssertHttpRetryWorksCorrectly()
        {
            // Arrange - Setup the handler for the mocked HTTP call
            _isRetryCalled = false;
            _retryCount = 0;

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    if (_isRetryCalled)
                    {
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.TooManyRequests
                        };
                    }
                });


            var client = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // Arrange - Setup the Service/Poco details
            DevToService devtoService = new DevToService(mockFactory.Object, mockLogger.Object);
            DevToPoco devtoPoco = new DevToPoco()
            {
                article = new Article()
                {
                    body_markdown = "#Test My Test",
                    description = "This is a description",
                    canonical_url = "https://www.cloudwithchris.com",
                    published = false,
                    series = "Cloud Drops",
                    tags = new List<string>() { "DevOps", "GitHub" },
                    title = "Descriptive Title"
                }
            };

            // Act
            await GetRetryPolicyAsync().ExecuteAsync(async () =>
            {
                return await devtoService.CreatePostAsync(devtoPoco, "integrationToken");
            });

            // Assert
            Assert.True(_isRetryCalled);
            Assert.Equal(1, _retryCount);
        }

        [Fact]
        public async Task AssertHttpCircuitBreakerWorksCorrectly()
        {
            // Arrange - Setup the handler for the mocked HTTP call
            _isCircuitBroken = false;
            _isCircuitRestored = false;
            _isRetryCalled = false;
            _retryCount = 0;

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    if (_retryCount > 5)
                    {
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.TooManyRequests
                        };
                    }
                });


            var client = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // Arrange - Setup the Service/Poco details
            DevToService devtoService = new DevToService(mockFactory.Object, mockLogger.Object);
            DevToPoco devtoPoco = new DevToPoco()
            {
                article = new Article()
                {
                    body_markdown = "#Test My Test",
                    description = "This is a description",
                    canonical_url = "https://www.cloudwithchris.com",
                    published = false,
                    series = "Cloud Drops",
                    tags = new List<string>() { "DevOps", "GitHub" },
                    title = "Descriptive Title"
                }
            };

            // Act
            try
            {
                await GetRetryPolicyAsync().WrapAsync(GetCircuitBreakerPolicyAsync()).ExecuteAsync(async () =>
                {
                    return await devtoService.CreatePostAsync(devtoPoco, "integrationToken");
                });
            }
            catch (BrokenCircuitException ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Assert
            Assert.True(_isRetryCalled);
            Assert.True(_isCircuitBroken);
            Assert.True(_isCircuitRestored);
        }


        [Fact]
        public async Task VerifyYouTubeLiquidTagAddedAtEndOfBody()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234";
            string youtube = "abc123456";

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK
                    };
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            DevToService devtoService = new DevToService(mockFactory.Object, mockLogger.Object);
            string contentWithYouTube = await devtoService.AppendYouTubeInformation(originalContent, youtube);

            // Assert
            Assert.Contains($"{{% youtube {youtube} %}}", contentWithYouTube);
            mockLogger.Verify(l => l.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.IsAny<It.IsAnyType>(),
             It.IsAny<Exception>(),
             (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Exactly(1));
        }

        [Fact]
        public async Task VerifyNoChangeIfEmptyYouTubePropertySpecified()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234";
            string youtube = "";

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK
                    };
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            DevToService devtoService = new DevToService(mockFactory.Object, mockLogger.Object);
            string contentWithYouTube = await devtoService.AppendYouTubeInformation(originalContent, youtube);

            // Assert
            Assert.Equal(originalContent, contentWithYouTube);
            mockLogger.Verify(l => l.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.IsAny<It.IsAnyType>(),
             It.IsAny<Exception>(),
             (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Exactly(1));
        }

        [Fact]
        public async Task VerifyNoChangeIfNullYouTubePropertySpecified()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234";
            string youtube = null;

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.OK
                    };
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            DevToService devtoService = new DevToService(mockFactory.Object, mockLogger.Object);
            string contentWithYouTube = await devtoService.AppendYouTubeInformation(originalContent, youtube);

            // Assert
            Assert.Equal(originalContent, contentWithYouTube);
            mockLogger.Verify(l => l.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.IsAny<It.IsAnyType>(),
             It.IsAny<Exception>(),
             (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Exactly(1));
        }

        public IAsyncPolicy<HttpResponseMessage> GetRetryPolicyAsync()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    6,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetryAsync: OnRetryAsync
                );
        }

        public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicyAsync()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    3,
                    TimeSpan.FromSeconds(10),
                    onBreak: (ex, breakDelay) =>
                    {
                        _isCircuitBroken = true;
                        // TODO: Investigate more elegant way to handle this. Currently detecting if circuit is broken, and sleeping until the next call.
                        Thread.Sleep(10000);
                    },
                    onReset: () =>
                    {
                        _isCircuitRestored = true;
                    }
                );
        }


        private async Task OnRetryAsync(DelegateResult<HttpResponseMessage> outcome, TimeSpan timespan, int retryCount, Context context)
        {
            //Log result
            await Task.Run(() => _isRetryCalled = true);
            await Task.Run(() => _retryCount++);
        }
    }
}
