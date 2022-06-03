using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        [Key]
        public int? Id { get; set; }
        public string? Name { get; set; }
    }
}
