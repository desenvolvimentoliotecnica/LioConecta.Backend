using LioConecta.Application.Interfaces.Services;
using LioConecta.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LioConecta.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IFeedService, FeedService>();
        services.AddScoped<IComunicadoService, ComunicadoService>();
        services.AddScoped<IWikiService, WikiService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ILoopService, LoopService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IMoodCheckService, MoodCheckService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<INewHireAnnouncementService, NewHireAnnouncementService>();
        services.AddScoped<IAppSettingService, AppSettingService>();
        services.AddScoped<IPayslipService, PayslipService>();
        services.AddScoped<IBenefitService, BenefitService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IHourBankService, HourBankService>();
        services.AddScoped<LeaveNotifyRecipientResolver>();
        services.AddScoped<ILeaveEmailNotifier, LeaveEmailNotifier>();
        services.AddScoped<IPontoAdjustmentService, PontoAdjustmentService>();
        services.AddScoped<PontoNotifyRecipientResolver>();
        services.AddScoped<UniLioApprovalRecipientResolver>();
        services.AddScoped<IUniLioEmailNotifier, UniLioEmailNotifier>();
        services.AddScoped<IHelpDeskService, HelpDeskService>();
        services.AddScoped<IPlannerService, PlannerService>();
        services.AddScoped<IPollClosureService, PollClosureService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IObservabilityIngestionService, ObservabilityIngestionService>();
        services.AddScoped<IObservabilityRetentionService, ObservabilityRetentionService>();
        services.AddScoped<IObservabilityQueryService, ObservabilityQueryService>();

        services.AddScoped<TimesheetAggregationService>();
        services.AddScoped<TimesheetMergeService>();
        services.AddScoped<IPontoService, PontoService>();
        services.AddScoped<ITimesheetSyncService, TimesheetSyncService>();
        services.AddScoped<IPayslipSyncService, PayslipSyncService>();
        services.AddScoped<ILeaveSyncService, LeaveSyncService>();
        services.AddScoped<PayslipPdfBuilder>();

        return services;
    }
}
