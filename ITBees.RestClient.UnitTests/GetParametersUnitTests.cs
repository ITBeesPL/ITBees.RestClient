using System;
using ITBees.RestClient.Interfaces.RestModelMarkup;
using NUnit.Framework;

namespace ITBees.RestClient.UnitTests
{
    public class GetParametersUnitTests
    {
        [Test]
        public void ClassDerviedFrom_GetParametersBaseClass_shouldReturnAllPropertiesFormatedToGetQuery()
        {
            var myAccountViewModel = new MyAccountVm();
            var expectedGetQuery = "CompanyName=TestCompanyName&CreatedDate=01.01.0001 01:01:01&Guid=12300000-0000-0000-0000-000000000123";

            var receivedGetQuery = myAccountViewModel.CreateGetQueryFromClassProperties();

            Assert.True(expectedGetQuery == receivedGetQuery);
        }
    }

    public class MyAccountVm : Vm
    {
        public string CompanyName { get; set; } = "TestCompanyName";
        public DateTime CreatedDate { get; set; } = new DateTime(1, 1, 1, 1, 1, 1);
        public Guid Guid { get; set; } = new Guid("12300000-0000-0000-0000-000000000123");
    }
}