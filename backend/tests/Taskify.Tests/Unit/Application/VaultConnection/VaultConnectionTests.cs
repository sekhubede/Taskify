using Xunit;
using Moq;
using Taskify.Application.VaultConnection.Services;
using Taskify.Domain.Interfaces;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Application.VaultConnection;

public class VaultConnectionTests
{
    [Fact]
    public void InitializeConnection_Should_Connect_To_Vault()
    {
        // Arrange
        var mockManager = new Mock<IVaultConnectionManager>();
        var service = new VaultConnectionService(mockManager.Object);

        // Act
        service.InitializeConnection("1234567890");

        // Assert
        mockManager.Verify(m => m.Connect("1234567890"), Moq.Times.Once());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void InitializeConnection_Should_Call_Connect_With_Any_Guid_Value(string vaultGuid)
    {
        // Arrange
        var mockManager = new Mock<IVaultConnectionManager>();
        var service = new VaultConnectionService(mockManager.Object);

        // Act
        service.InitializeConnection(vaultGuid);

        // Assert
        mockManager.Verify(m => m.Connect(vaultGuid), Moq.Times.Once());
    }

    [Fact]
    public void InitializeConnection_Should_Propagate_Exceptions_From_ConnectionManager()
    {
        // Arrange
        var mockManager = new Mock<IVaultConnectionManager>();
        var expectedException = new InvalidOperationException("Connection failed");
        mockManager.Setup(m => m.Connect(It.IsAny<string>())).Throws(expectedException);
        var service = new VaultConnectionService(mockManager.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.InitializeConnection("1234567890"));
        Assert.Equal("Connection failed", exception.Message);
    }

    [Fact]
    public void InitializeConnection_Should_Call_Connect_Exactly_Once_Per_Call()
    {
        // Arrange
        var mockManager = new Mock<IVaultConnectionManager>();
        var service = new VaultConnectionService(mockManager.Object);

        // Act
        service.InitializeConnection("guid1");
        service.InitializeConnection("guid2");
        service.InitializeConnection("guid3");

        // Assert
        mockManager.Verify(m => m.Connect("guid1"), Moq.Times.Once());
        mockManager.Verify(m => m.Connect("guid2"), Moq.Times.Once());
        mockManager.Verify(m => m.Connect("guid3"), Moq.Times.Once());
        mockManager.Verify(m => m.Connect(It.IsAny<string>()), Moq.Times.Exactly(3));
    }
}