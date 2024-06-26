using MassTransit;
using Play.Common;
using Play.Identity.Contracts;
using Play.Trading.Services.Entities;


namespace Play.Trading.Services.Consumers;

public class UserUpdatedConsumer : IConsumer<UserUpdated>
{
    private readonly IRepository<ApplicationUser> repository;

    public UserUpdatedConsumer(IRepository<ApplicationUser> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<UserUpdated> context)
    {
        var message = context.Message;

        var user = await repository.GetAsync(message.UserId);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = message.UserId,
                Gil = message.NewTotalGil
            };

            await repository.CreateAsync(user);
        }
        else
        {
            user.Gil = message.NewTotalGil;

            await repository.UpdateAsync(user);
        }
    }
}
