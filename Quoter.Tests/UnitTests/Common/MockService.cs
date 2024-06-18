

using System.Reflection;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;

namespace Quoter.Tests.UnitTests.Common;

public class MockService<TType>
    where TType : class
{
    private readonly Dictionary<Type, MockObject> _mockObjects;

    public IFixture Fixture { get; }

    public MockService()
    {
        _mockObjects = new Dictionary<Type, MockObject>();
        Fixture = new Fixture().Customize(new AutoMoqCustomization());
        InitializeMockObjects();
    }

    public virtual TType Create()
    {
        var type = typeof(TType);

        var constructor = GetConstructor();
        var constructorParameters = new List<MockObject>();
        foreach (var arg in constructor.GetParameters())
        {
            constructorParameters.Add(_mockObjects[arg.ParameterType]);
        }

        if (!constructorParameters.Any())
        {
            return (TType)Activator.CreateInstance(type);
        }

        return (TType)Activator.CreateInstance(type, constructorParameters.Select(x => x?.RealValue ?? x?.Mock.Object).ToArray());
    }

    public Mock<TMockedType> Get<TMockedType>()
        where TMockedType : class
    {
        var mockedType = typeof(TMockedType);
        _mockObjects.TryGetValue(mockedType, out var mockedObject);
        if (mockedObject is null)
        {
            if (!HasConstructorWithParameterType(mockedType))
            {
                throw new NullReferenceException($"Mock object for {mockedType.Name} not found.");
            }
            var mock = CreateMock(mockedType);
            mockedObject = new MockObject(mockedType, mock);
            _mockObjects.Add(mockedType, mockedObject);
            InitializeMockObjects();
        }
        if (mockedObject.RealValue != null)
        {
            throw new NotSupportedException($"Object for {mockedType.Name} is not mocked but a real implementation.");
        }

        return (Mock<TMockedType>)mockedObject.Mock;
    }

    public MockObject SetSpecificImplementation<TMockedType>(Mock<TMockedType> mock)
        where TMockedType : class
    {
        var mockedType = typeof(TMockedType);
        MockObject mockObject;

        if (!_mockObjects.ContainsKey(mockedType))
        {
            if (!HasConstructorWithParameterType(mockedType))
            {
                throw new ArgumentException($"No constructor argument of class '{nameof(TType)}' matches type '{mockedType.Name}'", nameof(mock));
            }
            mockObject = new MockObject(mockedType, mock);
            _mockObjects[mockedType] = mockObject;
            InitializeMockObjects();
        }
        else
        {
            mockObject = new MockObject(mockedType, mock);
            _mockObjects[mockedType] = mockObject;
        }
        return mockObject;
    }

    public MockObject SetSpecificImplementation<TMockedType>(object realValue)
        where TMockedType : class
    {
        var mockedType = typeof(TMockedType);
        MockObject mockObject;

        if (!_mockObjects.ContainsKey(mockedType))
        {
            if (!HasConstructorWithParameterType(mockedType))
            {
                throw new ArgumentException($"No constructor argument of class '{nameof(TType)}' matches type '{mockedType.Name}'", nameof(realValue));
            }
            mockObject = new MockObject(mockedType, realValue);
            _mockObjects[mockedType] = mockObject;
            InitializeMockObjects();
        }
        else
        {
            mockObject = new MockObject(mockedType, realValue);
            _mockObjects[mockedType] = mockObject;
        }
        return mockObject;
    }

    public void VerifyAll()
    {
        foreach (var mockObject in _mockObjects.Values.Where(x => x.Mock != null))
        {
            mockObject.Mock.VerifyAll();
        }
    }


    private static bool HasConstructorWithParameterType(Type type)
    {
        return typeof(TType)
            .GetConstructors()
            .Any(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == type));
    }

    private ConstructorInfo GetConstructor()
    {
        var type = typeof(TType);

        var constructors = type.GetConstructors();
        if (constructors.Length > 1)
        {
            return constructors
                .MaxBy(constructor => constructor.GetParameters().Count(parameter => IsRegisteredMockedType(parameter.ParameterType)));
        }
        if (!constructors.Any())
        {
            throw new NotSupportedException($"Class '{type.Name}' without constructor is not supported.");
        }

        return constructors.FirstOrDefault();
    }

    private bool IsRegisteredMockedType(Type parameterType)
    {
        if (_mockObjects.ContainsKey(parameterType))
        {
            return true;
        }
        var underlyingType = Nullable.GetUnderlyingType(parameterType);
        if (underlyingType != null && _mockObjects.ContainsKey(underlyingType))
        {
            return true;
        }
        return false;
    }

    private void InitializeMockObjects()
    {
        var constructor = GetConstructor();

        foreach (var parameter in constructor.GetParameters())
        {
            var type = parameter.ParameterType;
            if (IsRegisteredMockedType(type))
            {
                continue;
            }
            var mock = CreateMock(type);
            var mockObject = new MockObject(type, mock);
            _mockObjects.Add(type, mockObject);
        }
    }

    private static Mock CreateMock(Type mockedType)
    {
        return (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(mockedType));
    }
}