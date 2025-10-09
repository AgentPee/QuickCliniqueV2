using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Usertype
{
    public int UserId { get; set; }

    public string Role { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Clinicstaff> Clinicstaffs { get; set; } = new List<Clinicstaff>();

    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
