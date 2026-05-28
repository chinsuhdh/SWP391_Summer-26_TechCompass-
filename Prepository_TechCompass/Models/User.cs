using System;
using System.Collections.Generic;

namespace Repository_TechCompass.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Provider { get; set; } = null!;

    public string? ProviderId { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiry { get; set; }

    public virtual Mentor? Mentor { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual Student? Student { get; set; }
}
