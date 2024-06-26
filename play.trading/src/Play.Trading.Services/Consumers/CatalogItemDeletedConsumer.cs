using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Trading.Services.Entities;

namespace Play.Trading.Services.Consumers;

public class CatalogItemDeletedConsumer : IConsumer<CatalogItemDeleted>
{
    private readonly IRepository<CatalogItem> repository;

    public CatalogItemDeletedConsumer(IRepository<CatalogItem> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<CatalogItemDeleted> context)
    {
        var message = context.Message;

        var item = await repository.GetAsync(message.ItemId);

        if (item is null)
            return;

        await repository.RemoveAsync(message.ItemId);
    }
}