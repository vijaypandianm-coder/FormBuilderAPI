using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;
using Moq;

namespace FormBuilderAPI.UnitTests.Fakes
{
    public class FakeMongoDbContext 
    {
        private readonly Mock<IMongoCollection<Form>> _mockForms;
        private readonly Mock<IMongoCollection<Counter>> _mockCounters;

        public FakeMongoDbContext()
        {
            _mockForms = new Mock<IMongoCollection<Form>>();
            _mockCounters = new Mock<IMongoCollection<Counter>>();
        }

        public IMongoCollection<Form> Forms => _mockForms.Object;
        public IMongoCollection<Counter> Counters => _mockCounters.Object;

        public void SetupFindAsync(Form form)
        {
            var cursor = new Mock<IAsyncCursor<Form>>();
            cursor.Setup(c => c.Current).Returns(new List<Form> { form });
            cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockForms
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Form>>(),
                    It.IsAny<FindOptions<Form>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);
        }

        public void SetupFindAsync(List<Form> forms)
        {
            var cursor = new Mock<IAsyncCursor<Form>>();
            cursor.Setup(c => c.Current).Returns(forms);
            cursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockForms
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Form>>(),
                    It.IsAny<FindOptions<Form>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cursor.Object);
        }
    }
}