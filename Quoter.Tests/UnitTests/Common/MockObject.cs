using Moq;

namespace Quoter.Tests.UnitTests.Common;

public class MockObject
{
    public Type Type { get; }

    public Mock Mock { get; }

    public object RealValue { get; set; }

    public MockObject(Type type, Mock mock) => (Type, Mock) = (type, mock);
    public MockObject(Type type, object realValue) => (Type, RealValue) = (type, realValue);
}