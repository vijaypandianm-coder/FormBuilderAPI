using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Services;
using FormBuilderAPI.UnitTests.Fakes;
using Xunit;

namespace FormBuilderAPI.UnitTests.Services
{
            public class FormServicesTests
            {
                [Fact]
                public async Task GetByFormKeyAsync_ReturnsForm_WhenFound()
                {
                    // Arrange
                    var fakeMongoContext = new FakeMongoDbContext();
                    var form = new Form 
                    { 
                        FormKey = 123, 
                        Title = "Test Form", 
                        Status = "Published" 
                    };
            
                    // Setup the fake MongoDB context to return our test form
                    fakeMongoContext.Forms.InsertOne(form);
            
                    var formService = new FormService(fakeMongoContext);

                    // Act
                    var result = await formService.GetByFormKeyAsync(123);

                    // Assert
                    result.Should().NotBeNull();
                    result!.FormKey.Should().Be(123);
                    result.Title.Should().Be("Test Form");
                }

                [Fact]
                public void FormService_CanBeCreated()
                {
                    // Arrange & Act
                    var fakeMongoContext = new FakeMongoDbContext();
                    var formService = new FormService(fakeMongoContext);
            
                    // Assert
                    formService.Should().NotBeNull();
                }
            }
}