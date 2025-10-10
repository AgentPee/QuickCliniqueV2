using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int UserId { get; set; }

    public int Idnumber { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Precord> Precords { get; set; } = new List<Precord>();

    public virtual Usertype User { get; set; } = null!;
}
