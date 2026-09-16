using AutoMapper;
using Soccer.Application.DTO;
using Soccer.Application.Interfaces;
using Soccer.Common.Exceptions;
using Soccer.Domain.Entities;
using Soccer.Domain.Interfaces;

namespace Soccer.Application.Services
{
    public class PlayerService : IEntityService<PlayerDTO>
    {
        private IUnitOfWork Database { get; set; } // юніт оф ворк для доступу до репозиторіїв, буде створений через DI
        private readonly IMapper mapper;

        public PlayerService(IUnitOfWork uow, IMapper mapper)
        {
            Database = uow;
            this.mapper = mapper;
        }

        public async Task Create(PlayerDTO playerDto)
        {
            var player = new Player // приклад ручного мапінгу DTO в Entity
            {
                Id = playerDto.Id,
                Name = playerDto.Name,
                Age = playerDto.Age,
                Position = playerDto.Position,
                TeamId = playerDto.TeamId
            };

            await Database.Players.Create(player); // створюємо сутність гравця
            await Database.Save(); // зберігаємо зміни в БД
        }

        public async Task Update(PlayerDTO playerDto)
        {
            var player = new Player
            {
                Id = playerDto.Id,
                Name = playerDto.Name,
                Age = playerDto.Age,
                Position = playerDto.Position,
                TeamId = playerDto.TeamId
            };

            Database.Players.Update(player); // оновлюємо сутність
            await Database.Save(); // зберігаємо зміни
        }

        public async Task Delete(int id)
        {
            await Database.Players.Delete(id); // видаляємо гравця за ідентифікатором
            await Database.Save(); // зберігаємо зміни
        }

        public async Task<PlayerDTO> Get(int id)
        {
            var player = await Database.Players.Get(id); // отримуємо гравця за ідентифікатором із БД
            if (player == null)
                throw new ValidationException("Немає такого гравця!"); // викидаємо виключення, якщо гравця не знайдено

            return new PlayerDTO
            {
                Id = player.Id,
                Name = player.Name,
                Age = player.Age,
                Position = player.Position,
                TeamId = player.TeamId,
                Team = player.Team?.Name // назва команди, якщо вона завантажена
            };
        }

        // automapper дозволяє проєціювати одну модель на іншу, що зменшує обсяг коду та спрощує програму
        public async Task<IEnumerable<PlayerDTO>> GetAll()
        {
            // мапер тепер один раз налаштований у Soccer.Application.Mapping.MappingProfile
            // і переданий сюди через DI, замість того щоб перебудовувати конфігурацію щоразу
            return mapper.Map<IEnumerable<PlayerDTO>>(await Database.Players.GetAll());
        }
    }
}