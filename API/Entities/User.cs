using EntityFrameworkCore.EncryptColumn.Attribute;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [EncryptColumn]
        public string? FirstName { get; set; }

        [Required]
        [StringLength(50)]
        [EncryptColumn]
        public string? LastName { get; set; }

        [Required]
        [StringLength(80)]
        [EncryptColumn]
        public string? Email { get; set; }

        [Required]
        [StringLength(80)]
        [EncryptColumn]
        public string? Password { get; set; }

        [EncryptColumn]
        public string? MobileNumber { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        public UserType UserType { get; set; } = UserType.NONE;
        
        public AccountStatus AccountStatus { get; set; } = AccountStatus.UNAPROOVED;
    }
}
