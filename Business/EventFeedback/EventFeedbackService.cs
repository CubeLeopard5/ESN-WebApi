using AutoMapper;
using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.EventFeedback;
using Dto.User;
using Microsoft.Extensions.Logging;

namespace Business.EventFeedback;

/// <summary>
/// Implémentation du service de gestion des feedbacks d'événements
/// </summary>
public class EventFeedbackService : IEventFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<EventFeedbackService> _logger;

    public EventFeedbackService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<EventFeedbackService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FeedbackEligibilityDto> CheckEligibilityAsync(int eventId, string userEmail)
    {
        _logger.LogInformation("CheckEligibilityAsync called for EventId {EventId}, User {Email}", eventId, userEmail);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            _logger.LogWarning("Event {EventId} not found", eventId);
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            _logger.LogWarning("User {Email} not found", userEmail);
            throw new KeyNotFoundException($"User not found");
        }

        var result = new FeedbackEligibilityDto
        {
            Deadline = eventBo.FeedbackDeadline
        };

        // Check if event has a feedback form
        if (string.IsNullOrEmpty(eventBo.FeedbackFormData))
        {
            result.CanSubmit = false;
            result.Reason = "no_feedback_form";
            return result;
        }

        // Check if user attended the event
        var registration = await _unitOfWork.EventRegistrations.GetByEventAndUserAsync(eventId, user.Id);
        if (registration == null || registration.AttendanceStatus != AttendanceStatus.Present)
        {
            result.CanSubmit = false;
            result.Reason = "not_attended";
            return result;
        }

        // Check if user already submitted feedback
        var existingFeedback = await _unitOfWork.EventFeedbacks.GetByEventAndUserAsync(eventId, user.Id);
        if (existingFeedback != null)
        {
            result.HasSubmitted = true;
            result.ExistingFeedback = MapToDto(existingFeedback);

            // Can modify if deadline not passed
            if (eventBo.FeedbackDeadline.HasValue && DateTime.UtcNow > eventBo.FeedbackDeadline.Value)
            {
                result.CanSubmit = false;
                result.Reason = "deadline_passed";
            }
            else
            {
                result.CanSubmit = true;
                result.FeedbackFormData = eventBo.FeedbackFormData;
            }
            return result;
        }

        // Check if deadline passed
        if (eventBo.FeedbackDeadline.HasValue && DateTime.UtcNow > eventBo.FeedbackDeadline.Value)
        {
            result.CanSubmit = false;
            result.Reason = "deadline_passed";
            return result;
        }

        // User is eligible
        result.CanSubmit = true;
        result.FeedbackFormData = eventBo.FeedbackFormData;
        return result;
    }

    /// <inheritdoc />
    public async Task<EventFeedbackDto> SubmitFeedbackAsync(int eventId, string userEmail, SubmitFeedbackDto dto)
    {
        _logger.LogInformation("SubmitFeedbackAsync called for EventId {EventId}, User {Email}", eventId, userEmail);

        var eligibility = await CheckEligibilityAsync(eventId, userEmail);

        if (!eligibility.CanSubmit)
        {
            _logger.LogWarning("User {Email} is not eligible to submit feedback for Event {EventId}: {Reason}",
                userEmail, eventId, eligibility.Reason);
            throw new InvalidOperationException($"Cannot submit feedback: {eligibility.Reason}");
        }

        if (eligibility.HasSubmitted)
        {
            _logger.LogWarning("User {Email} has already submitted feedback for Event {EventId}", userEmail, eventId);
            throw new InvalidOperationException("Feedback already submitted. Use update instead.");
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(userEmail);

        var feedback = new EventFeedbackBo
        {
            EventId = eventId,
            UserId = user!.Id,
            FeedbackData = dto.FeedbackData,
            SubmittedAt = DateTime.UtcNow
        };

        await _unitOfWork.EventFeedbacks.AddAsync(feedback);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Feedback submitted successfully for EventId {EventId} by User {Email}", eventId, userEmail);

        // Reload with user info
        var savedFeedback = await _unitOfWork.EventFeedbacks.GetByEventAndUserAsync(eventId, user.Id);
        return MapToDto(savedFeedback!);
    }

    /// <inheritdoc />
    public async Task<EventFeedbackDto?> GetUserFeedbackAsync(int eventId, string userEmail)
    {
        _logger.LogInformation("GetUserFeedbackAsync called for EventId {EventId}, User {Email}", eventId, userEmail);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var feedback = await _unitOfWork.EventFeedbacks.GetByEventAndUserAsync(eventId, user.Id);
        return feedback == null ? null : MapToDto(feedback);
    }

    /// <inheritdoc />
    public async Task<EventFeedbackDto> UpdateFeedbackAsync(int eventId, string userEmail, SubmitFeedbackDto dto)
    {
        _logger.LogInformation("UpdateFeedbackAsync called for EventId {EventId}, User {Email}", eventId, userEmail);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        var user = await _unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        // Check deadline
        if (eventBo.FeedbackDeadline.HasValue && DateTime.UtcNow > eventBo.FeedbackDeadline.Value)
        {
            _logger.LogWarning("Cannot update feedback for Event {EventId}: deadline passed", eventId);
            throw new InvalidOperationException("Cannot update feedback: deadline passed");
        }

        // Get existing feedback (need tracking for update)
        var feedback = await _unitOfWork.EventFeedbacks.FirstOrDefaultAsync(
            f => f.EventId == eventId && f.UserId == user.Id);

        if (feedback == null)
        {
            throw new KeyNotFoundException("Feedback not found");
        }

        feedback.FeedbackData = dto.FeedbackData;
        feedback.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EventFeedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Feedback updated successfully for EventId {EventId} by User {Email}", eventId, userEmail);

        // Reload with user info
        var updatedFeedback = await _unitOfWork.EventFeedbacks.GetByEventAndUserAsync(eventId, user.Id);
        return MapToDto(updatedFeedback!);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventFeedbackDto>> GetAllFeedbacksAsync(int eventId, string adminEmail)
    {
        _logger.LogInformation("GetAllFeedbacksAsync called for EventId {EventId} by {Email}", eventId, adminEmail);

        await ValidateAdminAccessAsync(adminEmail);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        var feedbacks = await _unitOfWork.EventFeedbacks.GetByEventIdAsync(eventId);
        return feedbacks.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(int eventId, string adminEmail)
    {
        _logger.LogInformation("GetFeedbackSummaryAsync called for EventId {EventId} by {Email}", eventId, adminEmail);

        await ValidateAdminAccessAsync(adminEmail);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        // Count attendees (Present status)
        var registrations = await _unitOfWork.EventRegistrations.GetByEventIdWithAttendanceAsync(eventId);
        var totalAttendees = registrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);

        // Count feedbacks
        var totalFeedbacks = await _unitOfWork.EventFeedbacks.CountByEventIdAsync(eventId);

        return new FeedbackSummaryDto
        {
            EventId = eventId,
            EventTitle = eventBo.Title,
            TotalAttendees = totalAttendees,
            TotalFeedbacks = totalFeedbacks,
            ResponseRate = totalAttendees > 0
                ? Math.Round(totalFeedbacks * 100m / totalAttendees, 2)
                : 0,
            Deadline = eventBo.FeedbackDeadline,
            HasFeedbackForm = !string.IsNullOrEmpty(eventBo.FeedbackFormData)
        };
    }

    /// <inheritdoc />
    public async Task<bool> UpdateFeedbackFormAsync(int eventId, string adminEmail, UpdateFeedbackFormDto dto)
    {
        _logger.LogInformation("UpdateFeedbackFormAsync called for EventId {EventId} by {Email}", eventId, adminEmail);

        await ValidateAdminAccessAsync(adminEmail);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            throw new KeyNotFoundException($"Event {eventId} not found");
        }

        eventBo.FeedbackFormData = dto.FeedbackFormData;
        eventBo.FeedbackDeadline = dto.FeedbackDeadline;

        _unitOfWork.Events.Update(eventBo);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Feedback form updated for EventId {EventId} by {Email}", eventId, adminEmail);
        return true;
    }

    private async Task ValidateAdminAccessAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("User not found: {Email}", email);
            throw new UnauthorizedAccessException("User not found");
        }

        if (user.StudentType != Bo.Constants.StudentType.EsnMember && user.Role?.Name != "Admin")
        {
            _logger.LogWarning("User {Email} is not authorized to access feedback administration", email);
            throw new UnauthorizedAccessException("Only ESN members or Admins can access feedback administration");
        }
    }

    private EventFeedbackDto MapToDto(EventFeedbackBo feedback)
    {
        return new EventFeedbackDto
        {
            Id = feedback.Id,
            EventId = feedback.EventId,
            UserId = feedback.UserId,
            UserName = feedback.User != null
                ? $"{feedback.User.FirstName} {feedback.User.LastName}".Trim()
                : string.Empty,
            FeedbackData = feedback.FeedbackData,
            SubmittedAt = feedback.SubmittedAt,
            UpdatedAt = feedback.UpdatedAt,
            User = feedback.User != null ? _mapper.Map<UserDto>(feedback.User) : null
        };
    }
}
