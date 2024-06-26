using Play.Common;

namespace Play.Trading.Services.Entities;

public class ApplicationUser : IEntity
{
    public Guid Id { get; set; }
    public decimal Gil { get; set; }
}
