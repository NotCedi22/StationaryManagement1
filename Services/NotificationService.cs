using StationaryManagement1.Data;
using StationaryManagement1.Models;

namespace StationaryManagement1.Services
{
    /// <summary>
    /// Central place to create notifications for an employee and optionally their superior.
    /// </summary>
    public class NotificationService
    {
        private readonly AppDBContext _context;

        public NotificationService(AppDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates notifications for the employee and, when provided, their superior.
        /// </summary>
        public async Task NotifyAsync(int employeeId, int? superiorId, int? relatedRequestId, string message)
        {
            var notifications = new List<Notification>
            {
                new Notification
                {
                    EmployeeId = employeeId,
                    RelatedRequestId = relatedRequestId,
                    Message = message,
                    CreatedAt = DateTime.UtcNow
                }
            };

            if (superiorId.HasValue && superiorId.Value != 0 && superiorId.Value != employeeId)
            {
                notifications.Add(new Notification
                {
                    EmployeeId = superiorId.Value,
                    RelatedRequestId = relatedRequestId,
                    Message = message,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}

