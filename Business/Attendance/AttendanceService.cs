using AutoMapper;
using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Attendance;
using Dto.User;
using Microsoft.Extensions.Logging;

namespace Business.Attendance;

/// <summary>
/// Implémentation du service de gestion des présences aux événements
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AttendanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventAttendanceDto?> GetEventAttendanceAsync(int eventId)
    {
        _logger.LogInformation("GetEventAttendanceAsync called for EventId {EventId}", eventId);

        var eventBo = await _unitOfWork.Events.GetEventWithDetailsAsync(eventId);
        if (eventBo == null)
        {
            _logger.LogWarning("Event {EventId} not found", eventId);
            return null;
        }

        var registrations = await _unitOfWork.EventRegistrations.GetByEventIdWithAttendanceAsync(eventId);
        var registrationList = registrations.ToList();

        var stats = CalculateStats(eventId, eventBo.Title, eventBo.StartDate, registrationList);

        return new EventAttendanceDto
        {
            Id = eventBo.Id,
            Title = eventBo.Title,
            StartDate = eventBo.StartDate,
            EndDate = eventBo.EndDate,
            Location = eventBo.Location,
            Organizer = _mapper.Map<UserDto>(eventBo.User),
            Registrations = _mapper.Map<List<AttendanceDto>>(registrationList),
            Stats = stats
        };
    }

    /// <inheritdoc />
    public async Task<AttendanceStatsDto?> GetAttendanceStatsAsync(int eventId)
    {
        _logger.LogInformation("GetAttendanceStatsAsync called for EventId {EventId}", eventId);

        var eventBo = await _unitOfWork.Events.GetByIdAsync(eventId);
        if (eventBo == null)
        {
            _logger.LogWarning("Event {EventId} not found", eventId);
            return null;
        }

        var statsDict = await _unitOfWork.EventRegistrations.GetAttendanceStatsAsync(eventId);

        var totalRegistered = statsDict.Values.Sum();
        var presentCount = statsDict.GetValueOrDefault(AttendanceStatus.Present, 0);
        var absentCount = statsDict.GetValueOrDefault(AttendanceStatus.Absent, 0);
        var excusedCount = statsDict.GetValueOrDefault(AttendanceStatus.Excused, 0);
        // Find the null key entry by iterating (Dictionary doesn't support null key lookup directly)
        var notValidatedCount = statsDict
            .Where(kvp => kvp.Key == null)
            .Select(kvp => kvp.Value)
            .FirstOrDefault();
        var totalValidated = totalRegistered - notValidatedCount;

        return new AttendanceStatsDto
        {
            EventId = eventId,
            EventTitle = eventBo.Title,
            EventDate = eventBo.StartDate,
            TotalRegistered = totalRegistered,
            TotalValidated = totalValidated,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            ExcusedCount = excusedCount,
            NotYetValidatedCount = notValidatedCount,
            AttendanceRate = totalRegistered > 0
                ? Math.Round(presentCount * 100m / totalRegistered, 2)
                : 0,
            ValidationRate = totalRegistered > 0
                ? Math.Round(totalValidated * 100m / totalRegistered, 2)
                : 0
        };
    }

    /// <inheritdoc />
    public async Task<AttendanceDto> ValidateAttendanceAsync(int eventId, int registrationId, AttendanceStatus status, string validatorEmail)
    {
        _logger.LogInformation("ValidateAttendanceAsync called for EventId {EventId}, RegistrationId {RegistrationId}, Status {Status}",
            eventId, registrationId, status);

        var validator = await GetValidatorOrThrowAsync(validatorEmail);

        var registration = await _unitOfWork.EventRegistrations.GetByIdWithDetailsAsync(registrationId);
        if (registration == null || registration.EventId != eventId)
        {
            _logger.LogWarning("Registration {RegistrationId} not found for event {EventId}", registrationId, eventId);
            throw new KeyNotFoundException($"Registration {registrationId} not found for event {eventId}");
        }

        if (registration.Status != RegistrationStatus.Registered)
        {
            _logger.LogWarning("Cannot validate attendance for non-registered participant. Status: {Status}", registration.Status);
            throw new InvalidOperationException("Cannot validate attendance for non-registered participants");
        }

        registration.AttendanceStatus = status;
        registration.AttendanceValidatedAt = DateTime.UtcNow;
        registration.AttendanceValidatedById = validator.Id;

        _unitOfWork.EventRegistrations.Update(registration);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Attendance validated for RegistrationId {RegistrationId}: {Status} by {ValidatorEmail}",
            registrationId, status, validatorEmail);

        return _mapper.Map<AttendanceDto>(registration);
    }

    /// <inheritdoc />
    public async Task<int> BulkValidateAttendanceAsync(int eventId, BulkValidateAttendanceDto dto, string validatorEmail)
    {
        _logger.LogInformation("BulkValidateAttendanceAsync called for EventId {EventId}, {Count} attendances",
            eventId, dto.Attendances.Count);

        var validator = await GetValidatorOrThrowAsync(validatorEmail);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Load all registrations in one query to avoid N+1 problem
            var registrationIds = dto.Attendances.Select(a => a.RegistrationId);
            var registrationsDict = await _unitOfWork.EventRegistrations.GetByIdsAsync(registrationIds);

            var count = 0;
            foreach (var attendance in dto.Attendances)
            {
                if (registrationsDict.TryGetValue(attendance.RegistrationId, out var registration)
                    && registration.EventId == eventId
                    && registration.Status == RegistrationStatus.Registered)
                {
                    registration.AttendanceStatus = attendance.Status;
                    registration.AttendanceValidatedAt = DateTime.UtcNow;
                    registration.AttendanceValidatedById = validator.Id;
                    _unitOfWork.EventRegistrations.Update(registration);
                    count++;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Bulk attendance validated: {Count} registrations for EventId {EventId} by {ValidatorEmail}",
                count, eventId, validatorEmail);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk attendance validation for EventId {EventId}", eventId);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResetAttendanceAsync(int eventId, int registrationId, string validatorEmail)
    {
        _logger.LogInformation("ResetAttendanceAsync called for EventId {EventId}, RegistrationId {RegistrationId}",
            eventId, registrationId);

        await GetValidatorOrThrowAsync(validatorEmail);

        var registration = await _unitOfWork.EventRegistrations.GetByIdAsync(registrationId);
        if (registration == null || registration.EventId != eventId)
        {
            _logger.LogWarning("Registration {RegistrationId} not found for event {EventId}", registrationId, eventId);
            return false;
        }

        registration.AttendanceStatus = null;
        registration.AttendanceValidatedAt = null;
        registration.AttendanceValidatedById = null;

        _unitOfWork.EventRegistrations.Update(registration);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Attendance reset for RegistrationId {RegistrationId} by {ValidatorEmail}",
            registrationId, validatorEmail);

        return true;
    }

    private async Task<UserBo> GetValidatorOrThrowAsync(string validatorEmail)
    {
        var validator = await _unitOfWork.Users.GetByEmailAsync(validatorEmail);
        if (validator == null)
        {
            _logger.LogWarning("Validator not found: {Email}", validatorEmail);
            throw new UnauthorizedAccessException("Validator not found");
        }

        if (validator.StudentType != Bo.Constants.StudentType.EsnMember && validator.Role?.Name != "Admin")
        {
            _logger.LogWarning("User {Email} is not authorized to validate attendance", validatorEmail);
            throw new UnauthorizedAccessException("Only ESN members or Admins can validate attendance");
        }

        return validator;
    }

    private static AttendanceStatsDto CalculateStats(int eventId, string eventTitle, DateTime eventDate, List<EventRegistrationBo> registrations)
    {
        var totalRegistered = registrations.Count;
        var presentCount = registrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);
        var absentCount = registrations.Count(r => r.AttendanceStatus == AttendanceStatus.Absent);
        var excusedCount = registrations.Count(r => r.AttendanceStatus == AttendanceStatus.Excused);
        var notValidatedCount = registrations.Count(r => r.AttendanceStatus == null);
        var totalValidated = totalRegistered - notValidatedCount;

        return new AttendanceStatsDto
        {
            EventId = eventId,
            EventTitle = eventTitle,
            EventDate = eventDate,
            TotalRegistered = totalRegistered,
            TotalValidated = totalValidated,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            ExcusedCount = excusedCount,
            NotYetValidatedCount = notValidatedCount,
            AttendanceRate = totalRegistered > 0
                ? Math.Round(presentCount * 100m / totalRegistered, 2)
                : 0,
            ValidationRate = totalRegistered > 0
                ? Math.Round(totalValidated * 100m / totalRegistered, 2)
                : 0
        };
    }
}
