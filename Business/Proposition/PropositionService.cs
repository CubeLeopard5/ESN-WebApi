using AutoMapper;
using Bo.Constants;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto;
using Dto.Common;
using Microsoft.Extensions.Logging;

namespace Business.Proposition;

/// <summary>
/// Interface de gestion des propositions d'activités et système de vote
/// </summary>
public class PropositionService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<PropositionService> logger)
    : IPropositionService
{
    /// <inheritdoc />
    [Obsolete("Use GetAllPropositionsAsync(PaginationParams pagination) instead for better performance and memory management")]
    public async Task<IEnumerable<PropositionDto>> GetAllPropositionsAsync()
    {
        logger.LogInformation("PropositionService.GetAllPropositionsAsync called (non-paginated - deprecated)");

        var propositions = await unitOfWork.Propositions.GetAllPropositionsWithDetailsAsync();

        logger.LogInformation("PropositionService.GetAllPropositionsAsync completed, returning {Count} propositions", propositions.Count());

        return mapper.Map<IEnumerable<PropositionDto>>(propositions);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PropositionDto>> GetAllPropositionsAsync(PaginationParams pagination, string? userEmail = null)
    {
        logger.LogInformation("PropositionService.GetAllPropositionsAsync (paginated) called - Page {PageNumber}, Size {PageSize}, UserEmail: {UserEmail}",
            pagination.PageNumber, pagination.PageSize, userEmail ?? "anonymous");

        var (items, totalCount) = await unitOfWork.Propositions.GetPagedAsync(
            pagination.Skip,
            pagination.PageSize);

        var dtos = mapper.Map<List<PropositionDto>>(items);

        // Si un userEmail est fourni, récupérer les votes de l'utilisateur
        if (!string.IsNullOrEmpty(userEmail))
        {
            var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
            if (user != null)
            {
                // Récupérer tous les votes de l'utilisateur pour les propositions de cette page
                var propositionIds = dtos.Select(p => p.Id).ToList();
                var userVotes = await unitOfWork.PropositionVotes.GetUserVotesForPropositionsAsync(user.Id, propositionIds);

                // Remplir le champ UserVoteType pour chaque proposition
                foreach (var dto in dtos)
                {
                    var vote = userVotes.FirstOrDefault(v => v.PropositionId == dto.Id);
                    dto.UserVoteType = vote != null ? ConvertVoteTypeToDto(vote.VoteType) : null;
                }
            }
        }

        logger.LogInformation("PropositionService.GetAllPropositionsAsync (paginated) completed - Returned {Count} of {TotalCount}",
            dtos.Count, totalCount);

        return new PagedResult<PropositionDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <inheritdoc />
    public async Task<PropositionDto?> GetPropositionByIdAsync(int id, string? userEmail = null)
    {
        logger.LogInformation("PropositionService.GetPropositionByIdAsync called for PropositionId {Id}, UserEmail: {UserEmail}",
            id, userEmail ?? "anonymous");

        var proposition = await unitOfWork.Propositions.GetPropositionWithDetailsAsync(id);

        if (proposition == null)
        {
            logger.LogWarning("PropositionService.GetPropositionByIdAsync - Proposition {Id} not found", id);
            return null;
        }

        var dto = mapper.Map<PropositionDto>(proposition);

        // Si un userEmail est fourni, récupérer le vote de l'utilisateur
        if (!string.IsNullOrEmpty(userEmail))
        {
            var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
            if (user != null)
            {
                var userVote = await unitOfWork.PropositionVotes.GetByPropositionAndUserAsync(id, user.Id);
                dto.UserVoteType = userVote != null ? ConvertVoteTypeToDto(userVote.VoteType) : null;
            }
        }

        logger.LogInformation("PropositionService.GetPropositionByIdAsync completed for PropositionId {Id}", id);

        return dto;
    }

    /// <inheritdoc />
    public async Task<PropositionDto> CreatePropositionAsync(PropositionDto propositionDto, string userEmail)
    {
        logger.LogInformation("PropositionService.CreatePropositionAsync called with Title {Title} by {Email}",
            propositionDto.Title, userEmail);

        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("PropositionService.CreatePropositionAsync failed - user not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        var proposition = mapper.Map<Bo.Models.PropositionBo>(propositionDto);
        proposition.CreatedAt = DateTime.UtcNow;
        proposition.IsDeleted = false;
        proposition.UserId = user.Id;

        await unitOfWork.Propositions.AddAsync(proposition);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("PropositionService.CreatePropositionAsync completed for {Title} by {Email}",
            proposition.Title, user.Email);

        return mapper.Map<PropositionDto>(proposition);
    }

    /// <inheritdoc />
    public async Task<PropositionDto?> UpdatePropositionAsync(int id, PropositionDto propositionDto, string userEmail)
    {
        logger.LogInformation("PropositionService.UpdatePropositionAsync called for PropositionId {Id} with Title {Title} by {Email}",
            id, propositionDto.Title, userEmail);

        var existing = await unitOfWork.Propositions.GetByIdAsync(id);
        if (existing == null || existing.IsDeleted)
        {
            logger.LogWarning("PropositionService.UpdatePropositionAsync - Proposition {Id} not found", id);
            return null;
        }

        // Get the user to verify ownership
        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("PropositionService.UpdatePropositionAsync - User not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        // Verify ownership
        if (existing.UserId != user.Id)
        {
            logger.LogWarning("PropositionService.UpdatePropositionAsync - User {Email} (ID: {UserId}) tried to update Proposition {Id} owned by UserId {OwnerId}",
                userEmail, user.Id, id, existing.UserId);
            throw new UnauthorizedAccessException("You don't have permission to update this proposition");
        }

        mapper.Map(propositionDto, existing);
        existing.ModifiedAt = DateTime.UtcNow;

        try
        {
            unitOfWork.Propositions.Update(existing);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("PropositionService.UpdatePropositionAsync - Proposition {Id} updated successfully", id);
        }
        catch (Exception ex)
        {
            if (!await unitOfWork.Propositions.AnyAsync(e => e.Id == id))
            {
                logger.LogWarning("PropositionService.UpdatePropositionAsync - Concurrency failure for PropositionId {Id}, record not found", id);
                return null;
            }
            logger.LogError(ex, "PropositionService.UpdatePropositionAsync - Concurrency exception for PropositionId {Id}", id);
            throw;
        }

        logger.LogInformation("PropositionService.UpdatePropositionAsync completed for PropositionId {Id}", id);

        return mapper.Map<PropositionDto>(existing);
    }

    /// <inheritdoc />
    public async Task<PropositionDto?> DeletePropositionAsync(int id, string userEmail)
    {
        logger.LogInformation("PropositionService.DeletePropositionAsync called for PropositionId {Id} by {Email}", id, userEmail);

        var proposition = await unitOfWork.Propositions.GetByIdAsync(id);
        if (proposition == null || proposition.IsDeleted)
        {
            logger.LogWarning("PropositionService.DeletePropositionAsync - Proposition {Id} not found", id);
            return null;
        }

        // Get the user to verify authorization
        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("PropositionService.DeletePropositionAsync - User not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        // Verify authorization: owner, ESN member, or admin
        bool isOwner = proposition.UserId == user.Id;
        bool isEsnMember = user.StudentType?.ToLower() == "esn_member";
        bool isAdmin = user.Role?.Name == UserRole.Admin;

        if (!isOwner && !isEsnMember && !isAdmin)
        {
            logger.LogWarning("PropositionService.DeletePropositionAsync - User {Email} (ID: {UserId}) tried to delete Proposition {Id} owned by UserId {OwnerId}",
                userEmail, user.Id, id, proposition.UserId);
            throw new UnauthorizedAccessException("You don't have permission to delete this proposition");
        }

        // Soft delete
        proposition.IsDeleted = true;
        proposition.DeletedAt = DateTime.UtcNow;

        unitOfWork.Propositions.Update(proposition);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("PropositionService.DeletePropositionAsync completed for PropositionId {Id}", id);

        return mapper.Map<PropositionDto>(proposition);
    }

    /// <inheritdoc />
    public async Task<PropositionDto?> VoteUpAsync(int id, string userEmail)
        => await VoteAsync(id, userEmail, Bo.Models.VoteType.Up);

    /// <inheritdoc />
    public async Task<PropositionDto?> VoteDownAsync(int id, string userEmail)
        => await VoteAsync(id, userEmail, Bo.Models.VoteType.Down);

    private async Task<PropositionDto?> VoteAsync(int id, string userEmail, Bo.Models.VoteType voteType)
    {
        logger.LogInformation("PropositionService.VoteAsync called for PropositionId {Id} by {Email} with {VoteType}",
            id, userEmail, voteType);

        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("PropositionService.VoteAsync failed - user not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        var proposition = await unitOfWork.Propositions.GetByIdAsync(id);
        if (proposition == null || proposition.IsDeleted)
        {
            logger.LogWarning("PropositionService.VoteAsync - Proposition {Id} not found", id);
            return null;
        }

        // Check if user has already voted - filtrage au niveau de la base de données
        var existingVote = await unitOfWork.PropositionVotes.GetByPropositionAndUserAsync(id, user.Id);

        if (existingVote != null)
        {
            // If user already voted with the same type, REMOVE the vote (toggle behavior)
            if (existingVote.VoteType == voteType)
            {
                logger.LogInformation("User {Email} removing {VoteType} vote on Proposition {Id}",
                    userEmail, voteType, id);
                unitOfWork.PropositionVotes.Delete(existingVote);
            }
            else
            {
                // If user voted differently, change the vote
                logger.LogInformation("User {Email} changing vote from {OldVote} to {NewVote} on Proposition {Id}",
                    userEmail, existingVote.VoteType, voteType, id);
                existingVote.VoteType = voteType;
                existingVote.UpdatedAt = DateTime.UtcNow;
                unitOfWork.PropositionVotes.Update(existingVote);
            }
        }
        else
        {
            // Create new vote
            logger.LogInformation("User {Email} adding new {VoteType} vote on Proposition {Id}",
                userEmail, voteType, id);
            var newVote = new Bo.Models.PropositionVoteBo
            {
                PropositionId = id,
                UserId = user.Id,
                VoteType = voteType,
                CreatedAt = DateTime.UtcNow
            };
            await unitOfWork.PropositionVotes.AddAsync(newVote);
        }

        // IMPORTANT: Save the vote changes FIRST before recounting
        await unitOfWork.SaveChangesAsync();

        // Recalculate vote counts AFTER saving - utilisation des méthodes optimisées
        proposition.VotesUp = await unitOfWork.PropositionVotes.CountUpVotesAsync(id);
        proposition.VotesDown = await unitOfWork.PropositionVotes.CountDownVotesAsync(id);

        proposition.ModifiedAt = DateTime.UtcNow;
        unitOfWork.Propositions.Update(proposition);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("PropositionService.VoteAsync completed for PropositionId {Id}, VotesUp: {VotesUp}, VotesDown: {VotesDown}",
            id, proposition.VotesUp, proposition.VotesDown);

        var propositionWithDetails = await unitOfWork.Propositions.GetPropositionWithDetailsAsync(id);
        var dto = mapper.Map<PropositionDto>(propositionWithDetails);

        // Ajouter le vote de l'utilisateur au DTO
        var finalVote = await unitOfWork.PropositionVotes.GetByPropositionAndUserAsync(id, user.Id);
        dto.UserVoteType = finalVote != null ? ConvertVoteTypeToDto(finalVote.VoteType) : null;

        return dto;
    }

    /// <summary>
    /// Convertit un VoteType en sa représentation DTO (1 pour Up, -1 pour Down)
    /// </summary>
    private static int ConvertVoteTypeToDto(Bo.Models.VoteType voteType)
    {
        return voteType == Bo.Models.VoteType.Up ? 1 : -1;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PropositionDto>> GetAllPropositionsForAdminAsync(
        PaginationParams pagination,
        Dto.Proposition.PropositionFilterDto filter,
        string? userEmail = null)
    {
        logger.LogInformation("PropositionService.GetAllPropositionsForAdminAsync called with filter {Status}", filter.Status);

        // Calculate skip and take for pagination
        var skip = (pagination.PageNumber - 1) * pagination.PageSize;
        var take = pagination.PageSize;

        // Get propositions with filter
        var (propositions, totalCount) = await unitOfWork.Propositions.GetPagedWithFilterAsync(skip, take, filter.Status);

        // Map to DTOs
        var propositionDtos = mapper.Map<IEnumerable<PropositionDto>>(propositions).ToList();

        // If user email provided, enrich with user votes
        if (!string.IsNullOrEmpty(userEmail))
        {
            var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
            if (user != null)
            {
                // Optimisation: Récupérer tous les votes en une seule requête (évite N+1 queries)
                var propositionIds = propositionDtos.Select(p => p.Id).ToList();
                var userVotes = await unitOfWork.PropositionVotes.GetUserVotesForPropositionsAsync(user.Id, propositionIds);

                // Remplir le champ UserVoteType pour chaque proposition
                foreach (var dto in propositionDtos)
                {
                    var vote = userVotes.FirstOrDefault(v => v.PropositionId == dto.Id);
                    dto.UserVoteType = vote != null ? ConvertVoteTypeToDto(vote.VoteType) : null;
                }
            }
        }

        logger.LogInformation("PropositionService.GetAllPropositionsForAdminAsync returned {Count} propositions (total: {Total})",
            propositionDtos.Count, totalCount);

        return new PagedResult<PropositionDto>(
            propositionDtos,
            totalCount,
            pagination.PageNumber,
            pagination.PageSize
        );
    }

    /// <inheritdoc />
    public async Task<PropositionDto?> DeletePropositionAsAdminAsync(int id, string userEmail)
    {
        logger.LogInformation("PropositionService.DeletePropositionAsAdminAsync called for PropositionId {Id} by {Email}", id, userEmail);

        var proposition = await unitOfWork.Propositions.GetByIdAsync(id);
        if (proposition == null || proposition.IsDeleted)
        {
            logger.LogWarning("PropositionService.DeletePropositionAsAdminAsync - Proposition {Id} not found", id);
            return null;
        }

        // Get the user to verify authorization
        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("PropositionService.DeletePropositionAsAdminAsync - User not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        // Verify authorization: ESN member or admin
        bool isEsnMember = user.StudentType?.ToLower() == "esn_member";
        bool isAdmin = user.Role?.Name == UserRole.Admin;

        if (!isEsnMember && !isAdmin)
        {
            logger.LogWarning("PropositionService.DeletePropositionAsAdminAsync - User {Email} (ID: {UserId}) tried to delete Proposition {Id} without sufficient permissions",
                userEmail, user.Id, id);
            throw new UnauthorizedAccessException("You don't have permission to delete propositions as admin");
        }

        // Soft delete
        proposition.IsDeleted = true;
        proposition.DeletedAt = DateTime.UtcNow;

        unitOfWork.Propositions.Update(proposition);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("PropositionService.DeletePropositionAsAdminAsync completed for PropositionId {Id} by {Email}", id, userEmail);

        return mapper.Map<PropositionDto>(proposition);
    }
}
