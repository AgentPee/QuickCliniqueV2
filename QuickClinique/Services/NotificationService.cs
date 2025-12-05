using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Services
{
    public interface INotificationService
    {
        Task NotifyAllClinicStaffAsync(string content, int? patientId = null);
        Task NotifyClinicStaffAsync(int clinicStaffId, string content, int? patientId = null);
        Task NotifyNewAppointmentAsync(Appointment appointment);
        Task NotifyNewPatientAsync(Student student);
        Task NotifyNewClinicStaffAsync(Clinicstaff clinicStaff);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Notifies all active clinic staff members
        /// </summary>
        public async Task NotifyAllClinicStaffAsync(string content, int? patientId = null)
        {
            var activeClinicStaff = await _context.Clinicstaffs
                .Where(cs => cs.IsActive && cs.IsEmailVerified)
                .Select(cs => cs.ClinicStaffId)
                .ToListAsync();

            var notifications = activeClinicStaff.Select(clinicStaffId => new Notification
            {
                ClinicStaffId = clinicStaffId,
                PatientId = patientId ?? 0, // Use 0 or a default patient if no patient specified
                Content = content,
                NotifDateTime = TimeZoneHelper.GetPhilippineTime(),
                IsRead = "No"
            }).ToList();

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Notifies a specific clinic staff member
        /// </summary>
        public async Task NotifyClinicStaffAsync(int clinicStaffId, string content, int? patientId = null)
        {
            var notification = new Notification
            {
                ClinicStaffId = clinicStaffId,
                PatientId = patientId ?? 0,
                Content = content,
                NotifDateTime = TimeZoneHelper.GetPhilippineTime(),
                IsRead = "No"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates notifications for all clinic staff when a new appointment is booked
        /// </summary>
        public async Task NotifyNewAppointmentAsync(Appointment appointment)
        {
            // Reload appointment with patient and schedule details to ensure we have the latest data
            var appointmentId = appointment.AppointmentId;
            var reloadedAppointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (reloadedAppointment == null)
            {
                Console.WriteLine($"[NOTIFICATION WARNING] Appointment {appointmentId} not found for notification");
                return;
            }

            var patientName = reloadedAppointment.Patient != null 
                ? $"{reloadedAppointment.Patient.FirstName} {reloadedAppointment.Patient.LastName}"
                : "Unknown Patient";

            var appointmentDate = reloadedAppointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? "Unknown Date";
            var appointmentTime = reloadedAppointment.Schedule != null 
                ? $"{reloadedAppointment.Schedule.StartTime:hh:mm tt} - {reloadedAppointment.Schedule.EndTime:hh:mm tt}"
                : "Unknown Time";

            var content = $"New appointment booked by {patientName} on {appointmentDate} at {appointmentTime}. " +
                         $"Reason: {reloadedAppointment.ReasonForVisit}";

            await NotifyAllClinicStaffAsync(content, reloadedAppointment.PatientId);
        }

        /// <summary>
        /// Creates notifications for all clinic staff when a new patient's email is verified
        /// </summary>
        public async Task NotifyNewPatientAsync(Student student)
        {
            var patientName = $"{student.FirstName} {student.LastName}";
            var content = $"New patient email verified: {patientName} (ID: {student.Idnumber}). " +
                         $"Account is pending activation.";

            await NotifyAllClinicStaffAsync(content, student.StudentId);
        }

        /// <summary>
        /// Creates notifications for all existing clinic staff when a new staff member's email is verified
        /// </summary>
        public async Task NotifyNewClinicStaffAsync(Clinicstaff clinicStaff)
        {
            var staffName = $"{clinicStaff.FirstName} {clinicStaff.LastName}";
            var content = $"New clinic staff email verified: {staffName} ({clinicStaff.Email}). " +
                         $"Account is pending activation.";

            // Get all clinic staff except the newly registered one
            var existingClinicStaff = await _context.Clinicstaffs
                .Where(cs => cs.IsActive && cs.IsEmailVerified && cs.ClinicStaffId != clinicStaff.ClinicStaffId)
                .Select(cs => cs.ClinicStaffId)
                .ToListAsync();

            var notifications = existingClinicStaff.Select(clinicStaffId => new Notification
            {
                ClinicStaffId = clinicStaffId,
                PatientId = 0, // No patient associated with staff registration
                Content = content,
                NotifDateTime = TimeZoneHelper.GetPhilippineTime(),
                IsRead = "No"
            }).ToList();

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }
        }
    }
}