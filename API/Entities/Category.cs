using System.ComponentModel.DataAnnotations;

namespace API.Entities
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string? Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string? Author { get; set; } = string.Empty;

        [Required]
        public float Price { get; set; }


        public bool Ordered { get; set; }
        
        public int BookCategoryId { get; set; }

        public BookCategory? BookCategory { get; set; }

    }
}
