using LioConecta.Application.DTOs;
using LioConecta.Application.Interfaces.Repositories;
using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Mapping;
using LioConecta.Domain.Entities;
using LioConecta.Domain.Enums;
namespace LioConecta.Application.Services;
public sealed class FeedbackService(IFeedbackRepository repository, ICurrentUserService currentUserService) : IFeedbackService
{
 public async Task<FeedbackSubmissionDto> CreateAsync(CreateFeedbackRequest request, CancellationToken ct=default) { if(string.IsNullOrWhiteSpace(request.Subject)||string.IsNullOrWhiteSpace(request.Message)) throw new ArgumentException("Assunto e mensagem s?o obrigat?rios."); var authorId=await currentUserService.GetPersonIdAsync(ct); var now=DateTimeOffset.UtcNow; var item=new FeedbackSubmission { Id=Guid.NewGuid(), AuthorId=request.IsAnonymous?null:authorId, IsAnonymous=request.IsAnonymous, Category=request.Category, Status=FeedbackStatus.Received, Subject=request.Subject.Trim(), Message=request.Message.Trim(), CreatedAt=now, UpdatedAt=now }; await repository.AddAsync(item,ct); return ToDto(item); }
 public async Task<IReadOnlyList<FeedbackSubmissionDto>> ListAsync(FeedbackStatus? status, CancellationToken ct=default) => (await repository.ListAsync(status,ct)).Select(ToDto).ToList();
 public async Task<FeedbackSubmissionDto> UpdateAsync(Guid id, UpdateFeedbackRequest request, CancellationToken ct=default) { var item=await repository.GetByIdAsync(id,ct)??throw new KeyNotFoundException("Feedback n?o encontrado."); item.Status=request.Status; item.ResponseText=string.IsNullOrWhiteSpace(request.ResponseText)?null:request.ResponseText.Trim(); item.AssigneeId=request.AssigneeId; item.RespondedAt=item.ResponseText is null?null:DateTimeOffset.UtcNow; item.UpdatedAt=DateTimeOffset.UtcNow; await repository.SaveChangesAsync(ct); return ToDto(item); }
 private static FeedbackSubmissionDto ToDto(FeedbackSubmission f) => new(f.Id,f.Category,f.Status,f.Subject,f.Message,f.IsAnonymous,f.ResponseText,f.AssigneeId,f.IsAnonymous||f.Author is null?null:PersonMapper.ToSummary(f.Author),f.CreatedAt,f.RespondedAt);
}
