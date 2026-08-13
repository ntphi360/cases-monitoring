using HoSoMonitoring.Core.Configurations;
using HoSoMonitoring.Core.Content;
using HoSoMonitoring.Core.Enums;
using HoSoMonitoring.Core.Models.Reminder;
using HoSoMonitoring.Core.SeedWorks;
using HoSoMonitoring.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace HoSoMonitoring.Data.Services;

public class ReminderService : IReminderService
{
    private readonly HoSoMonitoringContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IZaloNotificationService _zaloService;
    private readonly MonitoringOptions _monitoring;

    public ReminderService(
        HoSoMonitoringContext context,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IZaloNotificationService zaloService,
        MonitoringOptions monitoring)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _zaloService = zaloService;
        _monitoring = monitoring;
    }

    public async Task<SendReminderResultDto> SendAsync(
        SendReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var message = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ReminderValidationException(
                "Nội dung nhắc nhở không được để trống.");
        }

        var channels = ParseChannels(request.Channels);
        var caseEntity = await _context.Cases
            .AsNoTracking()
            .Include(item => item.Procedure)
            .Include(item => item.CurrentAssignee)
            .FirstOrDefaultAsync(
                item => item.Id == request.CaseId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ.");

        var assignee = caseEntity.CurrentAssignee
            ?? throw new ReminderValidationException(
                "Hồ sơ chưa có cán bộ xử lý để nhận nhắc nhở.");
        var response = new SendReminderResultDto();

        foreach (var channel in channels)
        {
            var channelResult = channel switch
            {
                ReminderChannel.System => SendSystem(caseEntity, assignee, message),
                ReminderChannel.Email => await SendEmailAsync(
                    caseEntity,
                    assignee,
                    message,
                    cancellationToken),
                ReminderChannel.Zalo => await _zaloService.SendAsync(
                    assignee.PhoneNumber ?? string.Empty,
                    message,
                    cancellationToken),
                _ => throw new ReminderValidationException(
                    "Kênh nhắc nhở không hợp lệ.")
            };

            response.Data[channel.ToString().ToLowerInvariant()] =
                new ReminderChannelResultDto
                {
                    Success = channelResult.Success,
                    Message = channelResult.Message
                };
            AddDelivery(caseEntity, assignee, channel, message, channelResult);
        }

        await _context.SaveChangesAsync(cancellationToken);
        response.Success = response.Data.Values.Any(item => item.Success);
        return response;
    }

    private ChannelSendResult SendSystem(
        Case caseEntity,
        User assignee,
        string message)
    {
        _unitOfWork.Notifications.Add(new Notification
        {
            CaseId = caseEntity.Id,
            UserId = assignee.Id,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.Now
        });

        return new ChannelSendResult
        {
            Success = true,
            Message = "Đã tạo thông báo trong hệ thống."
        };
    }

    private Task<ChannelSendResult> SendEmailAsync(
        Case caseEntity,
        User assignee,
        string message,
        CancellationToken cancellationToken)
    {
        var deadlineStatus = DeadlineStatusCalculator.Calculate(
            caseEntity,
            DateTime.Now,
            _monitoring.WarningThresholdDays);
        var body = string.Join(Environment.NewLine,
        [
            $"Mã hồ sơ: {caseEntity.ExternalCaseCode}",
            $"Tên thủ tục: {caseEntity.Procedure?.Name ?? "Không xác định"}",
            $"Người xử lý: {assignee.FullName}",
            $"Hạn xử lý: {caseEntity.Deadline:dd/MM/yyyy HH:mm}",
            $"Tình trạng thời hạn: {GetDeadlineStatusLabel(deadlineStatus)}",
            string.Empty,
            message
        ]);

        return _emailService.SendAsync(
            assignee.Email,
            $"[HoSoMonitoring] Nhắc nhở hồ sơ {caseEntity.ExternalCaseCode}",
            body,
            cancellationToken);
    }

    private void AddDelivery(
        Case caseEntity,
        User assignee,
        ReminderChannel channel,
        string message,
        ChannelSendResult result)
    {
        var recipient = channel switch
        {
            ReminderChannel.System => assignee.Id.ToString(),
            ReminderChannel.Email => assignee.Email,
            ReminderChannel.Zalo => assignee.PhoneNumber,
            _ => null
        };

        _context.ReminderDeliveries.Add(new ReminderDelivery
        {
            CaseId = caseEntity.Id,
            UserId = assignee.Id,
            Channel = channel,
            Recipient = recipient,
            Message = message,
            Status = result.Success
                ? ReminderDeliveryStatus.Succeeded
                : ReminderDeliveryStatus.Failed,
            SentAt = DateTime.Now,
            ErrorMessage = result.Success ? null : result.Message
        });
    }

    private static HashSet<ReminderChannel> ParseChannels(
        IEnumerable<string> requestedChannels)
    {
        var channels = new HashSet<ReminderChannel>();
        foreach (var value in requestedChannels)
        {
            if (!Enum.TryParse<ReminderChannel>(value, true, out var channel)
                || !Enum.IsDefined(channel))
            {
                throw new ReminderValidationException(
                    $"Kênh nhắc nhở '{value}' không hợp lệ.");
            }

            channels.Add(channel);
        }

        if (channels.Count == 0)
        {
            throw new ReminderValidationException(
                "Vui lòng chọn ít nhất một kênh gửi nhắc nhở.");
        }

        return channels;
    }

    private static string GetDeadlineStatusLabel(DeadlineStatus status) =>
        status switch
        {
            DeadlineStatus.OnTime => "Còn hạn",
            DeadlineStatus.NearDeadline => "Sắp hạn",
            DeadlineStatus.DueToday => "Đến hạn hôm nay",
            DeadlineStatus.Overdue => "Quá hạn",
            DeadlineStatus.CompletedOnTime => "Hoàn thành đúng hạn",
            DeadlineStatus.CompletedLate => "Hoàn thành trễ hạn",
            _ => "Không xác định"
        };
}
