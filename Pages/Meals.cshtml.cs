using ALLmoco.Data;
using ALLmoco.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ALLmoco.Pages
{
    [Authorize] // Essa função faz o ASPNET entender: "so usuarios authenticados entram aqui", isso é configurado la no options.LoginPath = "/Login";
    public class MealsModel : PageModel
    {
        private readonly AppDbContext _context;

        public MealsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MealCheck MealCheck { get; set; } = new();
        public List<MealCheck> MealHistory { get; set; } = new();
        public int CurrentStreak { get; set; } // int para a streak
        public string? ErrorMessage { get; set; } // string dentro da classe meal para criar a mensagem de erro no preenchimento de descricao
        // string? significa “essa variável pode ser nula”, necessário para esse caso que pode ou nao existir mensagem
        public string StreakMessage { get; set; } = ""; // classe string para a streak

        /// <summary>
        /// 
        /// BLOCO DO METODO ONGET(), RESPONSAVEL POR TRAZER AS MENSAGENS DA STREAK 
        /// 
        /// </summary>
        public void OnGet()
        {
            // MealHistory = _context.MealChecks // pega a tabela
            //  .OrderByDescending(x => x.Date) // ordena do mais recente pro mais antigo '=>'
            //  .ToList(); // transforma em lista

            var userId = int.Parse( // pega o id do usuário logado, para relacionar a refeição com o usuário, isso é possível por causa do ClaimTypes.NameIdentifier que foi criado la no Login.cshtml.cs
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value); // pega o id do usuário logado, para relacionar a refeição com o usuário, isso é possível por causa do ClaimTypes.NameIdentifier que foi criado la no Login.cshtml.cs

            MealHistory = _context.MealChecks // pega a tabela
                .Where(x => x.UserId == userId) // filtra as refeições do usuário logado
                .OrderByDescending(x => x.Date) // ordena do mais recente pro mais antigo '=>'
                .ToList(); // transforma em lista


            CalculateStreak(); // Contagem de Streak

            // bloco de código para calcular se o tempo da streak for X, uma certa mensagem irá retornar
            if (CurrentStreak == 0)
            {
                StreakMessage = "Você precisa se alimentar bem... faça uma refeição agora.";
            }
            else if (CurrentStreak >= 5 && CurrentStreak < 10)
            {
                StreakMessage = "MUUITO BEM!! VOCÊ MERECE UM PRÊMIO!";
            }
            else if (CurrentStreak >= 10 && CurrentStreak < 15)
            {
                StreakMessage = "Você está criando um hábito incrível!";
            }
            else if (CurrentStreak >= 15 && CurrentStreak < 20)
            {
                StreakMessage = "Seu corpo agradece cada refeição ❤️";
            }
            else if (CurrentStreak >= 20)
            {
                StreakMessage = "Você virou exemplo de consistência 😭🔥";
            }
            else
            {
                StreakMessage = "Continue assim! Cada refeição importa.";
            }
        }


        /// <summary>
        /// 
        /// BLOCO DE CÓDIGO RESPONSÁVEL PELAS MENSAGENS DE ERRO CASO NÃO MARQUEM AS CHECKBOX OU SE CASO TENTEM SALVAR SEM MARCAR-LAS.
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync() // Recebe os dados
        {
            if (!MealCheck.AteMeal && !MealCheck.DidNotEat) // atualizada a função, dando a opção apenas de marcar a checkbox correta
            {
                ErrorMessage = "Marque uma das opções antes de salvar.";

                MealHistory = _context.MealChecks
                    .OrderByDescending(x => x.Date)
                    .ToList();

                CalculateStreak();

                return Page();
            }

            if (MealCheck.AteMeal && MealCheck.DidNotEat)
            {
                ErrorMessage = "Selecione apenas uma opção.";

                MealHistory = _context.MealChecks
                    .OrderByDescending(x => x.Date)
                    .ToList();

                CalculateStreak();

                return Page();
            }

            MealCheck.Date = DateTime.UtcNow;
            // metodo que verifica se existe refeição do tipo selecionado, se existir ele não vai salvar a refeição, alem de que ele verifica se a ref foi marcada como feita
            DateTime today = DateTime.UtcNow.Date;
            DateTime tomorrow = today.AddDays(1);

            var userId = int.Parse( // pega o id do usuário logado, para relacionar a refeição com o usuário, isso é possível por causa do ClaimTypes.NameIdentifier que foi criado la no Login.cshtml.cs
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // LINQ para verificar se já existe uma refeição do mesmo tipo feita hoje, para evitar que o usuário registre a mesma refeição mais de uma vez no mesmo dia
            bool alreadyExists = _context.MealChecks.Any(x =>
                x.UserId == userId &&
                x.MealType == MealCheck.MealType &&
                x.Date >= today &&
                x.Date < tomorrow &&
                x.AteMeal);

            if (alreadyExists)
            {
                ErrorMessage = "Essa refeição já foi registrada hoje.";

                MealHistory = _context.MealChecks
                    .OrderByDescending(x => x.Date)
                    .ToList();

                CalculateStreak();

                return Page();
            }
            // Atribui o UserId do MealCheck com o id do usuário logado, para relacionar a refeição com o usuário
            MealCheck.UserId = userId;

            _context.MealChecks.Add(MealCheck); // adiciona no banco

            await _context.SaveChangesAsync(); // salva

            return RedirectToPage();

            }

        /// <summary>
        /// 
        /// BLOCO RESPONSAVEL PELA CRIAÇÃO DOS CARDS DE HISTÓRICO 
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        public async Task<IActionResult> OnPostDeleteAsync(int id) // método responsavel por criar o delete nos cards do histórico
        {
            var meal = await _context.MealChecks.FindAsync(id);

            if (meal != null)
            {
                _context.MealChecks.Remove(meal);

                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        /// <summary>
        /// 
        /// BLOCO RESPONSAVEL PELA ACAO DO BOTÃO DE LIMPÉZA DO HISTORICO GERAL
        /// 
        /// </summary>
        /// <returns></returns>

        public async Task<IActionResult> OnPostDeleteAllAsync() // botão de limpar o historico geral
        {
            _context.MealChecks.RemoveRange(_context.MealChecks);

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        /// <summary>
        /// 
        /// BLOCO DEDICADO PARA O FUNCIONAMENTO DA STREAK
        /// 
        /// </summary>

        private void CalculateStreak() // LINQ feito para criar uma contagem de Streak
        {
            // LINQ para calcular a streak, ele pega as refeições feitas, agrupa por dia, filtra os dias que tem 2 ou mais refeições feitas, ordena por data decrescente e depois conta quantos dias consecutivos tem a partir de hoje
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var dates = _context.MealChecks
                .Where(x => x.AteMeal && x.UserId == userId) // pega so refeições feitas por usuário logado, para contar a streak apenas com as refeições do usuário logado
                .ToList() // traz os dados primeiro
                .GroupBy(x => x.Date.Date) // agrupa por dia
                .Where(group => group.Count() >= 2) // filtra as refeições pra contar streak apenas com 2 refeições ou mais
                .Select(group => group.Key) // pega apenas a data
                .OrderByDescending(date => date)
                .ToList();

            int streak = 0;

            DateTime currentDate = DateTime.Today;

            foreach (var date in dates)
            {
                if (date == currentDate)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else if (date == currentDate.AddDays(-1))
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            CurrentStreak = streak;
        }
    }
    }
