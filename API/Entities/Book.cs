using System.ComponentModel.DataAnnotations;

namespace API.Entities
{
    public class Book
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string? Title { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string? Author { get; set; } = string.Empty;

        [Required]
        public float Price { get; set; }


        public bool Ordered { get; set; }
        
        public int BookCategoryId { get; set; }

        public BookCategory? BookCategory { get; set; }
    }
}
