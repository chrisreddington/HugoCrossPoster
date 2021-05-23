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
using Microsoft.Extensions.Logging;

namespace HugoCrossPoster.Tests
{
    public class MediumServiceTests
    {
        private bool _isRetryCalled;
        private int _retryCount;
        private readonly Mock<IHttpClientFactory> mockFactory = new Mock<IHttpClientFactory>();
        private readonly Mock<ILogger<MediumService>> mockLogger = new Mock<ILogger<MediumService>>();
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
            MediumService mediumService = new MediumService(mockFactory.Object, mockLogger.Object);
            MediumPoco mediumPoco = new MediumPoco()
            {
                content = "#Test My Test",
                canonicalUrl = "https://www.cloudwithchris.com",
                tags = new List<string>() { "DevOps", "GitHub" },
                title = "Descriptive Title"
            };
            Mock<CancellationTokenSource> mockCancellationTokenSource = new Mock<CancellationTokenSource>();

            // Act
            await GetRetryPolicyAsync().ExecuteAsync(async () =>
            {
                return await mediumService.CreatePostAsync(mediumPoco, "integrationToken", mockCancellationTokenSource.Object, "myAuthorId");
            });

            // Assert
            Assert.True(_isRetryCalled);
            Assert.Equal(1, _retryCount);
        }

        [Fact]
        public async Task AssertHttpCircuitBreakerWorksCorrectly()
        {
            // Arrange - Setup the handler for the mocked HTTP call
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
            MediumService mediumService = new MediumService(mockFactory.Object, mockLogger.Object);
            MediumPoco mediumPoco = new MediumPoco()
            {
                content = "#Test My Test",
                canonicalUrl = "https://www.cloudwithchris.com",
                tags = new List<string>() { "DevOps", "GitHub" },
                title = "Descriptive Title"
            };
            Mock<CancellationTokenSource> mockCancellationTokenSource = new Mock<CancellationTokenSource>();

            // Act
            await GetRetryPolicyAsync().ExecuteAsync(async () =>
            {
                return await mediumService.CreatePostAsync(mediumPoco, "integrationToken", mockCancellationTokenSource.Object, "myAuthorId");
            });

            // Assert
            Assert.True(_retryCount > 5);
            Assert.True(_isRetryCalled);
        }

        [Fact]
        public async Task VerifyYouTubeLiquidTagAddedAtEndOfBody()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234";
            string youtube = "abc123456";
            string expectedLogOutput = $"Youtube ID {youtube} added";

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
            MediumService mediumService = new MediumService(mockFactory.Object, mockLogger.Object);
            string contentWithYouTube = await mediumService.AppendYouTubeInformation(originalContent, youtube);

            // Assert
            Assert.Contains($"https://youtu.be/{youtube}", contentWithYouTube);
            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedLogOutput) == 0;

            mockLogger.Verify(l => l.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => state(v, t)),
             It.IsAny<Exception>(),
             (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Exactly(1));
        }

        [Fact]
        public async Task VerifyTweetContentConvertedSuccessfully()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234\n{{< tweet 1395779887412170752 >}}";
            string expectedContent = "#Hello\n* world\n* 1234\nhttps://twitter.com/username/status/1395779887412170752";

            // Act
            MediumService mediumService = new MediumService(mockFactory.Object, mockLogger.Object);
            string actualContent = await mediumService.ReplaceEmbeddedTweets(originalContent);

            // Assert
            Assert.Equal(expectedContent, actualContent);
        }

        [Fact]
        public async Task VerifyNoChangeIfEmptyYouTubePropertySpecified()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234";
            string youtube = "";
            string expectedLogOutput = "No YouTube ID provided, not embedding a YouTube Video.";

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
            MediumService mediumService = new MediumService(mockFactory.Object, mockLogger.Object);
            string contentWithYouTube = await mediumService.AppendYouTubeInformation(originalContent, youtube);

            // Assert
            Assert.Equal(originalContent, contentWithYouTube);

            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedLogOutput) == 0;

            mockLogger.Verify(l => l.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => state(v, t)),
             It.IsAny<Exception>(),
             (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Exactly(1));
        }

        [Fact]
        public async Task VerifyNoChangeIfNullYouTubePropertySpecified()
        {
            // Arrange
            string originalContent = "#Hello\n* world\n* 1234";
            string youtube = null;
            string expectedLogOutput = "No YouTube ID provided, not embedding a YouTube Video.";

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
            MediumService mediumService = new MediumService(mockFactory.Object, mockLogger.Object);
            string contentWithYouTube = await mediumService.AppendYouTubeInformation(originalContent, youtube);

            // Assert
            Assert.Equal(originalContent, contentWithYouTube);

            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedLogOutput) == 0;

            mockLogger.Verify(l => l.Log(
             LogLevel.Information,
             It.IsAny<EventId>(),
             It.Is<It.IsAnyType>((v, t) => state(v, t)),
             It.IsAny<Exception>(),
             (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.Exactly(1));
        }

        public IAsyncPolicy<HttpResponseMessage> GetRetryPolicyAsync()
        {
            return HttpPolicyExtensions.HandleTransientHttpError()
                .WaitAndRetryAsync(
                    6,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetryAsync: OnRetryAsync)
                .WrapAsync(Policy.Handle<AggregateException>(x =>
                {
                    var result = x.InnerException is HttpRequestException;
                    return result;
                })
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
        }


        private async Task OnRetryAsync(DelegateResult<HttpResponseMessage> outcome, TimeSpan timespan, int retryCount, Context context)
        {
            //Log result
            await Task.Run(() => _isRetryCalled = true);
            await Task.Run(() => _retryCount++);
        }

    }
}
