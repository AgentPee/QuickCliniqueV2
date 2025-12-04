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

                // Check every minute for appointments within their time slots
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Queue Assignment Service is stopping.");
        }

        private async Task AssignQueueNumbersAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);
            
            // Check appointments where current time is within the selected time slot
            // (between StartTime and EndTime) and appointment is confirmed but doesn't have a queue number yet
            List<Appointment> appointmentsToProcess;
            bool createdAtColumnExists = true; // Assume it exists, will be set to false if query fails
            try
            {
                appointmentsToProcess = await context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.AppointmentStatus == "Confirmed" &&
                               a.QueueNumber == 0 &&
                               a.Schedule.Date == today &&
                               a.Schedule.StartTime <= currentTime &&
                               a.Schedule.EndTime > currentTime) // Current time is within the time slot
                    .OrderBy(a => a.CreatedAt) // Process by creation time (first come first served)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // If the query fails (e.g., CreatedAt column issue), log and skip
                _logger.LogWarning("Failed to query appointments with CreatedAt. The column may not be available yet. Error: {Error}", ex.Message);
                createdAtColumnExists = false;
                return;
            }

            if (!appointmentsToProcess.Any())
            {
                return;
            }

            _logger.LogInformation($"Found {appointmentsToProcess.Count} appointment(s) to assign queue numbers.");

            // Group appointments by date only (queue numbers reset per day, not per time slot)
            var appointmentsByDate = appointmentsToProcess
                .GroupBy(a => a.Schedule.Date)
                .ToList();

            foreach (var dateGroup in appointmentsByDate)
            {
                var appointmentDate = dateGroup.Key;
                
                // Get all appointments for this date that already have queue numbers
                // to determine the starting queue number for this day
                var existingAppointmentsOnDate = await context.Appointments
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == appointmentDate &&
                               a.QueueNumber > 0 &&
                               a.AppointmentStatus == "Confirmed")
                    .OrderByDescending(a => a.QueueNumber)
                    .FirstOrDefaultAsync();

                // Queue numbers reset per day, so start from 1 or continue from existing
                int nextQueueNumber = existingAppointmentsOnDate != null 
                    ? existingAppointmentsOnDate.QueueNumber + 1 
                    : 1;

                // Assign queue numbers to appointments for this date
                // Order by CreatedAt if available, otherwise fall back to AppointmentId
                var orderedAppointments = createdAtColumnExists
                    ? dateGroup.OrderBy(a => a.CreatedAt)
                    : dateGroup.OrderBy(a => a.AppointmentId);
                
                foreach (var appointment in orderedAppointments)
                {
                    appointment.QueueNumber = nextQueueNumber;
                    appointment.QueueStatus = "Waiting";
                    nextQueueNumber++;

                    // Calculate position in line (how many people are ahead for the same date)
                    var positionInLine = await context.Appointments
                        .Include(a => a.Schedule)
                        .Where(a => a.Schedule.Date == appointmentDate &&
                                   a.AppointmentStatus == "Confirmed" &&
                                   a.QueueNumber > 0 &&
                                   a.QueueNumber < appointment.QueueNumber)
                        .CountAsync() + 1;

                    await context.SaveChangesAsync();

                    _logger.LogInformation($"Assigned queue number {appointment.QueueNumber} to appointment {appointment.AppointmentId} (Patient: {appointment.Patient?.FullName})");

                    // Send email notification
                    if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                    {
                        try
                        {
                            var appointmentDateStr = appointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                            var appointmentTime = appointment.Schedule != null
                                ? $"{appointment.Schedule.StartTime:h:mm tt} - {appointment.Schedule.EndTime:h:mm tt}"
                                : "N/A";

                            var patientEmail = appointment.Patient.Email;
                            var patientName = appointment.Patient.FullName ?? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}";

                            _logger.LogInformation($"Sending queue number assignment email to {patientEmail} for appointment {appointment.AppointmentId}");

                            await emailService.SendQueueNumberAssignmentEmail(
                                patientEmail,
                                patientName,
                                appointmentDateStr,
                                appointmentTime,
                                appointment.QueueNumber,
                                positionInLine
                            );

                            _logger.LogInformation($"Successfully sent queue number assignment email to {patientEmail}");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, $"Failed to send queue number assignment email to {appointment.Patient.Email}. Error: {emailEx.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot send email for appointment {appointment.AppointmentId}: Patient is null or Email is empty. Patient: {appointment.Patient != null}, Email: {appointment.Patient?.Email ?? "NULL"}");
                    }
                }
            }
        }
    }
}

