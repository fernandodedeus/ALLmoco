using ALLmoco.Data;
using ALLmoco.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ALLmoco.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty] // Liga o input do HTML com a variável do C#, ou seja, asp-for Username vai se conectar com String Username atraves dessa propriedade.
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
        public string SuccessMessage { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public IActionResult OnPost() // o fluxo está como: Usuario digita > Onpost > verifica dupllicado > cria o user > salva no banco
        {
            var userExists = _context.Users.Any(u => u.Username == Username); // essa linha dessa função context users verifica se existe users duplicados

            if (userExists)
            {
                ErrorMessage = "Usuário já existe.";
                return Page();
            }

            var user = new User
            {
                Username = Username,
                Password = Password
            };

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Preencha todos os campos corretamente.";
                return Page();
            }

            _context.Users.Add(user);
            _context.SaveChanges(); // aqui é onde o banco recebe os dados, salvando o registro do usuario

            SuccessMessage = "Usuário cadastrado com sucesso!";

            return Page();

        }
    }
}