using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;
using ITBees.RestClient.Interfaces.RestModelMarkup;
using Moq;
using NUnit.Framework;

namespace ITBees.RestClient.UnitTests.RestClientTests
{
    // Covers the timeout (RequestTimeout) + retry (MaxRetryAttempts) wiring added so that
    // callers in latency-sensitive flows (e.g. parking gate opening) can bound how long a
    // single attempt waits and how many transient failures to absorb before bubbling up.
    public class RestClientTimeoutAndRetryTests
    {
        // Local fixtures — Put avoids the DerivedVmClassResolver path that Get/GetMany go
        // through, so tests don't need TinyMapper.Bind ritual.
        public class TestVm : Vm
        {
            public string Value { get; set; }
        }

        public class TestUm : Um
        {
            public string Value { get; set; }
        }

        private static Mock<IWebapiEndpointSetup> CreateEndpointSetup()
        {
            var setup = new Mock<IWebapiEndpointSetup>();
            setup.SetupGet(x => x.WebApiUrl).Returns("https://example.test");
            return setup;
        }

        private static HttpResponseMessage Ok(string json) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };

        [Test]
        public async Task Defaults_NoRetryNoTimeout_LegacyOverloadIsUsed()
        {
            // Backward-compat check: callers that don't opt in to timeout/retry must keep
            // the prior behavior (single attempt, non-CT IHttpClient overload). Existing
            // user mocks set up the non-CT signature only — this proves they still work.
            var http = new Mock<IHttpClient>();
            var legacyCallCount = 0;
            var ctCallCount = 0;
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(() =>
                {
                    legacyCallCount++;
                    return Ok(JsonSerializer.Serialize(new TestVm { Value = "hello" }));
                });
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    ctCallCount++;
                    return Ok(JsonSerializer.Serialize(new TestVm { Value = "hello" }));
                });

            var client = new RestClient<TestVm>(CreateEndpointSetup().Object, new NoTokenNeeded(), http.Object);

            var result = await client.Put("/whatever", new TestUm { Value = "x" });

            Assert.AreEqual(1, legacyCallCount);
            Assert.AreEqual(0, ctCallCount);
            Assert.AreEqual("hello", result.Value);
        }

        [Test]
        public void Timeout_NoRetry_ThrowsTimeoutAfterSingleAttempt()
        {
            // Operation hangs forever, RequestTimeout fires the CT, RestClient surfaces it as
            // TimeoutException (so callers can distinguish from other HTTP failures).
            var http = new Mock<IHttpClient>();
            var callCount = 0;
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
                .Returns<string, HttpContent, CancellationToken>(async (_, __, ct) =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(Timeout.Infinite, ct);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            var client = new RestClient<TestVm>(CreateEndpointSetup().Object, new NoTokenNeeded(), http.Object)
            {
                RequestTimeout = TimeSpan.FromMilliseconds(100),
                MaxRetryAttempts = 0,
            };

            Assert.ThrowsAsync<TimeoutException>(async () => await client.Put("/slow", new TestUm { Value = "x" }));
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public async Task Timeout_WithRetry_RetriesAndSucceedsOnSecondAttempt()
        {
            // First attempt hangs past the per-attempt timeout, second attempt returns OK.
            // This is the "parking column" target scenario: short timeout, single retry, no
            // hard failure when the second attempt lands.
            var http = new Mock<IHttpClient>();
            var attempt = 0;
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
                .Returns<string, HttpContent, CancellationToken>(async (_, __, ct) =>
                {
                    var current = Interlocked.Increment(ref attempt);
                    if (current == 1)
                    {
                        await Task.Delay(Timeout.Infinite, ct);
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                    return Ok(JsonSerializer.Serialize(new TestVm { Value = "second-attempt-ok" }));
                });

            var client = new RestClient<TestVm>(CreateEndpointSetup().Object, new NoTokenNeeded(), http.Object)
            {
                RequestTimeout = TimeSpan.FromMilliseconds(100),
                MaxRetryAttempts = 1,
            };

            var result = await client.Put("/flaky", new TestUm { Value = "x" });

            Assert.AreEqual(2, attempt);
            Assert.AreEqual("second-attempt-ok", result.Value);
        }

        [Test]
        public void Timeout_WithRetry_ThrowsAfterAllAttemptsExhausted()
        {
            // All attempts hang past the per-attempt timeout — RestClient must give up after
            // 1 + MaxRetryAttempts total attempts and rethrow as TimeoutException.
            var http = new Mock<IHttpClient>();
            var callCount = 0;
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
                .Returns<string, HttpContent, CancellationToken>(async (_, __, ct) =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(Timeout.Infinite, ct);
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            var client = new RestClient<TestVm>(CreateEndpointSetup().Object, new NoTokenNeeded(), http.Object)
            {
                RequestTimeout = TimeSpan.FromMilliseconds(50),
                MaxRetryAttempts = 2,
            };

            Assert.ThrowsAsync<TimeoutException>(async () => await client.Put("/dead", new TestUm { Value = "x" }));
            Assert.AreEqual(3, callCount); // 1 initial + 2 retries
        }

        [Test]
        public async Task HttpRequestException_IsTreatedAsTransient_AndRetried()
        {
            // Connection-level failures (DNS, refused, TLS) are HttpRequestException — must be
            // retried just like timeouts, since the "firewall blocking traffic" scenario can
            // surface either way.
            var http = new Mock<IHttpClient>();
            var attempt = 0;
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
                .Returns<string, HttpContent, CancellationToken>((_, __, ___) =>
                {
                    var current = Interlocked.Increment(ref attempt);
                    if (current == 1)
                        throw new HttpRequestException("Connection refused");
                    return Task.FromResult(Ok(JsonSerializer.Serialize(new TestVm { Value = "recovered" })));
                });

            var client = new RestClient<TestVm>(CreateEndpointSetup().Object, new NoTokenNeeded(), http.Object)
            {
                MaxRetryAttempts = 1,
            };

            var result = await client.Put("/blocked-once", new TestUm { Value = "x" });

            Assert.AreEqual(2, attempt);
            Assert.AreEqual("recovered", result.Value);
        }

        [Test]
        public void HttpStatusFailure_IsNotRetried()
        {
            // 5xx isn't a transport failure — server processed the request, we got an answer.
            // Retrying would surprise existing callers that rely on 5xx propagating up.
            var http = new Mock<IHttpClient>();
            var callCount = 0;
            http.Setup(x => x.PutAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        ReasonPhrase = "Server exploded",
                    };
                });

            var client = new RestClient<TestVm>(CreateEndpointSetup().Object, new NoTokenNeeded(), http.Object)
            {
                MaxRetryAttempts = 3,
            };

            var ex = Assert.ThrowsAsync<Exception>(async () => await client.Put("/server-error", new TestUm { Value = "x" }));
            Assert.AreEqual("Server exploded", ex!.Message);
            Assert.AreEqual(1, callCount);
        }
    }
}
