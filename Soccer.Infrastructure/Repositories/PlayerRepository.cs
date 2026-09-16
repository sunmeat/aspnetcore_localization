using Microsoft.EntityFrameworkCore;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;
using Soccer.Infrastructure.Persistence;

namespace Soccer.Infrastructure.Repositories
{
    public class PlayerRepository : IRepository<Player>
    {
        private SoccerContext db;

        public PlayerRepository(SoccerContext context) // конструктор, що приймає контекст через DI
        {
            db = context;
        }

        public async Task<IEnumerable<Player>> GetAll() // отримання всіх гравців
        {
            return await db.Player.Include(o => o.Team).ToListAsync(); // завантажуємо гравців разом з пов'язаною командою (eager loading) та повертаємо список
        }

        public async Task<Player?> Get(int id) // асинхронний метод для отримання гравця за ідентифікатором
        {
            var players = await db.Player.Include(o => o.Team).Where(a => a.Id == id).ToListAsync(); // фільтруємо та завантажуємо команду
            Player? player = players?.FirstOrDefault(); // беремо перший (або єдиний) елемент зі списку
            return player; // повертаємо знайденого гравця або null
        }

        public async Task<Player?> Get(string name) // асинхронний метод для отримання гравця за ім'ям
        {
            var players = await db.Player.Where(a => a.Name == name).ToListAsync(); // фільтруємо за точним збігом імені без завантаження команди
            Player? player = players?.FirstOrDefault(); // беремо першого знайденого
            return player; // повертаємо гравця або null
        }

        public async Task Create(Player player) // асинхронний метод для створення нового гравця
        {
            await db.Player.AddAsync(player); // додаємо сутність до dbset асинхронно (зміни зберігаються пізніше через savechanges)
        }

        public void Update(Player player) // синхронний метод для оновлення гравця
        {
            db.Entry(player).State = EntityState.Modified; // явно позначаємо сутність як змінену для відстеження EF
        }

        public async Task Delete(int id) // асинхронний метод для видалення гравця за ідентифікатором
        {
            Player? player = await db.Player.FindAsync(id); // ефективний пошук за первинним ключем асинхронно
            if (player != null) // перевірка на існування
                db.Player.Remove(player); // видаляємо сутність з dbset (зміни застосовуються при savechanges)
        }
    }
}