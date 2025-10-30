using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using Moq;
using Xunit;

namespace FormBuilderAPI.UnitTests.Data
{
    public class ResponsesRepositoryTests
    {
        [Fact]
        public async Task InsertFormResponseHeaderAsync_ShouldReturnId()
        {
            // Arrange
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.InsertFormResponseHeaderAsync(1, 100, "form1"))
                .ReturnsAsync(42);
            
            // Act
            var result = await mockRepo.Object.InsertFormResponseHeaderAsync(1, 100, "form1");
            
            // Assert
            result.Should().Be(42);
            mockRepo.Verify(r => r.InsertFormResponseHeaderAsync(1, 100, "form1"), Times.Once);
        }
        
        [Fact]
        public async Task InsertFormResponseAnswerAsync_ShouldReturnRowsAffected()
        {
            // Arrange
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.InsertFormResponseAnswerAsync(42, 1, 100, "field1", "text", "answer"))
                .ReturnsAsync(1);
            
            // Act
            var result = await mockRepo.Object.InsertFormResponseAnswerAsync(42, 1, 100, "field1", "text", "answer");
            
            // Assert
            result.Should().Be(1);
            mockRepo.Verify(r => r.InsertFormResponseAnswerAsync(42, 1, 100, "field1", "text", "answer"), Times.Once);
        }
        
        [Fact]
        public async Task ListHeadersByFormKeyAsync_ShouldReturnHeaders()
        {
            // Arrange
            var expectedHeaders = new List<ResponseHeaderDto>
            {
                new ResponseHeaderDto { Id = 1, FormKey = 100, UserId = 1, SubmittedAt = DateTime.UtcNow },
                new ResponseHeaderDto { Id = 2, FormKey = 100, UserId = 2, SubmittedAt = DateTime.UtcNow }
            };
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.ListHeadersByFormKeyAsync(100))
                .ReturnsAsync(expectedHeaders);
            
            // Act
            var result = await mockRepo.Object.ListHeadersByFormKeyAsync(100);
            
            // Assert
            result.Should().BeEquivalentTo(expectedHeaders);
            mockRepo.Verify(r => r.ListHeadersByFormKeyAsync(100), Times.Once);
        }
        
        [Fact]
        public async Task ListHeadersByFormKeyAndUserAsync_ShouldReturnHeaders()
        {
            // Arrange
            var expectedHeaders = new List<ResponseHeaderDto>
            {
                new ResponseHeaderDto { Id = 1, FormKey = 100, UserId = 1, SubmittedAt = DateTime.UtcNow }
            };
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.ListHeadersByFormKeyAndUserAsync(100, 1))
                .ReturnsAsync(expectedHeaders);
            
            // Act
            var result = await mockRepo.Object.ListHeadersByFormKeyAndUserAsync(100, 1);
            
            // Assert
            result.Should().BeEquivalentTo(expectedHeaders);
            mockRepo.Verify(r => r.ListHeadersByFormKeyAndUserAsync(100, 1), Times.Once);
        }
        
        [Fact]
        public async Task ListHeadersByUserAsync_ShouldReturnHeaders()
        {
            // Arrange
            var expectedHeaders = new List<ResponseHeaderDto>
            {
                new ResponseHeaderDto { Id = 1, FormKey = 100, UserId = 1, SubmittedAt = DateTime.UtcNow },
                new ResponseHeaderDto { Id = 2, FormKey = 101, UserId = 1, SubmittedAt = DateTime.UtcNow }
            };
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.ListHeadersByUserAsync(1))
                .ReturnsAsync(expectedHeaders);
            
            // Act
            var result = await mockRepo.Object.ListHeadersByUserAsync(1);
            
            // Assert
            result.Should().BeEquivalentTo(expectedHeaders);
            mockRepo.Verify(r => r.ListHeadersByUserAsync(1), Times.Once);
        }
        
        [Fact]
        public async Task GetHeaderByIdAsync_ShouldReturnHeader()
        {
            // Arrange
            var expectedHeader = new ResponseHeaderDto { Id = 42, FormKey = 100, UserId = 1, SubmittedAt = DateTime.UtcNow };
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetHeaderByIdAsync(42))
                .ReturnsAsync(expectedHeader);
            
            // Act
            var result = await mockRepo.Object.GetHeaderByIdAsync(42);
            
            // Assert
            result.Should().BeEquivalentTo(expectedHeader);
            mockRepo.Verify(r => r.GetHeaderByIdAsync(42), Times.Once);
        }
        
        [Fact]
        public async Task GetHeaderByIdAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetHeaderByIdAsync(42))
                .ReturnsAsync((ResponseHeaderDto?)null);
            
            // Act
            var result = await mockRepo.Object.GetHeaderByIdAsync(42);
            
            // Assert
            result.Should().BeNull();
            mockRepo.Verify(r => r.GetHeaderByIdAsync(42), Times.Once);
        }
        
        [Fact]
        public async Task ListAnswersByResponseIdAsync_ShouldReturnAnswers()
        {
            // Arrange
            var expectedAnswers = new List<ResponseAnswerRow>
            {
                new ResponseAnswerRow { Id = 1, ResponseId = 42, FieldId = "field1", FieldType = "text", AnswerValue = "answer1", SubmittedAt = DateTime.UtcNow },
                new ResponseAnswerRow { Id = 2, ResponseId = 42, FieldId = "field2", FieldType = "text", AnswerValue = "answer2", SubmittedAt = DateTime.UtcNow }
            };
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.ListAnswersByResponseIdAsync(42))
                .ReturnsAsync(expectedAnswers);
            
            // Act
            var result = await mockRepo.Object.ListAnswersByResponseIdAsync(42);
            
            // Assert
            result.Should().BeEquivalentTo(expectedAnswers);
            mockRepo.Verify(r => r.ListAnswersByResponseIdAsync(42), Times.Once);
        }
        
        [Fact]
        public async Task InsertFileAsync_ShouldReturnId()
        {
            // Arrange
            var testBlob = new byte[] { 1, 2, 3 };
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.InsertFileAsync(42, 100, "field1", "test.txt", "text/plain", 10, testBlob))
                .ReturnsAsync(99);
            
            // Act
            var result = await mockRepo.Object.InsertFileAsync(42, 100, "field1", "test.txt", "text/plain", 10, testBlob);
            
            // Assert
            result.Should().Be(99);
            mockRepo.Verify(r => r.InsertFileAsync(42, 100, "field1", "test.txt", "text/plain", 10, testBlob), Times.Once);
        }
        
        [Fact]
        public async Task GetFileAsync_ShouldReturnFile()
        {
            // Arrange
            var fileName = "test.txt";
            var contentType = "text/plain";
            var blob = new byte[] { 1, 2, 3 };

            var fileData = (FileName: fileName, ContentType: contentType, Blob: blob);
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetFileAsync(99))
                .ReturnsAsync(fileData);
            
            // Act
            var result = await mockRepo.Object.GetFileAsync(99);
            
            // Assert
            result.Should().NotBeNull();
            result.Value.FileName.Should().Be(fileName);
            result.Value.ContentType.Should().Be(contentType);
            result.Value.Blob.Should().BeEquivalentTo(blob);
            mockRepo.Verify(r => r.GetFileAsync(99), Times.Once);
        }
        
        [Fact]
        public async Task GetFileAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetFileAsync(99))
                .ReturnsAsync((ValueTuple<string, string, byte[]>?)null);
            
            // Act
            var result = await mockRepo.Object.GetFileAsync(99);
            
            // Assert
            result.Should().BeNull();
            mockRepo.Verify(r => r.GetFileAsync(99), Times.Once);
        }
        
        [Fact]
        public async Task GetResponseOwnerAsync_ShouldReturnOwner()
        {
            // Arrange
            var ownerData = (ResponseUserId: 1L, FormKey: 100);
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetResponseOwnerAsync(42))
                .ReturnsAsync(ownerData);
            
            // Act
            var result = await mockRepo.Object.GetResponseOwnerAsync(42);
            
            // Assert
            result.Should().NotBeNull();
            result.Value.ResponseUserId.Should().Be(1);
            result.Value.FormKey.Should().Be(100);
            mockRepo.Verify(r => r.GetResponseOwnerAsync(42), Times.Once);
        }
        
        [Fact]
        public async Task GetResponseOwnerAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetResponseOwnerAsync(42))
                .ReturnsAsync((ValueTuple<long, int>?)null);
            
            // Act
            var result = await mockRepo.Object.GetResponseOwnerAsync(42);
            
            // Assert
            result.Should().BeNull();
            mockRepo.Verify(r => r.GetResponseOwnerAsync(42), Times.Once);
        }
        
        [Fact]
        public async Task GetFileOwnerByIdAsync_ShouldReturnOwner()
        {
            // Arrange
            var ownerData = (ResponseId: 42L, ResponseUserId: 1L, FormKey: 100);
            
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetFileOwnerByIdAsync(99))
                .ReturnsAsync(ownerData);
            
            // Act
            var result = await mockRepo.Object.GetFileOwnerByIdAsync(99);
            
            // Assert
            result.Should().NotBeNull();
            result.Value.ResponseId.Should().Be(42);
            result.Value.ResponseUserId.Should().Be(1);
            result.Value.FormKey.Should().Be(100);
            mockRepo.Verify(r => r.GetFileOwnerByIdAsync(99), Times.Once);
        }
        
        [Fact]
        public async Task GetFileOwnerByIdAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var mockRepo = new Mock<IResponsesRepository>();
            mockRepo.Setup(r => r.GetFileOwnerByIdAsync(99))
                .ReturnsAsync((ValueTuple<long, long, int>?)null);
            
            // Act
            var result = await mockRepo.Object.GetFileOwnerByIdAsync(99);
            
            // Assert
            result.Should().BeNull();
            mockRepo.Verify(r => r.GetFileOwnerByIdAsync(99), Times.Once);
        }
    }
}
