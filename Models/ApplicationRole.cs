using Microsoft.AspNetCore.Identity;
using System;

namespace PracticeLogger.Models
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
