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

namespace HugoCrossPoster.Tests
{
    public class MediumServiceTests
    {
        private bool _isRetryCalled;
        private int _retryCount;
        private readonly Mock<IHttpClientFactory> mockFactory = new Mock<IHttpClientFactory>();
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
            MediumService mediumService = new MediumService(mockFactory.Object);
            MediumPoco mediumPoco = new MediumPoco()
            {
                content = "#Test My Test",
                canonicalUrl = "https://www.cloudwithchris.com",
                tags = new List<string>() { "DevOps", "GitHub" },
                title = "Descriptive Title"
            };

            // Act
            await GetRetryPolicyAsync().ExecuteAsync(async () =>
            {
                return await mediumService.CreatePostAsync(mediumPoco, "integrationToken", "myAuthorId");
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
            MediumService mediumService = new MediumService(mockFactory.Object);
            MediumPoco mediumPoco = new MediumPoco()
            {
                content = "#Test My Test",
                canonicalUrl = "https://www.cloudwithchris.com",
                tags = new List<string>() { "DevOps", "GitHub" },
                title = "Descriptive Title"
            };

            // Act
            await GetRetryPolicyAsync().ExecuteAsync(async () =>
            {
                return await mediumService.CreatePostAsync(mediumPoco, "integrationToken", "myAuthorId");
            });

            // Assert
            Assert.True(_retryCount > 5);
            Assert.True(_isRetryCalled);
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
