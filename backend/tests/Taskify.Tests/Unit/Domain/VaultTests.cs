using Xunit;
using Taskify.Domain.Entities;

namespace Taskify.Tests.Unit.Domain;

public class VaultTests
{
    [Fact]
    public void Constructor_With_Valid_Parameters_Should_Create_Vault()
    {
        // Arrange
        const string name = "Test Vault";
        const string guid = "1234567890";

        // Act
        var vault = new Vault(name, guid);

        // Assert
        Assert.Equal(name, vault.Name);
        Assert.Equal(guid, vault.Guid);
        Assert.False(vault.IsAuthenticated);
    }

    [Fact]
    public void Constructor_With_Null_Name_Should_Throw_ArgumentException()
    {
        // Arrange
        const string guid = "1234567890";
        string? name = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Vault(name!, guid));
        Assert.Equal("Vault name cannot be empty (Parameter 'name')", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_With_Empty_Or_Whitespace_Name_Should_Throw_ArgumentException(string name)
    {
        // Arrange
        const string guid = "1234567890";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Vault(name, guid));
        Assert.Equal("Vault name cannot be empty (Parameter 'name')", exception.Message);
    }

    [Fact]
    public void Constructor_With_Null_Guid_Should_Throw_ArgumentNullException()
    {
        // Arrange
        const string name = "Test Vault";
        string? guid = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Vault(name, guid!));
        Assert.Equal("guid (Parameter 'Vault GUID cannot be empty')", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_With_Empty_Or_Whitespace_Guid_Should_Throw_ArgumentNullException(string guid)
    {
        // Arrange
        const string name = "Test Vault";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Vault(name, guid));
        Assert.Equal("guid (Parameter 'Vault GUID cannot be empty')", exception.Message);
    }

    [Fact]
    public void Constructor_Should_Set_IsAuthenticated_To_False()
    {
        // Arrange & Act
        var vault = new Vault("Test Vault", "1234567890");

        // Assert
        Assert.False(vault.IsAuthenticated);
    }

    [Fact]
    public void MarkAsAuthenticated_Should_Set_IsAuthenticated_To_True()
    {
        // Arrange
        var vault = new Vault("Test Vault", "1234567890");
        Assert.False(vault.IsAuthenticated);

        // Act
        vault.MarkAsAuthenticated();

        // Assert
        Assert.True(vault.IsAuthenticated);
    }

    [Fact]
    public void MarkAsAuthenticated_Can_Be_Called_Multiple_Times_Without_Error()
    {
        // Arrange
        var vault = new Vault("Test Vault", "1234567890");

        // Act
        vault.MarkAsAuthenticated();
        vault.MarkAsAuthenticated();
        vault.MarkAsAuthenticated();

        // Assert
        Assert.True(vault.IsAuthenticated);
    }

    [Fact]
    public void Properties_Should_Be_Immutable_After_Construction()
    {
        // Arrange
        var vault = new Vault("Original Name", "OriginalGuid");

        // Act
        vault.Name = "New Name";
        vault.Guid = "NewGuid";

        // Assert - Properties are settable, but this tests they can be changed
        Assert.Equal("New Name", vault.Name);
        Assert.Equal("NewGuid", vault.Guid);
    }
}
