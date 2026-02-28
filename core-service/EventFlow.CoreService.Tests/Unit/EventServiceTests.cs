using EventFlow.CoreService.Application.DTOs;
using EventFlow.CoreService.Application.Interfaces;
using EventFlow.CoreService.Application.Services;
using EventFlow.CoreService.Domain.Entities;
using EventFlow.SharedKernel.DTOs;
using FluentAssertions;
using Moq;
using Xunit;

namespace EventFlow.CoreService.Tests.Unit;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repo = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<ILogger<EventService>> _logger = new();

    private EventService CreateSut() => new(_repo.Object, _publisher.Object, _logger.Object);

    private static CreateEventRequest ValidRequest() => new()
    {
        Title = "Test Conference",
        Description = "An amazing tech conference",
        Location = "Tbilisi, Georgia",
        StartDate = DateTime.UtcNow.AddDays(30),
        EndDate = DateTime.UtcNow.AddDays(31),
        IsPublic = true
    };

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsEventDto()
    {
        // Arrange
        var sut = CreateSut();
        var ownerId = Guid.NewGuid();
        _repo.Setup(r => r.AddAsync(It.IsAny<Event>(), default)).Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);
        _publisher.Setup(p => p.PublishCreatedAsync(It.IsAny<Event>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        // Act
        var result = await sut.CreateAsync(ValidRequest(), ownerId, "corr-id-123");

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Conference");
        result.OwnerId.Should().Be(ownerId);
        _publisher.Verify(p => p.PublishCreatedAsync(It.IsAny<Event>(), "corr-id-123", default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenEventNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var sut = CreateSut();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Event?)null);

        // Act
        var act = () => sut.UpdateAsync(Guid.NewGuid(), new UpdateEventRequest
        {
            Title = "New Title", Description = "Desc", Location = "Location",
            StartDate = DateTime.UtcNow.AddDays(1), EndDate = DateTime.UtcNow.AddDays(2)
        }, Guid.NewGuid(), "corr-id");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotOwner_ThrowsUnauthorizedException()
    {
        // Arrange
        var sut = CreateSut();
        var ownerId = Guid.NewGuid();
        var ev = Event.Create("Title", "Desc", "Location",
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), ownerId);

        _repo.Setup(r => r.GetByIdAsync(ev.Id, default)).ReturnsAsync(ev);

        var differentUserId = Guid.NewGuid();

        // Act
        var act = () => sut.DeleteAsync(ev.Id, differentUserId, "corr-id");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public void Event_Create_WithEndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Act
        var act = () => Event.Create("Title", "Desc", "Location",
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(1), Guid.NewGuid());

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*End date must be after start date*");
    }

    [Fact]
    public void Event_IsOwnedBy_ReturnsCorrectResult()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var ev = Event.Create("Title", "Desc", "Location",
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), ownerId);

        // Assert
        ev.IsOwnedBy(ownerId).Should().BeTrue();
        ev.IsOwnedBy(Guid.NewGuid()).Should().BeFalse();
    }
}
