using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MapApp.Domain.Entities
{
    public class AppUser : IdentityUser<int>
    {
        // IdentityUser<int> zaten Id, UserName, Email, PasswordHash gibi alanları içerir.
        // Bu yüzden tekrar tanımlamaya gerek yok.
        // Ancak ekstra alan eklemek istersen aşağıya ekleyebilirsin.

        [Required]
        public string Role { get; set; } = "User"; // Default rol

        // Örnek ekstra alan
        // public string FullName { get; set; }
    }
}
