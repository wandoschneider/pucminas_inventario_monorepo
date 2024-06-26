using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Services.Entities;

namespace Play.Inventory.Services.Consumers;

public class CatalogItemCreatedConsumer : IConsumer<CatalogItemCreated>
{
    private readonly IRepository<CatalogItem> repository;

    public CatalogItemCreatedConsumer(IRepository<CatalogItem> repository)
    {
        this.repository = repository;
    }

    public async Task Consume(ConsumeContext<CatalogItemCreated> context)
    {
        var message = context.Message;

        var item = await repository.GetAsync(message.ItemId);

        if (item is not null)
            return;

        item = new CatalogItem
        {
            Id = message.ItemId,
            Name = message.Name,
            Description = message.Description
        };

        await repository.CreateAsync(item);

    }
}