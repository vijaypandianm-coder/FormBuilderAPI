using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using FluentAssertions;
using FormBuilderAPI.Data;
using Moq;
using Xunit;

namespace FormBuilderAPI.UnitTests.Data
{
    public class DapperExtensionsTests
    {
        [Fact]
        public async Task WithConn_Generic_ShouldOpenConnectionAndExecuteWork()
        {
            // Arrange
            var mockFactory = new Mock<IDbConnectionFactory>();
            var mockConnection = new Mock<DbConnection>();
            var mockDbConnection = mockConnection.As<IDbConnection>();
            
            mockFactory.Setup(f => f.Create()).Returns(mockDbConnection.Object);
            
            bool workExecuted = false;
            
            // Act
            var result = await mockFactory.Object.WithConn(async conn => 
            {
                workExecuted = true;
                await Task.Delay(1); // Add an await to make the method truly async
                return 42;
            });
            
            // Assert
            result.Should().Be(42);
            workExecuted.Should().BeTrue();
            mockFactory.Verify(f => f.Create(), Times.Once);
            mockConnection.Verify(c => c.OpenAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task WithConn_Generic_ShouldDisposeConnectionEvenWhenWorkThrows()
        {
            // Arrange
            var mockFactory = new Mock<IDbConnectionFactory>();
            var mockConnection = new Mock<DbConnection>();
            var mockDbConnection = mockConnection.As<IDbConnection>();
            
            mockFactory.Setup(f => f.Create()).Returns(mockDbConnection.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await mockFactory.Object.WithConn<int>(async _ => 
                {
                    await Task.Delay(1); // Add an await to make the method truly async
                    throw new InvalidOperationException("Test exception");
                });
            });
            
            mockFactory.Verify(f => f.Create(), Times.Once);
            mockConnection.Verify(c => c.OpenAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task WithConn_NonGeneric_ShouldOpenConnectionAndExecuteWork()
        {
            // Arrange
            var mockFactory = new Mock<IDbConnectionFactory>();
            var mockConnection = new Mock<DbConnection>();
            var mockDbConnection = mockConnection.As<IDbConnection>();
            
            mockFactory.Setup(f => f.Create()).Returns(mockDbConnection.Object);
            
            bool workExecuted = false;
            
            // Act
            await mockFactory.Object.WithConn(async conn => 
            {
                workExecuted = true;
                await Task.Delay(1); // Add an await to make the method truly async
            });
            
            // Assert
            workExecuted.Should().BeTrue();
            mockFactory.Verify(f => f.Create(), Times.Once);
            mockConnection.Verify(c => c.OpenAsync(default), Times.Once);
        }
        
        [Fact]
        public async Task WithConn_NonGeneric_ShouldDisposeConnectionEvenWhenWorkThrows()
        {
            // Arrange
            var mockFactory = new Mock<IDbConnectionFactory>();
            var mockConnection = new Mock<DbConnection>();
            var mockDbConnection = mockConnection.As<IDbConnection>();
            
            mockFactory.Setup(f => f.Create()).Returns(mockDbConnection.Object);
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await mockFactory.Object.WithConn(async _ => 
                {
                    await Task.Delay(1); // Add an await to make the method truly async
                    throw new InvalidOperationException("Test exception");
                });
            });
            
            mockFactory.Verify(f => f.Create(), Times.Once);
            mockConnection.Verify(c => c.OpenAsync(default), Times.Once);
        }
    }
}
