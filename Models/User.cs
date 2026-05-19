using System.ComponentModel.DataAnnotations;

namespace ALLmoco.Models
{
    public class User
    {
        public int Id { get; set; } // primary key
        [Required]
        public string Username { get; set; } = string.Empty; // string do username do usuario
        [Required]
        public string Password { get; set; } = string.Empty; // senha do usuario

        public List<MealCheck> MealChecks { get; set; } = new(); // lista de MealChecks para relacionar as refeições com o usuário, um usuário pode ter várias refeições
    }
}
