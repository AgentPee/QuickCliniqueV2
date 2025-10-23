using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;

namespace QuickClinique.Services
{
    public interface IDataSeedingService
    {
        Task SeedDataAsync();
    }

    public class DataSeedingService : IDataSeedingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public DataSeedingService(ApplicationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task SeedDataAsync()
        {
            // Check if data already exists
            if (await _context.Usertypes.AnyAsync())
            {
                return; // Data already seeded
            }

            // Create default user types
            var userTypes = new List<Usertype>
            {
                new Usertype { Name = "System Administrator", Role = "Admin" },
                new Usertype { Name = "Clinic Staff", Role = "ClinicStaff" },
                new Usertype { Name = "Student", Role = "Student" }
            };

            _context.Usertypes.AddRange(userTypes);
            await _context.SaveChangesAsync();

            // Create default admin clinic staff
            var adminUserType = userTypes.First(ut => ut.Role == "Admin");
            var adminStaff = new Clinicstaff
            {
                UserId = adminUserType.UserId,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@quickclinique.com",
                PhoneNumber = "09123456789",
                Password = _passwordService.HashPassword("Admin123!"),
                IsEmailVerified = true
            };

            _context.Clinicstaffs.Add(adminStaff);
            await _context.SaveChangesAsync();

            // Create sample schedules for the next 30 days
            var schedules = new List<Schedule>();
            var startDate = DateTime.Today;
            
            for (int i = 0; i < 30; i++)
            {
                var currentDate = startDate.AddDays(i);
                
                // Skip weekends
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Create morning slots (9 AM - 12 PM)
                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0),
                    IsAvailable = "Yes"
                });

                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0),
                    IsAvailable = "Yes"
                });

                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(12, 0),
                    IsAvailable = "Yes"
                });

                // Create afternoon slots (1 PM - 5 PM)
                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(14, 0),
                    IsAvailable = "Yes"
                });

                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(15, 0),
                    IsAvailable = "Yes"
                });

                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(15, 0),
                    EndTime = new TimeOnly(16, 0),
                    IsAvailable = "Yes"
                });

                schedules.Add(new Schedule
                {
                    Date = DateOnly.FromDateTime(currentDate),
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(17, 0),
                    IsAvailable = "Yes"
                });
            }

            _context.Schedules.AddRange(schedules);
            await _context.SaveChangesAsync();

            Console.WriteLine("Database seeded successfully!");
        }
    }
}
