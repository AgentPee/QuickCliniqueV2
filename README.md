# QuickClinique - University Clinic Management System

A comprehensive web-based clinic management system designed for university medical-dental clinics. Built with ASP.NET Core MVC, Entity Framework Core, and MySQL.

## 🏥 Features

### For Students
- **User Registration & Authentication** - Secure account creation with email verification
- **Appointment Booking** - Easy online appointment scheduling with real-time availability
- **Queue Management** - Real-time queue status and notifications
- **Appointment History** - View past and upcoming appointments
- **Messaging System** - Direct communication with clinic staff
- **Password Recovery** - Secure password reset functionality

### For Clinic Staff
- **Comprehensive Dashboard** - Real-time overview of appointments, statistics, and notifications
- **Appointment Management** - Confirm, complete, or cancel appointments
- **Schedule Management** - Create and manage clinic schedules with bulk operations
- **Patient Records** - Maintain detailed patient medical records
- **Notification System** - Send notifications to patients
- **Queue Management** - Monitor and manage patient queues in real-time

### System Features
- **Secure Authentication** - Password hashing, email verification, session management
- **Responsive Design** - Mobile-friendly interface with modern UI/UX
- **Real-time Updates** - Live queue status and appointment updates
- **Data Validation** - Comprehensive input validation and error handling
- **Email Integration** - Automated email notifications and verification
- **Role-based Access** - Separate interfaces for students and clinic staff

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- MySQL Server 8.0+
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/quickclinique.git
   cd quickclinique
   ```

2. **Configure Database**
   - Install MySQL Server
   - Create a database named `quickclinique`
   - Update connection string in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "server=localhost;database=quickclinique;uid=root;pwd=yourpassword;"
     }
   }
   ```

3. **Configure Email Settings** (Optional)
   ```json
   {
     "EmailSettings": {
       "FromEmail": "noreply@quickclinique.com",
       "FromName": "QuickClinique",
       "SmtpServer": "smtp.gmail.com",
       "SmtpPort": "587",
       "SmtpUsername": "your-email@gmail.com",
       "SmtpPassword": "your-app-password"
     }
   }
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

5. **Access the Application**
   - Navigate to `https://localhost:5001`
   - Default admin credentials:
     - Email: `admin@quickclinique.com`
     - Password: `Admin123!`

## 📁 Project Structure

```
QuickClinique/
├── Controllers/          # MVC Controllers
│   ├── DashboardController.cs
│   ├── StudentController.cs
│   ├── ClinicStaffController.cs
│   ├── AppointmentsController.cs
│   └── ...
├── Models/              # Data Models and ViewModels
│   ├── Student.cs
│   ├── Clinicstaff.cs
│   ├── Appointment.cs
│   └── ...
├── Views/               # Razor Views
│   ├── Student/
│   ├── ClinicStaff/
│   ├── Dashboard/
│   └── Shared/
├── Services/            # Business Logic Services
│   ├── EmailService.cs
│   ├── PasswordService.cs
│   └── DataSeedingService.cs
├── Attributes/          # Custom Authorization Attributes
├── Middleware/          # Custom Middleware
└── wwwroot/            # Static Files (CSS, JS, Images)
```

## 🔧 Configuration

### Database Configuration
The application uses Entity Framework Core with MySQL. Database migrations are automatically applied on startup.

### Email Configuration
Email functionality is configured through `appsettings.json`. For development, emails are logged to console.

### Security Features
- Password hashing using PBKDF2 with salt
- Email verification for new accounts
- Session-based authentication
- CSRF protection
- Input validation and sanitization

## 🎨 UI/UX Features

- **Modern Design** - Clean, professional interface with Bootstrap 5
- **Responsive Layout** - Works seamlessly on desktop, tablet, and mobile
- **Dark Mode Support** - Toggle between light and dark themes
- **Accessibility** - WCAG compliant with proper ARIA labels
- **Real-time Updates** - Live data updates without page refresh
- **Interactive Elements** - Smooth animations and transitions

## 📊 Database Schema

### Core Entities
- **Students** - Patient information and authentication
- **ClinicStaff** - Staff members and authentication
- **Appointments** - Booking information and status
- **Schedules** - Available time slots
- **Notifications** - System and user notifications
- **Messages** - Communication between users
- **Precords** - Patient medical records

## 🔒 Security

- **Password Security** - PBKDF2 hashing with salt
- **Session Management** - Secure session handling
- **Authorization** - Role-based access control
- **Input Validation** - Comprehensive data validation
- **CSRF Protection** - Anti-forgery tokens
- **SQL Injection Prevention** - Parameterized queries

## 🚀 Deployment

### Production Deployment

1. **Configure Production Settings**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "your-production-connection-string"
     },
     "EmailSettings": {
       "SmtpServer": "your-smtp-server",
       "SmtpUsername": "your-username",
       "SmtpPassword": "your-password"
     }
   }
   ```

2. **Build for Production**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

3. **Deploy to Server**
   - Copy published files to web server
   - Configure IIS or Apache
   - Set up SSL certificate
   - Configure database connection

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Perry Ian Mendoza** - *Backend* - (https://github.com/AgentPee)
- **Adonis Henry Espinosa** - *Frontend* - (https://github.com/QuinntinChuy)

## 🙏 Acknowledgments

- University of Cebu for the project requirements
- Bootstrap team for the UI framework
- Entity Framework team for the ORM
- All contributors and testers

## 📞 Support

For support, email quickclinique@gmail.com or create an issue in the repository.

---

**QuickClinique** - Your Health, Our Priority 🏥


