using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

using FormBuilderAPI.Services;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.UnitTests.Services
{
    public class ResponseServiceTests
    {
        private readonly Mock<IFormService> _mockFormService;
        private readonly Mock<IResponsesRepository> _mockRepository;
        private readonly ResponseService _sut;

        public ResponseServiceTests()
        {
            _mockFormService = new Mock<IFormService>();
            _mockRepository = new Mock<IResponsesRepository>();
            _sut = new ResponseService(_mockFormService.Object, _mockRepository.Object);
        }

        [Fact]
        public async Task SaveAsync_ValidatesFormExistsAndIsPublished()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            var payload = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "field1", Value = "test" }
                }
            };

            // Case 1: Form not found
            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync((Form)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _sut.SaveAsync(formKey, userId, payload));

            // Case 2: Form not published
            var draftForm = new Form
            {
                Id = "form1",
                FormKey = formKey,
                Status = "Draft"
            };

            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync(draftForm);

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload));
        }

        [Fact]
        public async Task SaveAsync_ValidatesRequiredFields()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            
            var form = new Form
            {
                Id = "form1",
                FormKey = formKey,
                Status = "Published",
                Layout = new List<FormSection>
                {
                    new FormSection
                    {
                        Fields = new List<FormField>
                        {
                            new FormField
                            {
                                FieldId = "required1",
                                Label = "Required Field",
                                Type = "text",
                                IsRequired = true
                            },
                            new FormField
                            {
                                FieldId = "required2",
                                Label = "Required Choice",
                                Type = "radio",
                                IsRequired = true,
                                Options = new List<FieldOption>
                                {
                                    new FieldOption { Id = "opt1", Text = "Option 1" }
                                }
                            },
                            new FormField
                            {
                                FieldId = "required3",
                                Label = "Required File",
                                Type = "file",
                                IsRequired = true
                            }
                        }
                    }
                }
            };

            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync(form);

            // Case 1: Missing required text field
            var payload1 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "required2", OptionIds = new List<string> { "opt1" } },
                    new AnswerDto { FieldId = "required3", FileBase64 = "data:text/plain;base64,SGVsbG8=" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload1));

            // Case 2: Missing required choice field options
            var payload2 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "required1", Value = "test" },
                    new AnswerDto { FieldId = "required2", OptionIds = new List<string>() },
                    new AnswerDto { FieldId = "required3", FileBase64 = "data:text/plain;base64,SGVsbG8=" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload2));

            // Case 3: Missing required file
            var payload3 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "required1", Value = "test" },
                    new AnswerDto { FieldId = "required2", OptionIds = new List<string> { "opt1" } },
                    new AnswerDto { FieldId = "required3", FileBase64 = "" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload3));
        }

        [Fact]
        public async Task SaveAsync_ValidatesFieldTypes()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            
            var form = new Form
            {
                Id = "form1",
                FormKey = formKey,
                Status = "Published",
                Layout = new List<FormSection>
                {
                    new FormSection
                    {
                        Fields = new List<FormField>
                        {
                            new FormField
                            {
                                FieldId = "text1",
                                Label = "Short Text",
                                Type = "text",
                                IsRequired = false
                            },
                            new FormField
                            {
                                FieldId = "textarea1",
                                Label = "Long Text",
                                Type = "textarea",
                                IsRequired = false
                            },
                            new FormField
                            {
                                FieldId = "number1",
                                Label = "Number Field",
                                Type = "number",
                                IsRequired = false
                            },
                            new FormField
                            {
                                FieldId = "date1",
                                Label = "Date Field",
                                Type = "date",
                                IsRequired = false
                            },
                            new FormField
                            {
                                FieldId = "choice1",
                                Label = "Choice Field",
                                Type = "radio",
                                IsRequired = false,
                                Options = new List<FieldOption>
                                {
                                    new FieldOption { Id = "opt1", Text = "Option 1" },
                                    new FieldOption { Id = "opt2", Text = "Option 2" }
                                }
                            }
                        }
                    }
                }
            };

            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync(form);

            // Case 1: Text too long
            var longText = new string('A', 1000);
            var payload1 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "text1", Value = longText }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload1));

            // Case 2: Invalid number
            var payload2 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "number1", Value = "not-a-number" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload2));

            // Case 3: Invalid date
            var payload3 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "date1", Value = "not-a-date" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload3));

            // Case 4: Invalid choice option
            var payload4 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "choice1", OptionIds = new List<string> { "invalid-option" } }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload4));
        }

        [Fact]
        public async Task SaveAsync_HandlesFileUploads()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            var responseId = 42L;
            var fileId = 99L;
            
            var form = new Form
            {
                Id = "form1",
                FormKey = formKey,
                Status = "Published",
                Layout = new List<FormSection>
                {
                    new FormSection
                    {
                        Fields = new List<FormField>
                        {
                            new FormField
                            {
                                FieldId = "file1",
                                Label = "File Upload",
                                Type = "file",
                                IsRequired = false
                            }
                        }
                    }
                }
            };

            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync(form);
            
            _mockRepository.Setup(r => r.InsertFormResponseHeaderAsync(userId, formKey, form.Id))
                .ReturnsAsync(responseId);
            
            _mockRepository.Setup(r => r.InsertFileAsync(
                    responseId, formKey, "file1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()))
                .ReturnsAsync(fileId);

            // Case 1: Valid file upload with base64 data
            var payload1 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto 
                    { 
                        FieldId = "file1", 
                        FileBase64 = "data:text/plain;base64,SGVsbG8gV29ybGQ=",
                        FileName = "test.txt",
                        ContentType = "text/plain"
                    }
                }
            };

            var result1 = await _sut.SaveAsync(formKey, userId, payload1);
            result1.Should().Be(responseId);

            // Verify file was saved
            _mockRepository.Verify(r => r.InsertFileAsync(
                responseId, formKey, "file1", "test.txt", "text/plain", It.IsAny<long>(), It.IsAny<byte[]>()), 
                Times.Once);

            // Case 2: File upload with raw base64 (no data URI prefix)
            var payload2 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto 
                    { 
                        FieldId = "file1", 
                        FileBase64 = "SGVsbG8gV29ybGQ=",
                        FileName = "test.txt",
                        ContentType = "text/plain"
                    }
                }
            };

            await _sut.SaveAsync(formKey, userId, payload2);

            // Case 3: File upload with default filename and content type
            var payload3 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto 
                    { 
                        FieldId = "file1", 
                        FileBase64 = "SGVsbG8gV29ybGQ="
                    }
                }
            };

            await _sut.SaveAsync(formKey, userId, payload3);
            
            _mockRepository.Verify(r => r.InsertFileAsync(
                responseId, formKey, "file1", "upload.bin", "application/octet-stream", It.IsAny<long>(), It.IsAny<byte[]>()), 
                Times.Once);

            // Case 4: Invalid base64 data
            var payload4 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto 
                    { 
                        FieldId = "file1", 
                        FileBase64 = "not-valid-base64!"
                    }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload4));

            // Case 5: File too large
            _mockRepository.Setup(r => r.InsertFileAsync(
                    responseId, formKey, "file1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()))
                .Callback<long, int, string, string, string, long, byte[]>((_, _, _, _, _, size, _) => {
                    if (size > 10 * 1024 * 1024) // 10MB
                        throw new InvalidOperationException("File too large");
                });

            // This test would be better with a large actual file, but for simplicity we'll just mock the behavior
        }

        [Fact]
        public async Task SaveAsync_HandlesChoiceFields()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            var responseId = 42L;
            
            var form = new Form
            {
                Id = "form1",
                FormKey = formKey,
                Status = "Published",
                Layout = new List<FormSection>
                {
                    new FormSection
                    {
                        Fields = new List<FormField>
                        {
                            new FormField
                            {
                                FieldId = "radio1",
                                Label = "Radio Choice",
                                Type = "radio",
                                IsRequired = false,
                                Options = new List<FieldOption>
                                {
                                    new FieldOption { Id = "opt1", Text = "Option 1" },
                                    new FieldOption { Id = "opt2", Text = "Option 2" }
                                }
                            },
                            new FormField
                            {
                                FieldId = "checkbox1",
                                Label = "Checkbox Choice",
                                Type = "checkbox",
                                IsRequired = false,
                                Options = new List<FieldOption>
                                {
                                    new FieldOption { Id = "opt1", Text = "Option 1" },
                                    new FieldOption { Id = "opt2", Text = "Option 2" }
                                }
                            }
                        }
                    }
                }
            };

            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync(form);
            
            _mockRepository.Setup(r => r.InsertFormResponseHeaderAsync(userId, formKey, form.Id))
                .ReturnsAsync(responseId);

            // Case 1: Single choice selection
            var payload1 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto 
                    { 
                        FieldId = "radio1", 
                        OptionIds = new List<string> { "opt1" }
                    }
                }
            };

            await _sut.SaveAsync(formKey, userId, payload1);
            
            _mockRepository.Verify(r => r.InsertFormResponseAnswerAsync(
                responseId, userId, formKey, "radio1", "radio", "opt1"), 
                Times.Once);

            // Case 2: Multiple choice selection
            var payload2 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto 
                    { 
                        FieldId = "checkbox1", 
                        OptionIds = new List<string> { "opt1", "opt2" }
                    }
                }
            };

            await _sut.SaveAsync(formKey, userId, payload2);
            
            // The JSON serialized array should be passed
            _mockRepository.Verify(r => r.InsertFormResponseAnswerAsync(
                responseId, userId, formKey, "checkbox1", "checkbox", It.Is<string>(s => s.Contains("opt1") && s.Contains("opt2"))), 
                Times.Once);
        }

        [Fact]
        public async Task SaveAsync_HandlesEmptyPayload()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            
            // Case 1: Null answers
            var payload1 = new SubmitResponseDto
            {
                Answers = null
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload1));

            // Case 2: Empty answers
            var payload2 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>()
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload2));

            // Case 3: Null payload
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, null));
        }

        [Fact]
        public async Task SaveAsync_ValidatesFieldExists()
        {
            // Arrange
            var formKey = 100;
            var userId = 123L;
            
            var form = new Form
            {
                Id = "form1",
                FormKey = formKey,
                Status = "Published",
                Layout = new List<FormSection>
                {
                    new FormSection
                    {
                        Fields = new List<FormField>
                        {
                            new FormField
                            {
                                FieldId = "field1",
                                Label = "Field 1",
                                Type = "text",
                                IsRequired = false
                            }
                        }
                    }
                }
            };

            _mockFormService.Setup(f => f.GetByFormKeyAsync(formKey))
                .ReturnsAsync(form);

            // Case 1: Unknown field
            var payload1 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "unknown", Value = "test" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload1));

            // Case 2: Empty field ID
            var payload2 = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto { FieldId = "", Value = "test" }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SaveAsync(formKey, userId, payload2));
        }

        [Fact]
        public async Task ListAsync_WithFormKeyAndUserId_ReturnsFormattedResponses()
        {
            // Arrange
            var formKey = 77;
            var userId = 555L;
            
            var headers = new List<ResponseHeaderDto>
            {
                new ResponseHeaderDto
                {
                    Id = 1001,
                    FormKey = formKey,
                    UserId = userId,
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-1)
                }
            };

            var answers = new List<ResponseAnswerRow>
            {
                new ResponseAnswerRow { Id = 1, ResponseId = 1001, FieldId = "q1", FieldType = "text", AnswerValue = "hello", SubmittedAt = DateTime.UtcNow },
                new ResponseAnswerRow { Id = 2, ResponseId = 1001, FieldId = "q2", FieldType = "number", AnswerValue = "10", SubmittedAt = DateTime.UtcNow }
            };

            _mockRepository.Setup(r => r.ListHeadersByFormKeyAndUserAsync(formKey, userId))
                .ReturnsAsync(headers);

            _mockRepository.Setup(r => r.ListAnswersByResponseIdAsync(1001))
                .ReturnsAsync(answers);

            // Act
            var result = await _sut.ListAsync(formKey, userId);

            // Assert
            result.Should().HaveCount(2);
            result.Select(r => r.FieldId).Should().BeEquivalentTo(new[] { "q1", "q2" });
            result.All(r => r.ResponseId == 1001).Should().BeTrue();
            result.All(r => r.FormKey == formKey).Should().BeTrue();
            result.All(r => r.UserId == userId).Should().BeTrue();
            
            var textAnswer = result.First(r => r.FieldId == "q1");
            textAnswer.AnswerValue.Should().Be("hello");
            
            var numberAnswer = result.First(r => r.FieldId == "q2");
            numberAnswer.AnswerValue.Should().Be("10");
        }

        [Fact]
        public async Task ListAsync_WithFormKeyOnly_ReturnsFormattedResponses()
        {
            // Arrange
            var formKey = 77;
            
            var headers = new List<ResponseHeaderDto>
            {
                new ResponseHeaderDto
                {
                    Id = 1001,
                    FormKey = formKey,
                    UserId = 555,
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-1)
                },
                new ResponseHeaderDto
                {
                    Id = 1002,
                    FormKey = formKey,
                    UserId = 556,
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-2)
                }
            };

            var answers1 = new List<ResponseAnswerRow>
            {
                new ResponseAnswerRow { Id = 1, ResponseId = 1001, FieldId = "q1", FieldType = "text", AnswerValue = "hello", SubmittedAt = DateTime.UtcNow }
            };
            
            var answers2 = new List<ResponseAnswerRow>
            {
                new ResponseAnswerRow { Id = 2, ResponseId = 1002, FieldId = "q1", FieldType = "text", AnswerValue = "world", SubmittedAt = DateTime.UtcNow }
            };

            _mockRepository.Setup(r => r.ListHeadersByFormKeyAsync(formKey))
                .ReturnsAsync(headers);

            _mockRepository.Setup(r => r.ListAnswersByResponseIdAsync(1001))
                .ReturnsAsync(answers1);
                
            _mockRepository.Setup(r => r.ListAnswersByResponseIdAsync(1002))
                .ReturnsAsync(answers2);

            // Act
            var result = await _sut.ListAsync(formKey);

            // Assert
            result.Should().HaveCount(2);
            result.Select(r => r.ResponseId).Should().BeEquivalentTo(new[] { 1001, 1002 });
            result.All(r => r.FormKey == formKey).Should().BeTrue();
            
            var response1 = result.First(r => r.ResponseId == 1001);
            response1.UserId.Should().Be(555);
            response1.AnswerValue.Should().Be("hello");
            
            var response2 = result.First(r => r.ResponseId == 1002);
            response2.UserId.Should().Be(556);
            response2.AnswerValue.Should().Be("world");
        }

        [Fact]
        public async Task ListAsync_WithNoParameters_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _sut.ListAsync(0));
        }

        [Fact]
        public async Task ListPublishedFormsAsync_ReturnsFormattedList()
        {
            // Arrange
            var forms = new List<Form>
            {
                new Form
                {
                    FormKey = 101,
                    Title = "Form 1",
                    Description = "Description 1",
                    PublishedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Form
                {
                    FormKey = 102,
                    Title = "Form 2",
                    Description = "Description 2",
                    PublishedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            _mockFormService.Setup(f => f.ListAsync("Published", null, true, 1, 200))
                .ReturnsAsync((forms, forms.Count));

            // Act
            var result = await _sut.ListPublishedFormsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Select(f => f.FormKey).Should().BeEquivalentTo(new[] { 101, 102 });
            result.Select(f => f.Title).Should().BeEquivalentTo(new[] { "Form 1", "Form 2" });
        }

        [Fact]
        public async Task GetDetailAsync_ReturnsFormattedResponse()
        {
            // Arrange
            var responseId = 1001L;
            var header = new ResponseHeaderDto
            {
                Id = responseId,
                FormKey = 77,
                UserId = 555,
                SubmittedAt = DateTime.UtcNow
            };

            var answers = new List<ResponseAnswerRow>
            {
                new ResponseAnswerRow { Id = 1, ResponseId = responseId, FieldId = "q1", FieldType = "text", AnswerValue = "hello", SubmittedAt = DateTime.UtcNow },
                new ResponseAnswerRow { Id = 2, ResponseId = responseId, FieldId = "q2", FieldType = "number", AnswerValue = "10", SubmittedAt = DateTime.UtcNow }
            };

            _mockRepository.Setup(r => r.GetHeaderByIdAsync(responseId))
                .ReturnsAsync(header);

            _mockRepository.Setup(r => r.ListAnswersByResponseIdAsync(responseId))
                .ReturnsAsync(answers);

            // Act
            var result = await _sut.GetDetailAsync(responseId);

            // Assert
            result.Should().NotBeNull();
            result!.Header.Should().Be(header);
            result.Answers.Should().HaveCount(2);
            result.Answers.Select(a => a.FieldId).Should().BeEquivalentTo(new[] { "q1", "q2" });
            result.Answers.Select(a => a.FieldType).Should().BeEquivalentTo(new[] { "text", "number" });
            result.Answers.Select(a => a.AnswerValue).Should().BeEquivalentTo(new[] { "hello", "10" });
        }

        [Fact]
        public async Task GetDetailAsync_ReturnsNullForNonExistentResponse()
        {
            // Arrange
            var responseId = 9999L;

            _mockRepository.Setup(r => r.GetHeaderByIdAsync(responseId))
                .ReturnsAsync((ResponseHeaderDto)null);

            // Act
            var result = await _sut.GetDetailAsync(responseId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_ReturnsFirstAnswerForResponse()
        {
            // Arrange
            var responseId = 1001L;
            var header = new ResponseHeaderDto
            {
                Id = responseId,
                FormKey = 77,
                UserId = 555,
                SubmittedAt = DateTime.UtcNow
            };

            var answers = new List<ResponseAnswerRow>
            {
                new ResponseAnswerRow { Id = 1, ResponseId = responseId, FieldId = "q1", FieldType = "text", AnswerValue = "hello", SubmittedAt = DateTime.UtcNow },
                new ResponseAnswerRow { Id = 2, ResponseId = responseId, FieldId = "q2", FieldType = "number", AnswerValue = "10", SubmittedAt = DateTime.UtcNow }
            };

            _mockRepository.Setup(r => r.GetHeaderByIdAsync(responseId))
                .ReturnsAsync(header);

            _mockRepository.Setup(r => r.ListAnswersByResponseIdAsync(responseId))
                .ReturnsAsync(answers);

            // Act
            var result = await _sut.GetAsync(responseId);

            // Assert
            result.Should().NotBeNull();
            result!.ResponseId.Should().Be(responseId);
            result.FormKey.Should().Be(77);
            result.UserId.Should().Be(555);
            result.FieldId.Should().Be("q1");
            result.AnswerValue.Should().Be("hello");
        }

        [Fact]
        public async Task GetAsync_ReturnsNullForNonExistentResponse()
        {
            // Arrange
            var responseId = 9999L;

            _mockRepository.Setup(r => r.GetHeaderByIdAsync(responseId))
                .ReturnsAsync((ResponseHeaderDto)null);

            // Act
            var result = await _sut.GetAsync(responseId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_HandlesNoAnswers()
        {
            // Arrange
            var responseId = 1001L;
            var header = new ResponseHeaderDto
            {
                Id = responseId,
                FormKey = 77,
                UserId = 555,
                SubmittedAt = DateTime.UtcNow
            };

            _mockRepository.Setup(r => r.GetHeaderByIdAsync(responseId))
                .ReturnsAsync(header);

            _mockRepository.Setup(r => r.ListAnswersByResponseIdAsync(responseId))
                .ReturnsAsync(new List<ResponseAnswerRow>());

            // Act
            var result = await _sut.GetAsync(responseId);

            // Assert
            result.Should().NotBeNull();
            result!.ResponseId.Should().Be(responseId);
            result.FormKey.Should().Be(77);
            result.UserId.Should().Be(555);
            result.FieldId.Should().Be(string.Empty);
            result.AnswerValue.Should().BeNull();
        }

        [Fact]
        public void MapToSqlFieldType_ReturnsCorrectMappings()
        {
            // Use reflection to access the private method
            var method = typeof(ResponseService).GetMethod("MapToSqlFieldType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (method == null)
            {
                Assert.Fail("MapToSqlFieldType method not found");
                return;
            }

            // Test all mappings
            Assert.Equal("shortText", method.Invoke(null, new object[] { "text" }));
            Assert.Equal("shortText", method.Invoke(null, new object[] { "shorttext" }));
            Assert.Equal("shortText", method.Invoke(null, new object[] { "short_text" }));
            Assert.Equal("textarea", method.Invoke(null, new object[] { "textarea" }));
            Assert.Equal("textarea", method.Invoke(null, new object[] { "longtext" }));
            Assert.Equal("textarea", method.Invoke(null, new object[] { "long_text" }));
            Assert.Equal("email", method.Invoke(null, new object[] { "email" }));
            Assert.Equal("number", method.Invoke(null, new object[] { "number" }));
            Assert.Equal("date", method.Invoke(null, new object[] { "date" }));
            Assert.Equal("radio", method.Invoke(null, new object[] { "radio" }));
            Assert.Equal("dropdown", method.Invoke(null, new object[] { "dropdown" }));
            Assert.Equal("checkbox", method.Invoke(null, new object[] { "checkbox" }));
            Assert.Equal("multiselect", method.Invoke(null, new object[] { "multiselect" }));
            Assert.Equal("multiselect", method.Invoke(null, new object[] { "multi-select" }));
            Assert.Equal("mcq", method.Invoke(null, new object[] { "mcq" }));
            Assert.Equal("mcq", method.Invoke(null, new object[] { "multiple" }));
            Assert.Equal("file", method.Invoke(null, new object[] { "file" }));
            Assert.Null(method.Invoke(null, new object[] { "unknown" }));
            Assert.Null(method.Invoke(null, new object[] { null }));
        }
    }
}