using System;
using System.Collections.Generic;

namespace QuickClinique.Models;

public partial class Message
{
    public int MessageId { get; set; }

    public int SenderId { get; set; }

    public int ReceiverId { get; set; }

    public string Message1 { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Usertype Receiver { get; set; } = null!;

    public virtual Usertype Sender { get; set; } = null!;
}
