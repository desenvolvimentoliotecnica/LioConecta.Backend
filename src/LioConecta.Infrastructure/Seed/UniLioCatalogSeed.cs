using System.Text.Json;
using LioConecta.Domain.Entities;
using LioConecta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LioConecta.Infrastructure.Seed;

internal sealed record UniLioCourseModuleSeed(
    int SortOrder,
    string Title,
    string ContentType,
    string? ContentUrl,
    int DurationMinutes,
    string? ArticleHtml = null,
    string? QuizJson = null);

internal sealed record UniLioCourseSeed(
    string SeedKey,
    string Title,
    string Description,
    string ContentType,
    int DurationMinutes,
    bool IsMandatory,
    string Area,
    string Department,
    decimal Rating,
    string InstructorName,
    string? ExternalUrl,
    string? Provider,
    string? ThumbnailUrl,
    string Status,
    IReadOnlyList<string>? SkillSeedKeys,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<UniLioCourseModuleSeed> Modules);

internal sealed record UniLioPathSeed(
    string SeedKey,
    string Title,
    string Description,
    IReadOnlyList<string> CourseSeedKeys);

internal sealed record UniLioSkillSeed(
    string SeedKey,
    string Name,
    string Category,
    string Description);

internal sealed record UniLioAssessmentQuestionSeed(
    string Id,
    string Text,
    IReadOnlyList<string> Options,
    string CorrectAnswer);

internal sealed record UniLioAssessmentSeed(
    string CourseSeedKey,
    string Title,
    int PassingScore,
    IReadOnlyList<UniLioAssessmentQuestionSeed> Questions);

internal sealed record UniLioEventSeed(
    string SeedKey,
    string Title,
    string EventType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? InstructorPersonSeedKey,
    int MaxAttendees,
    string? MeetingUrl);

internal sealed record UniLioCommunityPostSeed(
    string SeedKey,
    string AuthorPersonSeedKey,
    string? CourseSeedKey,
    string Body,
    int LikesCount);

internal sealed record UniLioIntegrationLinkSeed(
    string SeedKey,
    string SourceType,
    string SourceKey,
    string CourseSeedKey);

internal sealed record UniLioEnrollmentSeed(
    string SeedKey,
    string PersonSeedKey,
    string CourseSeedKey,
    string Status,
    int ProgressPct,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? DueDate,
    bool IssueCertificate,
    string? CertificateCode);

internal sealed record UniLioPersonSkillSeed(
    string PersonSeedKey,
    string SkillSeedKey,
    int Level);

internal sealed record UniLioCatalogSeedFile(
    IReadOnlyList<UniLioCourseSeed> Courses,
    IReadOnlyList<UniLioPathSeed> Paths,
    IReadOnlyList<UniLioSkillSeed> Skills,
    IReadOnlyList<UniLioAssessmentSeed> Assessments,
    IReadOnlyList<UniLioEventSeed> Events,
    IReadOnlyList<UniLioCommunityPostSeed> CommunityPosts,
    IReadOnlyList<UniLioIntegrationLinkSeed> IntegrationLinks,
    IReadOnlyList<UniLioEnrollmentSeed> Enrollments,
    IReadOnlyList<UniLioPersonSkillSeed> PersonSkills);

internal sealed record UniLioContentOverlayFile(
    int Version,
    IReadOnlyList<UniLioCourseSeed> Courses);

internal static class UniLioCatalogSeed
{
    // Stable GUID maps — mirrored in SeedIds.cs (UniLio section) for idempotent seeding.
    private static readonly IReadOnlyDictionary<string, Guid> CourseIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["ext-liofilizacao-ufv"] = Guid.Parse("22222222-2222-2222-2222-222222220001"),
        ["ext-eng-alimentos-podcast"] = Guid.Parse("22222222-2222-2222-2222-222222220002"),
        ["ext-custo-nao-qualidade"] = Guid.Parse("22222222-2222-2222-2222-222222220003"),
        ["ext-stp-alimenticia"] = Guid.Parse("22222222-2222-2222-2222-222222220004"),
        ["ext-appcc-crq"] = Guid.Parse("22222222-2222-2222-2222-222222220005"),
        ["ext-fssc-22000"] = Guid.Parse("22222222-2222-2222-2222-222222220006"),
        ["ext-lean-manufacturing"] = Guid.Parse("22222222-2222-2222-2222-222222220007"),
        ["ext-lean-six-sigma"] = Guid.Parse("22222222-2222-2222-2222-222222220008"),
        ["ext-instrumentacao"] = Guid.Parse("22222222-2222-2222-2222-222222220009"),
        ["ext-tpm-manutencao"] = Guid.Parse("22222222-2222-2222-2222-22222222000a"),
        ["ext-nr35-altura"] = Guid.Parse("22222222-2222-2222-2222-22222222000b"),
        ["ext-gestao-pessoas-rh"] = Guid.Parse("22222222-2222-2222-2222-22222222000c"),
        ["ext-lideranca-negociar"] = Guid.Parse("22222222-2222-2222-2222-22222222000d"),
        ["ext-power-bi-iniciantes"] = Guid.Parse("22222222-2222-2222-2222-22222222000e"),
        ["ext-power-bi-curso"] = Guid.Parse("22222222-2222-2222-2222-22222222000f"),
        ["ext-excel-tabela-dinamica"] = Guid.Parse("22222222-2222-2222-2222-222222220010"),
        ["ext-excel-entrevista"] = Guid.Parse("22222222-2222-2222-2222-222222220011"),
        ["ext-lgpd-basico"] = Guid.Parse("22222222-2222-2222-2222-222222220012"),
        ["ext-lgpd-pme"] = Guid.Parse("22222222-2222-2222-2222-222222220013"),
        ["ext-nr35-resumo"] = Guid.Parse("22222222-2222-2222-2222-222222220014"),
        ["ext-bpf-usp"] = Guid.Parse("22222222-2222-2222-2222-222222220015"),
        ["ext-appcc-usp"] = Guid.Parse("22222222-2222-2222-2222-222222220016"),
        ["codigo-conduta"] = Guid.Parse("22222222-2222-2222-2222-222222220017"),
        ["seguranca-informacao"] = Guid.Parse("22222222-2222-2222-2222-222222220018"),
        ["feedback-efetivo"] = Guid.Parse("22222222-2222-2222-2222-222222220019"),
        ["onboarding-liotecnica"] = Guid.Parse("22222222-2222-2222-2222-22222222001a"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> PathIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["path-onboarding-liotecnica"] = Guid.Parse("33333333-3333-3333-3333-333333330001"),
        ["path-processos-liofilizacao"] = Guid.Parse("33333333-3333-3333-3333-333333330002"),
        ["path-seguranca-alimentar"] = Guid.Parse("33333333-3333-3333-3333-333333330003"),
        ["path-excelencia-operacional"] = Guid.Parse("33333333-3333-3333-3333-333333330004"),
        ["path-lideranca-essentials"] = Guid.Parse("33333333-3333-3333-3333-333333330005"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> SkillIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["liofilizacao"] = Guid.Parse("44444444-4444-4444-4444-444444440001"),
        ["qualidade-alimentar"] = Guid.Parse("44444444-4444-4444-4444-444444440002"),
        ["lean-manufacturing"] = Guid.Parse("44444444-4444-4444-4444-444444440003"),
        ["lideranca"] = Guid.Parse("44444444-4444-4444-4444-444444440004"),
        ["power-bi"] = Guid.Parse("44444444-4444-4444-4444-444444440005"),
        ["excel"] = Guid.Parse("44444444-4444-4444-4444-444444440006"),
        ["lgpd"] = Guid.Parse("44444444-4444-4444-4444-444444440007"),
        ["seguranca-trabalho"] = Guid.Parse("44444444-4444-4444-4444-444444440008"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> AssessmentIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["codigo-conduta"] = Guid.Parse("55555555-5555-5555-5555-555555550001"),
        ["ext-appcc-crq"] = Guid.Parse("55555555-5555-5555-5555-555555550002"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> EventIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["event-webinar-appcc-pratica"] = Guid.Parse("66666666-6666-6666-6666-666666660001"),
        ["event-workshop-lean-planta"] = Guid.Parse("66666666-6666-6666-6666-666666660002"),
        ["event-webinar-lgpd-rh"] = Guid.Parse("66666666-6666-6666-6666-666666660003"),
        ["event-workshop-power-bi"] = Guid.Parse("66666666-6666-6666-6666-666666660004"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> CommunityPostIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["post-appcc-dicas"] = Guid.Parse("77777777-7777-7777-7777-777777770001"),
        ["post-liofilizacao-ufv"] = Guid.Parse("77777777-7777-7777-7777-777777770002"),
        ["post-lean-manufacturing"] = Guid.Parse("77777777-7777-7777-7777-777777770003"),
        ["post-lgpd-colaboradores"] = Guid.Parse("77777777-7777-7777-7777-777777770004"),
        ["post-power-bi-iniciantes"] = Guid.Parse("77777777-7777-7777-7777-777777770005"),
        ["post-feedback-efetivo"] = Guid.Parse("77777777-7777-7777-7777-777777770006"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> IntegrationLinkIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["link-biblioteca-guia-cultura"] = Guid.Parse("88888888-8888-8888-8888-888888880001"),
        ["link-beneficio-educacao"] = Guid.Parse("88888888-8888-8888-8888-888888880002"),
        ["link-loop-aprendizado"] = Guid.Parse("88888888-8888-8888-8888-888888880003"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> EnrollmentIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["enrollment-julio-onboarding"] = Guid.Parse("99999999-9999-9999-9999-999999990001"),
        ["enrollment-carlos-lean"] = Guid.Parse("99999999-9999-9999-9999-999999990002"),
        ["enrollment-maria-appcc"] = Guid.Parse("99999999-9999-9999-9999-999999990003"),
        ["enrollment-ricardo-nr35"] = Guid.Parse("99999999-9999-9999-9999-999999990004"),
        ["enrollment-julia-powerbi"] = Guid.Parse("99999999-9999-9999-9999-999999990005"),
    };

    private static readonly IReadOnlyDictionary<string, Guid> PersonSkillIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["julio:lideranca"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa21"),
        ["julio:lean-manufacturing"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa22"),
        ["julio:liofilizacao"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa23"),
        ["carlos:lean-manufacturing"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa24"),
        ["carlos:qualidade-alimentar"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa25"),
        ["carlos:excel"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa26"),
        ["maria:qualidade-alimentar"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa27"),
        ["maria:lgpd"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa28"),
        ["maria:lideranca"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa29"),
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static UniLioCatalogSeedFile LoadFromJson()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Seed", "Data", "unilio-catalog.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Seed", "Data", "unilio-catalog.json")),
        };

        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("UniLio catalog seed file not found.", candidates[0]);

        var json = File.ReadAllText(path);
        var payload = JsonSerializer.Deserialize<UniLioCatalogSeedFile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize UniLio catalog seed file.");

        var overlay = TryLoadContentOverlay();
        if (overlay is not null)
        {
            payload = MergeContentOverlay(payload, overlay);
        }

        return payload;
    }

    private static UniLioContentOverlayFile? TryLoadContentOverlay()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Seed", "Data", "unilio-content-overlay.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Seed", "Data", "unilio-content-overlay.json")),
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<UniLioContentOverlayFile>(File.ReadAllText(path), JsonOptions);
    }

    private static UniLioCatalogSeedFile MergeContentOverlay(UniLioCatalogSeedFile payload, UniLioContentOverlayFile overlay)
    {
        var index = payload.Courses
            .Select((course, i) => (course, i))
            .ToDictionary(x => x.course.SeedKey, x => x.i, StringComparer.Ordinal);

        var courses = payload.Courses.ToList();
        foreach (var patch in overlay.Courses)
        {
            if (!index.TryGetValue(patch.SeedKey, out var courseIndex))
            {
                continue;
            }

            var existing = courses[courseIndex];
            courses[courseIndex] = existing with
            {
                Title = string.IsNullOrWhiteSpace(patch.Title) ? existing.Title : patch.Title,
                Description = string.IsNullOrWhiteSpace(patch.Description) ? existing.Description : patch.Description,
                DurationMinutes = patch.DurationMinutes > 0 ? patch.DurationMinutes : existing.DurationMinutes,
                ThumbnailUrl = patch.ThumbnailUrl ?? existing.ThumbnailUrl,
                Modules = patch.Modules.Count > 0 ? patch.Modules : existing.Modules,
            };
        }

        return payload with { Courses = courses };
    }

    public static async Task<int> RefreshCourseContentAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var payload = LoadFromJson();
        var now = DateTimeOffset.UtcNow;
        var updatedCourses = 0;

        foreach (var courseSeed in payload.Courses)
        {
            var course = await db.UniLioCourses
                .Include(c => c.Modules)
                .FirstOrDefaultAsync(c => c.SeedKey == courseSeed.SeedKey, cancellationToken);

            if (course is null)
            {
                continue;
            }

            var changed = false;

            if (courseSeed.ThumbnailUrl is not null && course.ThumbnailUrl != courseSeed.ThumbnailUrl)
            {
                course.ThumbnailUrl = courseSeed.ThumbnailUrl;
                changed = true;
            }

            if (!string.Equals(course.Description, courseSeed.Description, StringComparison.Ordinal))
            {
                course.Description = courseSeed.Description;
                course.DurationMinutes = courseSeed.DurationMinutes;
                changed = true;
            }

            foreach (var moduleSeed in courseSeed.Modules.OrderBy(m => m.SortOrder))
            {
                var moduleChanged = false;
                var module = course.Modules.FirstOrDefault(m => m.SortOrder == moduleSeed.SortOrder);
                if (module is null)
                {
                    db.UniLioCourseModules.Add(new UniLioCourseModule
                    {
                        Id = ResolveModuleId(courseSeed.SeedKey, moduleSeed.SortOrder),
                        CourseId = course.Id,
                        SortOrder = moduleSeed.SortOrder,
                        Title = moduleSeed.Title,
                        ContentType = moduleSeed.ContentType,
                        ContentUrl = moduleSeed.ContentUrl,
                        DurationMinutes = moduleSeed.DurationMinutes,
                        ArticleHtml = moduleSeed.ArticleHtml,
                        QuizJson = moduleSeed.QuizJson,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                    changed = true;
                    continue;
                }

                if (module.Title != moduleSeed.Title)
                {
                    module.Title = moduleSeed.Title;
                    moduleChanged = true;
                }

                if (module.ContentType != moduleSeed.ContentType)
                {
                    module.ContentType = moduleSeed.ContentType;
                    moduleChanged = true;
                }

                if (module.ContentUrl != moduleSeed.ContentUrl)
                {
                    module.ContentUrl = moduleSeed.ContentUrl;
                    moduleChanged = true;
                }

                if (module.DurationMinutes != moduleSeed.DurationMinutes)
                {
                    module.DurationMinutes = moduleSeed.DurationMinutes;
                    moduleChanged = true;
                }

                if (module.ArticleHtml != moduleSeed.ArticleHtml)
                {
                    module.ArticleHtml = moduleSeed.ArticleHtml;
                    moduleChanged = true;
                }

                if (module.QuizJson != moduleSeed.QuizJson)
                {
                    module.QuizJson = moduleSeed.QuizJson;
                    moduleChanged = true;
                }

                if (moduleChanged)
                {
                    module.UpdatedAt = now;
                    changed = true;
                }
            }

            if (changed)
            {
                course.UpdatedAt = now;
                updatedCourses++;
            }
        }

        if (updatedCourses > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return updatedCourses;
    }

    public static Guid ResolveCourseId(string seedKey) =>
        CourseIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio course seed key: {seedKey}");

    public static Guid ResolvePathId(string seedKey) =>
        PathIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio path seed key: {seedKey}");

    public static Guid ResolveSkillId(string seedKey) =>
        SkillIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio skill seed key: {seedKey}");

    public static Guid ResolveAssessmentId(string courseSeedKey) =>
        AssessmentIds.TryGetValue(courseSeedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio assessment course seed key: {courseSeedKey}");

    public static Guid ResolveEventId(string seedKey) =>
        EventIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio event seed key: {seedKey}");

    public static Guid ResolveCommunityPostId(string seedKey) =>
        CommunityPostIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio community post seed key: {seedKey}");

    public static Guid ResolveIntegrationLinkId(string seedKey) =>
        IntegrationLinkIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio integration link seed key: {seedKey}");

    public static Guid ResolveEnrollmentId(string seedKey) =>
        EnrollmentIds.TryGetValue(seedKey, out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio enrollment seed key: {seedKey}");

    public static Guid ResolvePersonSkillId(string personSeedKey, string skillSeedKey) =>
        PersonSkillIds.TryGetValue($"{personSeedKey}:{skillSeedKey}", out var id)
            ? id
            : throw new KeyNotFoundException($"Unknown UniLio person skill: {personSeedKey}/{skillSeedKey}");

    public static Guid ResolveModuleId(string courseSeedKey, int sortOrder)
    {
        var suffix = $"{ResolveCourseNumericSuffix(courseSeedKey):x4}{sortOrder:x4}0000";
        return Guid.Parse($"22222222-2222-2222-2222-{suffix}");
    }

    public static Guid ResolvePersonId(string personSeedKey) => personSeedKey switch
    {
        "julio" => SeedIds.JulioSchwartzman,
        "carlos" => SeedIds.CarlosMendes,
        "maria" => SeedIds.MariaSilva,
        "ricardo" => SeedIds.RicardoSouza,
        "julia" => SeedIds.JuliaSantos,
        _ => throw new KeyNotFoundException($"Unknown person seed key: {personSeedKey}"),
    };

    public static UniLioCourse ToCourseEntity(UniLioCourseSeed seed, DateTimeOffset seedTime)
    {
        var courseId = ResolveCourseId(seed.SeedKey);
        return new UniLioCourse
        {
            Id = courseId,
            SeedKey = seed.SeedKey,
            Title = seed.Title,
            Description = seed.Description,
            ContentType = seed.ContentType,
            DurationMinutes = seed.DurationMinutes,
            IsMandatory = seed.IsMandatory,
            Area = seed.Area,
            Department = seed.Department,
            Rating = seed.Rating,
            InstructorName = seed.InstructorName,
            ExternalUrl = seed.ExternalUrl,
            Provider = seed.Provider,
            ThumbnailUrl = seed.ThumbnailUrl,
            Status = seed.Status,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    public static IEnumerable<UniLioCourseModule> ToModuleEntities(
        UniLioCourseSeed seed,
        DateTimeOffset seedTime)
    {
        var courseId = ResolveCourseId(seed.SeedKey);
        foreach (var module in seed.Modules.OrderBy(m => m.SortOrder))
        {
            yield return new UniLioCourseModule
            {
                Id = ResolveModuleId(seed.SeedKey, module.SortOrder),
                CourseId = courseId,
                SortOrder = module.SortOrder,
                Title = module.Title,
                ContentType = module.ContentType,
                ContentUrl = module.ContentUrl,
                DurationMinutes = module.DurationMinutes,
                ArticleHtml = module.ArticleHtml,
                QuizJson = module.QuizJson,
                CreatedAt = seedTime,
                UpdatedAt = seedTime,
            };
        }
    }

    public static UniLioLearningPath ToPathEntity(UniLioPathSeed seed, DateTimeOffset seedTime) =>
        new()
        {
            Id = ResolvePathId(seed.SeedKey),
            SeedKey = seed.SeedKey,
            Title = seed.Title,
            Description = seed.Description,
            IsActive = true,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };

    public static IEnumerable<UniLioPathCourse> ToPathCourseEntities(
        UniLioPathSeed seed,
        DateTimeOffset seedTime)
    {
        var pathId = ResolvePathId(seed.SeedKey);
        var sortOrder = 0;
        foreach (var courseSeedKey in seed.CourseSeedKeys)
        {
            sortOrder++;
            var pathCourseSuffix = $"{ResolvePathNumericSuffix(seed.SeedKey):x4}{sortOrder:x4}0000";
            yield return new UniLioPathCourse
            {
                Id = Guid.Parse($"33333333-3333-3333-3333-{pathCourseSuffix}"),
                PathId = pathId,
                CourseId = ResolveCourseId(courseSeedKey),
                SortOrder = sortOrder,
                CreatedAt = seedTime,
                UpdatedAt = seedTime,
            };
        }
    }

    public static UniLioSkill ToSkillEntity(UniLioSkillSeed seed, DateTimeOffset seedTime) =>
        new()
        {
            Id = ResolveSkillId(seed.SeedKey),
            SeedKey = seed.SeedKey,
            Name = seed.Name,
            Category = seed.Category,
            Description = seed.Description,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };

    public static IEnumerable<UniLioCourseSkill> ToCourseSkillEntities(
        UniLioCourseSeed seed,
        DateTimeOffset seedTime)
    {
        if (seed.SkillSeedKeys is not { Count: > 0 })
        {
            yield break;
        }

        var courseId = ResolveCourseId(seed.SeedKey);
        var index = 0;
        foreach (var skillSeedKey in seed.SkillSeedKeys.Distinct())
        {
            index++;
            var courseSkillSuffix = $"{ResolveCourseNumericSuffix(seed.SeedKey):x4}{index:x4}0000";
            yield return new UniLioCourseSkill
            {
                Id = Guid.Parse($"44444444-4444-4444-4444-{courseSkillSuffix}"),
                CourseId = courseId,
                SkillId = ResolveSkillId(skillSeedKey),
                CreatedAt = seedTime,
                UpdatedAt = seedTime,
            };
        }
    }

    public static UniLioAssessment ToAssessmentEntity(
        UniLioAssessmentSeed seed,
        DateTimeOffset seedTime) =>
        new()
        {
            Id = ResolveAssessmentId(seed.CourseSeedKey),
            CourseId = ResolveCourseId(seed.CourseSeedKey),
            Title = seed.Title,
            PassingScore = seed.PassingScore,
            QuestionsJson = SerializeQuestions(seed.Questions),
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };

    public static UniLioEvent ToEventEntity(UniLioEventSeed seed, DateTimeOffset seedTime)
    {
        Guid? instructorId = string.IsNullOrWhiteSpace(seed.InstructorPersonSeedKey)
            ? null
            : ResolvePersonId(seed.InstructorPersonSeedKey);

        return new UniLioEvent
        {
            Id = ResolveEventId(seed.SeedKey),
            Title = seed.Title,
            EventType = seed.EventType,
            StartsAt = seed.StartsAt.ToUniversalTime(),
            EndsAt = seed.EndsAt.ToUniversalTime(),
            InstructorPersonId = instructorId,
            MaxAttendees = seed.MaxAttendees,
            MeetingUrl = seed.MeetingUrl,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    public static UniLioCommunityPost ToCommunityPostEntity(
        UniLioCommunityPostSeed seed,
        DateTimeOffset seedTime)
    {
        Guid? courseId = string.IsNullOrWhiteSpace(seed.CourseSeedKey)
            ? null
            : ResolveCourseId(seed.CourseSeedKey);

        return new UniLioCommunityPost
        {
            Id = ResolveCommunityPostId(seed.SeedKey),
            AuthorPersonId = ResolvePersonId(seed.AuthorPersonSeedKey),
            CourseId = courseId,
            Body = seed.Body,
            LikesCount = seed.LikesCount,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    public static UniLioIntegrationLink ToIntegrationLinkEntity(
        UniLioIntegrationLinkSeed seed,
        DateTimeOffset seedTime) =>
        new()
        {
            Id = ResolveIntegrationLinkId(seed.SeedKey),
            SourceType = seed.SourceType,
            SourceKey = seed.SourceKey,
            CourseId = ResolveCourseId(seed.CourseSeedKey),
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };

    public static UniLioEnrollment ToEnrollmentEntity(
        UniLioEnrollmentSeed seed,
        DateTimeOffset seedTime) =>
        new()
        {
            Id = ResolveEnrollmentId(seed.SeedKey),
            PersonId = ResolvePersonId(seed.PersonSeedKey),
            CourseId = ResolveCourseId(seed.CourseSeedKey),
            Status = seed.Status,
            ProgressPct = seed.ProgressPct,
            StartedAt = seed.StartedAt?.ToUniversalTime(),
            CompletedAt = seed.CompletedAt?.ToUniversalTime(),
            DueDate = seed.DueDate?.ToUniversalTime(),
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };

    public static UniLioCertificate? ToCertificateEntity(
        UniLioEnrollmentSeed seed,
        DateTimeOffset seedTime)
    {
        if (!seed.IssueCertificate || string.IsNullOrWhiteSpace(seed.CertificateCode))
        {
            return null;
        }

        return new UniLioCertificate
        {
            Id = Guid.Parse($"55555555-5555-5555-5555-{ResolveEnrollmentNumericSuffix(seed.SeedKey):x12}"),
            PersonId = ResolvePersonId(seed.PersonSeedKey),
            CourseId = ResolveCourseId(seed.CourseSeedKey),
            CertificateCode = seed.CertificateCode,
            IssuedAt = (seed.CompletedAt ?? seedTime).ToUniversalTime(),
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };
    }

    public static UniLioPersonSkill ToPersonSkillEntity(
        UniLioPersonSkillSeed seed,
        DateTimeOffset seedTime) =>
        new()
        {
            Id = ResolvePersonSkillId(seed.PersonSeedKey, seed.SkillSeedKey),
            PersonId = ResolvePersonId(seed.PersonSeedKey),
            SkillId = ResolveSkillId(seed.SkillSeedKey),
            Level = seed.Level,
            CreatedAt = seedTime,
            UpdatedAt = seedTime,
        };

    public static string SerializeQuestions(IReadOnlyList<UniLioAssessmentQuestionSeed> questions) =>
        JsonSerializer.Serialize(questions, JsonOptions);

    private static int ResolveCourseNumericSuffix(string seedKey)
    {
        var lastPart = ResolveCourseId(seedKey).ToString().Split('-')[4];
        return Convert.ToInt32(lastPart[^4..], 16);
    }

    private static int ResolvePathNumericSuffix(string seedKey)
    {
        var lastPart = ResolvePathId(seedKey).ToString().Split('-')[4];
        return Convert.ToInt32(lastPart[^4..], 16);
    }

    private static int ResolveEnrollmentNumericSuffix(string seedKey) =>
        EnrollmentIds.TryGetValue(seedKey, out var id)
            ? BitConverter.ToInt32(id.ToByteArray()[8..12], 0) & 0xFFFFFF
            : throw new KeyNotFoundException($"Unknown UniLio enrollment seed key: {seedKey}");
}
