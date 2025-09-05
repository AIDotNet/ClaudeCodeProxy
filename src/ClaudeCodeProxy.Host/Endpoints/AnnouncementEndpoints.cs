using ClaudeCodeProxy.Core;
using ClaudeCodeProxy.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaudeCodeProxy.Host.Endpoints;

public static class AnnouncementEndpoints
{
    public static void MapAnnouncementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/announcements")
            .WithTags("Announcements");

        // 获取当前可见的公告
        group.MapGet("/current", GetCurrentAnnouncements)
            .WithName("GetCurrentAnnouncements")
            .WithDescription("获取当前可见的公告")
            .Produces<List<AnnouncementDto>>()
            .Produces(401);

        // 获取所有公告（管理员）
        group.MapGet("/", GetAllAnnouncements)
            .WithName("GetAllAnnouncements")
            .WithDescription("获取所有公告（管理员）")
            .RequireAuthorization("AdminOnly")
            .Produces<List<AnnouncementDto>>()
            .Produces(401)
            .Produces(403);

        // 获取单个公告（管理员）
        group.MapGet("/{id:long}", GetAnnouncement)
            .WithName("GetAnnouncement")
            .WithDescription("获取单个公告（管理员）")
            .RequireAuthorization("AdminOnly")
            .Produces<AnnouncementDto>()
            .Produces(404)
            .Produces(401)
            .Produces(403);

        // 创建公告（管理员）
        group.MapPost("/", CreateAnnouncement)
            .WithName("CreateAnnouncement")
            .WithDescription("创建公告（管理员）")
            .RequireAuthorization("AdminOnly")
            .Produces<AnnouncementDto>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        // 更新公告（管理员）
        group.MapPut("/{id:long}", UpdateAnnouncement)
            .WithName("UpdateAnnouncement")
            .WithDescription("更新公告（管理员）")
            .RequireAuthorization("AdminOnly")
            .Produces<AnnouncementDto>()
            .Produces(404)
            .Produces(400)
            .Produces(401)
            .Produces(403);

        // 删除公告（管理员）
        group.MapDelete("/{id:long}", DeleteAnnouncement)
            .WithName("DeleteAnnouncement")
            .WithDescription("删除公告（管理员）")
            .RequireAuthorization("AdminOnly")
            .Produces(204)
            .Produces(404)
            .Produces(401)
            .Produces(403);

        // 切换公告可见性（管理员）
        group.MapPatch("/{id:long}/toggle", ToggleAnnouncementVisibility)
            .WithName("ToggleAnnouncementVisibility")
            .WithDescription("切换公告可见性（管理员）")
            .RequireAuthorization("AdminOnly")
            .Produces<AnnouncementDto>()
            .Produces(404)
            .Produces(401)
            .Produces(403);
    }

    private static async Task<IResult> GetCurrentAnnouncements(IContext context, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var announcements = await context.Announcements
            .Where(a => a.IsVisible)
            .Where(a => a.StartTime == null || a.StartTime <= now)
            .Where(a => a.EndTime == null || a.EndTime >= now)
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                IsVisible = a.IsVisible,
                BackgroundColor = a.BackgroundColor,
                TextColor = a.TextColor,
                Priority = a.Priority,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                CreatedAt = a.CreatedAt,
                ModifiedAt = a.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(announcements);
    }

    private static async Task<IResult> GetAllAnnouncements(IContext context, CancellationToken cancellationToken)
    {
        var announcements = await context.Announcements
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                IsVisible = a.IsVisible,
                BackgroundColor = a.BackgroundColor,
                TextColor = a.TextColor,
                Priority = a.Priority,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                CreatedAt = a.CreatedAt,
                ModifiedAt = a.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(announcements);
    }

    private static async Task<IResult> GetAnnouncement(long id, IContext context, CancellationToken cancellationToken)
    {
        var announcement = await context.Announcements
            .Where(a => a.Id == id)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                IsVisible = a.IsVisible,
                BackgroundColor = a.BackgroundColor,
                TextColor = a.TextColor,
                Priority = a.Priority,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                CreatedAt = a.CreatedAt,
                ModifiedAt = a.ModifiedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return announcement == null ? Results.NotFound() : Results.Ok(announcement);
    }

    private static async Task<IResult> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequest request,
        IContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest("标题不能为空");

        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest("内容不能为空");

        var announcement = new Announcement
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsVisible = request.IsVisible,
            BackgroundColor = request.BackgroundColor ?? "bg-blue-50",
            TextColor = request.TextColor ?? "text-blue-800",
            Priority = request.Priority,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        context.Announcements.Add(announcement);
        await context.SaveAsync(cancellationToken);

        var dto = new AnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Content = announcement.Content,
            IsVisible = announcement.IsVisible,
            BackgroundColor = announcement.BackgroundColor,
            TextColor = announcement.TextColor,
            Priority = announcement.Priority,
            StartTime = announcement.StartTime,
            EndTime = announcement.EndTime,
            CreatedAt = announcement.CreatedAt,
            ModifiedAt = announcement.ModifiedAt
        };

        return Results.Created($"/api/announcements/{announcement.Id}", dto);
    }

    private static async Task<IResult> UpdateAnnouncement(
        long id,
        [FromBody] UpdateAnnouncementRequest request,
        IContext context,
        CancellationToken cancellationToken)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (announcement == null)
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest("标题不能为空");

        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest("内容不能为空");

        announcement.Title = request.Title.Trim();
        announcement.Content = request.Content.Trim();
        announcement.IsVisible = request.IsVisible;
        announcement.BackgroundColor = request.BackgroundColor ?? "bg-blue-50";
        announcement.TextColor = request.TextColor ?? "text-blue-800";
        announcement.Priority = request.Priority;
        announcement.StartTime = request.StartTime;
        announcement.EndTime = request.EndTime;

        await context.SaveAsync(cancellationToken);

        var dto = new AnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Content = announcement.Content,
            IsVisible = announcement.IsVisible,
            BackgroundColor = announcement.BackgroundColor,
            TextColor = announcement.TextColor,
            Priority = announcement.Priority,
            StartTime = announcement.StartTime,
            EndTime = announcement.EndTime,
            CreatedAt = announcement.CreatedAt,
            ModifiedAt = announcement.ModifiedAt
        };

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteAnnouncement(long id, IContext context,
        CancellationToken cancellationToken)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (announcement == null)
            return Results.NotFound();

        context.Announcements.Remove(announcement);
        await context.SaveAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> ToggleAnnouncementVisibility(
        long id,
        IContext context,
        CancellationToken cancellationToken)
    {
        var announcement = await context.Announcements
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (announcement == null)
            return Results.NotFound();

        announcement.IsVisible = !announcement.IsVisible;
        await context.SaveAsync(cancellationToken);

        var dto = new AnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Content = announcement.Content,
            IsVisible = announcement.IsVisible,
            BackgroundColor = announcement.BackgroundColor,
            TextColor = announcement.TextColor,
            Priority = announcement.Priority,
            StartTime = announcement.StartTime,
            EndTime = announcement.EndTime,
            CreatedAt = announcement.CreatedAt,
            ModifiedAt = announcement.ModifiedAt
        };

        return Results.Ok(dto);
    }
}

public class AnnouncementDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public int Priority { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public int Priority { get; set; } = 0;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class UpdateAnnouncementRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public int Priority { get; set; } = 0;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}