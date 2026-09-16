using Microsoft.EntityFrameworkCore;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;
using Soccer.Infrastructure.Persistence;

namespace Soccer.Infrastructure.Repositories
{
    public class TeamRepository : IRepository<Team>
    {
        private SoccerContext db;

        public TeamRepository(SoccerContext context)
        {
            db = context;
        }

        public async Task<IEnumerable<Team>> GetAll() // отримання всіх команд
        {
            return await db.Team.ToListAsync(); // повертаємо список всіх команд без додаткового завантаження пов'язаних даних
        }

        public async Task<Team?> Get(int id) // отримання команди за ідентифікатором
        {
            Team? team = await db.Team.FindAsync(id); // пошук за первинним ключем асинхронно
            return team; // повертаємо знайдену команду або null
        }

        public async Task<Team?> Get(string name) // отримання команди за назвою
        {
            var teams = await db.Team.Where(a => a.Name == name).ToListAsync(); // фільтруємо за точним збігом назви
            Team? team = teams?.FirstOrDefault(); // беремо першу знайдену команду
            return team; // повертаємо команду або null
        }

        public async Task Create(Team team) // створення нової команди
        {
            await db.Team.AddAsync(team); // додаємо сутність до dbset асинхронно (зміни зберігаються пізніше)
        }

        public void Update(Team team) // оновлення команди
        {
            db.Entry(team).State = EntityState.Modified; // явно позначаємо сутність як змінену для відстеження EF
            // при виклику SaveChanges() фреймворк згенерує і виконає SQL-команду UPDATE для оновлення відповідного запису в БД
        }

        public async Task Delete(int id) // видалення команди за ідентифікатором
        {
            Team? team = await db.Team.FindAsync(id); // пошук за первинним ключем
            if (team != null) // перевірка на існування
                db.Team.Remove(team); // видаляємо сутність з dbset (зміни застосовуються при savechanges)
        }
    }
}