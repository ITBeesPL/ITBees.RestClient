using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ITBees.RestClient.Interfaces;
using ITBees.RestClient.UnitTests.DerivedViewModels;
using Moq;
using Nelibur.ObjectMapper;
using NUnit.Framework;

namespace ITBees.RestClient.UnitTests.RestClientTests
{
    public class RestClientTest
    {
        [Test]
        public async Task RestClientShouldReturn_derivedClass_ifBaseModelHasPropertyNamedLikeClassWithSufixNamed_Type()
        {
            TinyMapper.Bind<BaseVm, DerivedFromBase>();
            TinyMapper.Bind<List<BaseVm>, List<DerivedFromBase>>();
            var httpClientMock = new Mock<IHttpClient>();
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpClientMock.SetupGet(x => x.DefaultRequestHeaders);
            httpClientMock.Setup(x => x.GetAsync(It.IsAny<string>())).ReturnsAsync(httpResponseMessage);
            var targetClassesStrings = new List<DerivedFromBase>()
            {
                new() {BaseType = "DerivedFromBase", Value = "1"},
                new() {BaseType = "DerivedFromBase", Value = "2"}
            };
            var serializedResponse = JsonSerializer.Serialize(targetClassesStrings);
            httpResponseMessage.Content = new StringContent(serializedResponse);
            
            var webapiEndpointSetup = new Mock<IWebapiEndpointSetup>();
            webapiEndpointSetup.SetupGet(x => x.Login).Returns("Test");
            webapiEndpointSetup.SetupGet(x => x.Pass).Returns("Test");
            webapiEndpointSetup.SetupGet(x => x.WebApiUrl).Returns("https://supertesttest.test");
            
            var tokenService = new Mock<ITokenService>();

            var restClient = new RestClient<BaseVm>(webapiEndpointSetup.Object, tokenService.Object, httpClientMock.Object);
            var result = await restClient.GetMany("/test", new BaseVm());

            Assert.IsInstanceOf<List<BaseVm>>(result);
            Assert.IsInstanceOf<DerivedFromBase>(result.First());
        }
    }
}