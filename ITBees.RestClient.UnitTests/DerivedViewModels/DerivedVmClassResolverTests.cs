using ITBees.RestClient.Interfaces.RestModelMarkup;
using NUnit.Framework;
using System.Text.Json;
using InheritedMapper;
using Nelibur.ObjectMapper;

namespace ITBees.RestClient.UnitTests.DerivedViewModels
{
    public class DerivedVmClassResolverTests
    {
        [Test]
        public void DerivedVmClassResolver_ShouldReturnInheritedObjectInstance_BasedOndPropertyName()
        {
            TinyMapper.Bind<BaseVm, DerivedFromBase>();
            var derivedVmClassResolver = new DerivedVmClassResolver<BaseVm>();
            var classToSerialize = new BaseVm() { BaseType = "DerivedFromBase", Value = "Test123" };
            var serializedDerivedFromBaseClass = JsonSerializer.Serialize(classToSerialize); ;

            object result = derivedVmClassResolver.Get(serializedDerivedFromBaseClass);

            Assert.IsInstanceOf<DerivedFromBase>(result);
            Assert.True(((DerivedFromBase)result).Value == "Test123");
        }
    }


    public class BaseVm : Vm
    {
        public string BaseType { get; set; }
        public string Value { get; set; }
    }

    public class DerivedFromBase : BaseVm
    {
    }
}