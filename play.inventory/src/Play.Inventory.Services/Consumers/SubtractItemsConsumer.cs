using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Services.Entities;
using Play.Inventory.Services.Exceptions;

namespace Play.Inventory.Services.Consumers;

public class SubtractItemsConsumer : IConsumer<SubtractItems>
{
    private readonly IRepository<InventoryItem> inventoryItemsRepository;
    private readonly IRepository<CatalogItem> catalogItemsRepository;
    private readonly ILogger<GrantItemsConsumer> logger;

    public SubtractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository, ILogger<GrantItemsConsumer> logger)
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<SubtractItems> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Subtracting {Quantity} of catalog item {CatalogItemId} from user {UserId} with CorrelationId {CorrelationId}...",
            message.Quantity,
            message.CatalogItemId,
            message.UserId,
            message.CorrelationId);

        var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

        if (item is null)
            throw new UnknownItemException(message.CatalogItemId);

        var inventoryItem = await inventoryItemsRepository.GetAsync(
            item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

        if (inventoryItem is not null)
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity -= message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await inventoryItemsRepository.UpdateAsync(inventoryItem);

            await context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));
        }

        await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
    }
}