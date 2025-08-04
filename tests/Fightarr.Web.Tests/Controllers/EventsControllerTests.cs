using Fightarr.Core.Models;
using Fightarr.Core.Services;
using Fightarr.Data.Repositories;
using Fightarr.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Fightarr.Web.Tests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IFightEventRepository> _mockEventRepository;
    private readonly Mock<IMetadataService> _mockMetadataService;
    private readonly Mock<ILogger<EventsController>> _mockLogger;
    private readonly EventsController _controller;

    public EventsControllerTests()
    {
        _mockEventRepository = new Mock<IFightEventRepository>();
        _mockMetadataService = new Mock<IMetadataService>();
        _mockLogger = new Mock<ILogger<EventsController>>();
        _controller = new EventsController(_mockEventRepository.Object, _mockMetadataService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetEvents_ReturnsOkResult_WithListOfEvents()
    {
        // Arrange
        var events = new List<FightEvent>
        {
            new FightEvent { Id = 1, EventName = "UFC 294", Promotion = "UFC", EventDate = DateTime.UtcNow.AddDays(30) },
            new FightEvent { Id = 2, EventName = "Bellator 300", Promotion = "Bellator", EventDate = DateTime.UtcNow.AddDays(45) }
        };

        _mockEventRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(events);

        // Act
        var result = await _controller.GetEvents();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<List<FightEvent>>().Subject;
        returnedEvents.Should().HaveCount(2);
        returnedEvents.Should().Contain(e => e.EventName == "UFC 294");
        returnedEvents.Should().Contain(e => e.EventName == "Bellator 300");
    }

    [Fact]
    public async Task GetEvent_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var eventId = 1;
        var fightEvent = new FightEvent 
        { 
            Id = eventId, 
            EventName = "UFC 294", 
            Promotion = "UFC", 
            EventDate = DateTime.UtcNow.AddDays(30) 
        };

        _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
            .ReturnsAsync(fightEvent);

        // Act
        var result = await _controller.GetEvent(eventId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvent = okResult.Value.Should().BeAssignableTo<FightEvent>().Subject;
        returnedEvent.Id.Should().Be(eventId);
        returnedEvent.EventName.Should().Be("UFC 294");
    }

    [Fact]
    public async Task GetEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var eventId = 999;
        _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
            .ReturnsAsync((FightEvent?)null);

        // Act
        var result = await _controller.GetEvent(eventId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetUpcomingEvents_ReturnsOkResult_WithUpcomingEvents()
    {
        // Arrange
        var upcomingEvents = new List<FightEvent>
        {
            new FightEvent { Id = 1, EventName = "UFC 295", EventDate = DateTime.UtcNow.AddDays(7) },
            new FightEvent { Id = 2, EventName = "UFC 296", EventDate = DateTime.UtcNow.AddDays(14) }
        };

        _mockEventRepository.Setup(repo => repo.GetUpcomingAsync(It.IsAny<DateTime?>()))
            .ReturnsAsync(upcomingEvents);

        // Act
        var result = await _controller.GetUpcomingEvents();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<List<FightEvent>>().Subject;
        returnedEvents.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchEvents_WithValidQuery_ReturnsOkResult()
    {
        // Arrange
        var query = "UFC";
        var searchResults = new List<FightEvent>
        {
            new FightEvent { Id = 1, EventName = "UFC 294", Promotion = "UFC" },
            new FightEvent { Id = 2, EventName = "UFC 295", Promotion = "UFC" }
        };

        _mockEventRepository.Setup(repo => repo.SearchAsync(query))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _controller.SearchEvents(query);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvents = okResult.Value.Should().BeAssignableTo<List<FightEvent>>().Subject;
        returnedEvents.Should().HaveCount(2);
        returnedEvents.Should().OnlyContain(e => e.Promotion == "UFC");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SearchEvents_WithEmptyQuery_ReturnsBadRequest(string query)
    {
        // Act
        var result = await _controller.SearchEvents(query);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AddEvent_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new AddEventRequest
        {
            ExternalId = "ufc-294-2023",
            Monitored = true,
            QualityProfileId = 1
        };

        var metadataEvent = new FightEvent
        {
            ExternalId = "ufc-294-2023",
            EventName = "UFC 294",
            Promotion = "UFC",
            EventDate = DateTime.UtcNow.AddDays(30)
        };

        var addedEvent = new FightEvent
        {
            Id = 1,
            ExternalId = "ufc-294-2023",
            EventName = "UFC 294",
            Promotion = "UFC",
            EventDate = DateTime.UtcNow.AddDays(30),
            Monitored = true,
            QualityProfileId = 1
        };

        _mockEventRepository.Setup(repo => repo.ExistsAsync(request.ExternalId))
            .ReturnsAsync(false);

        _mockMetadataService.Setup(service => service.GetEventByIdAsync(request.ExternalId))
            .ReturnsAsync(metadataEvent);

        _mockEventRepository.Setup(repo => repo.AddAsync(It.IsAny<FightEvent>()))
            .ReturnsAsync(addedEvent);

        // Act
        var result = await _controller.AddEvent(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedEvent = createdResult.Value.Should().BeAssignableTo<FightEvent>().Subject;
        returnedEvent.ExternalId.Should().Be(request.ExternalId);
        returnedEvent.Monitored.Should().Be(request.Monitored);
    }

    [Fact]
    public async Task AddEvent_WithExistingEvent_ReturnsConflict()
    {
        // Arrange
        var request = new AddEventRequest
        {
            ExternalId = "ufc-294-2023",
            Monitored = true
        };

        _mockEventRepository.Setup(repo => repo.ExistsAsync(request.ExternalId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AddEvent(request);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task AddEvent_WithEventNotFoundInMetadata_ReturnsNotFound()
    {
        // Arrange
        var request = new AddEventRequest
        {
            ExternalId = "non-existent-event",
            Monitored = true
        };

        _mockEventRepository.Setup(repo => repo.ExistsAsync(request.ExternalId))
            .ReturnsAsync(false);

        _mockMetadataService.Setup(service => service.GetEventByIdAsync(request.ExternalId))
            .ReturnsAsync((FightEvent?)null);

        // Act
        var result = await _controller.AddEvent(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateEvent_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var eventId = 1;
        var request = new UpdateEventRequest
        {
            Monitored = false,
            QualityProfileId = 2
        };

        var existingEvent = new FightEvent
        {
            Id = eventId,
            EventName = "UFC 294",
            Monitored = true,
            QualityProfileId = 1
        };

        var updatedEvent = new FightEvent
        {
            Id = eventId,
            EventName = "UFC 294",
            Monitored = false,
            QualityProfileId = 2
        };

        _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
            .ReturnsAsync(existingEvent);

        _mockEventRepository.Setup(repo => repo.UpdateAsync(It.IsAny<FightEvent>()))
            .ReturnsAsync(updatedEvent);

        // Act
        var result = await _controller.UpdateEvent(eventId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvent = okResult.Value.Should().BeAssignableTo<FightEvent>().Subject;
        returnedEvent.Monitored.Should().Be(false);
        returnedEvent.QualityProfileId.Should().Be(2);
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var eventId = 1;
        var existingEvent = new FightEvent { Id = eventId, EventName = "UFC 294" };

        _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
            .ReturnsAsync(existingEvent);

        _mockEventRepository.Setup(repo => repo.DeleteAsync(eventId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteEvent_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var eventId = 999;
        _mockEventRepository.Setup(repo => repo.GetByIdAsync(eventId))
            .ReturnsAsync((FightEvent?)null);

        // Act
        var result = await _controller.DeleteEvent(eventId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
