using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickClinique.Models;

namespace QuickClinique.Services
{
    public class QueueAssignmentService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueAssignmentService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

        public QueueAssignmentService(
            IServiceProvider serviceProvider,
            ILogger<QueueAssignmentService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue Assignment Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await AssignQueueNumbersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while assigning queue numbers.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Queue Assignment Service is stopping.");
        }

        private async Task AssignQueueNumbersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                // First, check if the columns exist by trying a simple query
                // This will throw if columns don't exist, which we'll catch
                var testQuery = await context.Appointments
                    .Where(a => a.AppointmentId > 0)
                    .Select(a => new { a.AppointmentId, a.TimeSelected, a.CreatedAt })
                    .Take(1)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Columns don't exist yet, skip processing
                _logger.LogWarning("TimeSelected and CreatedAt columns not found. Please run the migration SQL script. Error: {Error}", ex.Message);
                return;
            }

            var now = DateTime.Now;
            // Check appointments where TimeSelected matches current time (within 1 minute window)
            // and appointment is confirmed but doesn't have a queue number yet
            var appointmentsToProcess = await context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a => a.AppointmentStatus == "Confirmed" &&
                           a.QueueNumber == 0 &&
                           a.TimeSelected <= now &&
                           a.TimeSelected >= now.AddMinutes(-1)) // Within the last minute
                .OrderBy(a => a.CreatedAt) // Process by creation time (first come first served)
                .ToListAsync();

            if (!appointmentsToProcess.Any())
            {
                return;
            }

            _logger.LogInformation($"Found {appointmentsToProcess.Count} appointment(s) to assign queue numbers.");

            // Group appointments by time slot (same date and start time)
            var appointmentsByTimeSlot = appointmentsToProcess
                .GroupBy(a => new { a.Schedule.Date, a.Schedule.StartTime })
                .ToList();

            foreach (var timeSlotGroup in appointmentsByTimeSlot)
            {
                var timeSlotDate = timeSlotGroup.Key.Date;
                var timeSlotStartTime = timeSlotGroup.Key.StartTime;
                
                // Get all appointments in this time slot that already have queue numbers
                // to determine the starting queue number for this slot
                var existingAppointmentsInSlot = await context.Appointments
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == timeSlotDate &&
                               a.Schedule.StartTime == timeSlotStartTime &&
                               a.QueueNumber > 0 &&
                               a.AppointmentStatus == "Confirmed")
                    .OrderByDescending(a => a.QueueNumber)
                    .FirstOrDefaultAsync();

                // Queue numbers reset per time slot, so start from 1 or continue from existing
                int nextQueueNumber = existingAppointmentsInSlot != null 
                    ? existingAppointmentsInSlot.QueueNumber + 1 
                    : 1;

                // Assign queue numbers to appointments in this time slot
                foreach (var appointment in timeSlotGroup.OrderBy(a => a.CreatedAt))
                {
                    appointment.QueueNumber = nextQueueNumber;
                    appointment.QueueStatus = "Waiting";
                    nextQueueNumber++;

                    // Calculate position in line (how many people are ahead in the same time slot)
                    var positionInLine = await context.Appointments
                        .Include(a => a.Schedule)
                        .Where(a => a.Schedule.Date == timeSlotDate &&
                                   a.Schedule.StartTime == timeSlotStartTime &&
                                   a.AppointmentStatus == "Confirmed" &&
                                   a.QueueNumber > 0 &&
                                   a.QueueNumber < appointment.QueueNumber)
                        .CountAsync() + 1;

                    // Count total appointments in this time slot
                    var totalInTimeSlot = await context.Appointments
                        .Include(a => a.Schedule)
                        .Where(a => a.Schedule.Date == timeSlotDate &&
                                   a.Schedule.StartTime == timeSlotStartTime &&
                                   a.AppointmentStatus == "Confirmed" &&
                                   a.QueueNumber > 0)
                        .CountAsync();

                    await context.SaveChangesAsync();

                    _logger.LogInformation($"Assigned queue number {appointment.QueueNumber} to appointment {appointment.AppointmentId} (Patient: {appointment.Patient?.FullName})");

                    // Send email notification
                    if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                    {
                        try
                        {
                            var appointmentDate = appointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                            var appointmentTime = appointment.Schedule != null
                                ? $"{appointment.Schedule.StartTime:h:mm tt} - {appointment.Schedule.EndTime:h:mm tt}"
                                : "N/A";

                            await emailService.SendQueueNumberAssignmentEmail(
                                appointment.Patient.Email,
                                appointment.Patient.FullName,
                                appointmentDate,
                                appointmentTime,
                                appointment.QueueNumber,
                                positionInLine
                            );

                            _logger.LogInformation($"Sent queue number assignment email to {appointment.Patient.Email}");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, $"Failed to send queue number assignment email to {appointment.Patient.Email}");
                        }
                    }
                }
            }
        }
    }
}

