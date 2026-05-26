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

        public bool StreakAtRisk { get; set; } // propriedade de risco de perder a streak

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
                StreakMessage = "CONTINUE ASSIM, VOCÊ É TÃO FORTE QUANTO PENSA!!";
            }
            else if (CurrentStreak >= 15 && CurrentStreak < 20)
            {
                StreakMessage = "Oii rs, to gostando de ver os resultados da boa alimentação ❤️";
            }
            else if (CurrentStreak >= 20)
            {
                StreakMessage = "Você virou exemplo de consistência 🔥";
            }
            else
            {
                StreakMessage = "Continue assim! Cada refeição importa.";
            }
        }


        /// <summary>
        /// 
        /// BLOCO DE CÓDIGO RESPONSÁVEL PELAS MENSAGENS DE ERRO CASO NÃO MARQUEM A CHECKBOX.
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

            MealCheck.Date = DateTime.UtcNow;
            // metodo que verifica se existe refeição do tipo selecionado, se existir ele não vai salvar a refeição, alem de que ele verifica se a ref foi marcada como feita
            DateTime today = DateTime.UtcNow.Date;
            DateTime tomorrow = today.AddDays(1);

            var userId = int.Parse( // pega o id do usuário logado, para relacionar a refeição com o usuário, isso é possível por causa do ClaimTypes.NameIdentifier que foi criado la no Login.cshtml.cs
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // LINQ para verificar se já existe uma refeição do mesmo tipo feita hoje, para evitar que o usuário registre a mesma refeição mais de uma vez no mesmo dia
            bool alreadyExists = _context.MealChecks.Any(x =>
                x.UserId == userId &&
                x.MealType != "Personalizado" &&
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
        /// Calcula a streak atual do usuário com base nos dias que possuem
        /// pelo menos 2 refeições registradas como realizadas.
        ///
        /// Regras da streak:
        /// - Dias consecutivos contam normalmente.
        /// - É permitido 1 dia de tolerância sem perder a sequência.
        /// - Caso existam 2 ou mais dias consecutivos sem registros válidos,
        ///   a streak é encerrada.
        /// - Quando a streak entra em tolerância, a propriedade
        ///   StreakAtRisk é ativada para exibir alertas visuais ao usuário.
        ///
        /// Funcionamento:
        /// - As refeições são agrupadas por data.
        /// - Apenas dias com 2 ou mais refeições realizadas são considerados.
        /// - As datas são ordenadas da mais recente para a mais antiga.
        /// - A diferença entre as datas é utilizada para determinar:
        ///     0 = mesmo dia
        ///     1 = dia consecutivo
        ///     2 = tolerância de 1 dia
        ///     3+ = quebra da streak
        ///
        /// O valor final é armazenado em CurrentStreak.
        /// 
        /// </summary>

        private void CalculateStreak() // LINQ feito para criar uma contagem de Streak
        {
            // LINQ para calcular a streak, ele pega as refeições feitas, agrupa por dia, filtra os dias que tem 2 ou mais refeições feitas, ordena por data decrescente e depois conta quantos dias consecutivos tem a partir de hoje
            var userId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var dates = _context.MealChecks
                .Where(x => x.MealType != "Personalizado") // diz que x poderá fazer parte dos dados de mealtype se for diferente de personalizado
                .Where(x => x.AteMeal && x.UserId == userId) // pega so refeições feitas por usuário logado, para contar a streak apenas com as refeições do usuário logado
                .ToList() // traz os dados primeiro
                .GroupBy(x => x.Date.Date) // agrupa por dia
                .Select(group => new
                {
                    Date = group.Key,
                    Count = group.Count()
                }) 
                .OrderByDescending(day => day.Date)
                .ToList();

            int streak = 0;

            StreakAtRisk = false;

            DateTime currentDate = DateTime.UtcNow.Date; // responsavel pela perca da Streak caso passe um dia sem marcar


            foreach (var day in dates)
            {
                int difference = (currentDate - day.Date).Days;

                // quebra total da sequencia
                if (difference >= 3)
                {
                    break;
                }

                // dia perfeito (2 ou mais refs)
                if (day.Count >= 2)
                {
                    streak++;

                    currentDate = day.Date.AddDays(-1);
                }

                // dia parcial
                else if (day.Count == 1)
                {
                    if (day.Date == DateTime.UtcNow.Date ||
                        day.Date == DateTime.UtcNow.Date.AddDays(-1))
                    {
                        StreakAtRisk = true;
                    }

                }
               
            }

            CurrentStreak = streak;
        }
    }
    }
