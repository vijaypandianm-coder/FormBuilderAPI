using System;
using System.Collections.Generic;
using FluentAssertions;
using FormBuilderAPI.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Xunit;

namespace FormBuilderAPI.UnitTests.Data
{
    public class MySqlConnectionFactoryTests
    {
        [Fact]
        public void Create_ShouldReturnMySqlConnection()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?> {
                {"ConnectionStrings:Sql", "Server=localhost;Database=testdb;User=user;Password=password;"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            
            var factory = new MySqlConnectionFactory(configuration);
            
            // Act
            var connection = factory.Create();
            
            // Assert
            connection.Should().BeOfType<MySqlConnection>();
        }
        
        [Fact]
        public void Constructor_WithMissingConnectionString_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string?>();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            
            // Act & Assert
            Action act = () => new MySqlConnectionFactory(configuration);
            
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Missing ConnectionStrings:Sql*");
        }
    }
}