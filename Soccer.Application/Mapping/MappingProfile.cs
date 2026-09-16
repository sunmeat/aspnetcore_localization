using AutoMapper;
using Soccer.Application.DTO;
using Soccer.Domain.Entities;

namespace Soccer.Application.Mapping
{
    /// <summary>
    /// В прикладі на 3-шарову архітектуру кожен сервіс створював свій MapperConfiguration
    /// прямо всередині GetAll() - конфігурація перебудовувалась при кожному виклику.
    /// Тут вона винесена в єдиний AutoMapper-профіль, який реєструється один раз
    /// через AddAutoMapper у DI-контейнері (Application/DependencyInjection),
    /// а IMapper інжектиться в сервіси через конструктор.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Team, TeamDTO>(); // мепінг простіше, тому що властивості збігаються за назвою та типами

            CreateMap<Player, PlayerDTO>()
                .ForMember(d => d.Team, o => o.MapFrom(s => s.Team != null ? s.Team.Name : null));
            /* d => d.Team — вказує на властивість Team у цільовому типі (PlayerDTO), куди потрібно записати значення
             * o => o.MapFrom(s => s.Team != null ? s.Team.Name : null) — визначає джерело значення:
             * з вихідного об'єкта (Player) береться властивість Team, і якщо вона не null, то мапиться лише її Name (рядок), інакше — null
             * вью не потрібне посилання на команду, тільки її назва
             */
        }
    }
}