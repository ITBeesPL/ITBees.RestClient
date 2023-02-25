using NUnit.Framework;

namespace ITBees.RestClient.UnitTests.VmAbstracClassTests
{
    public class VmAbstractClassTests
    {
        [Test]
        public void VmAbstractClass_GetApiEndpointUrl_shouldReturnCorrectEndpoint()
        {
            var derivedVmClass = new MyAccountVm();

            Assert.True(derivedVmClass.GetApiEndpointUrl() == "MyAccount");
        }
        
        [Test]
        public void VmAbstractClass_GetApiEndpointUrl_shouldReturnFullClassNameIfNameDoesntHaveSufixVm()
        {
            var derivedVmClass = new MyAccount123();

            Assert.True(derivedVmClass.GetApiEndpointUrl() == "MyAccount123");
        }
    }
}