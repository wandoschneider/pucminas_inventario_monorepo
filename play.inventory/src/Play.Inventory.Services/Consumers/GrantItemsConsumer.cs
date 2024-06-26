using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Services.Entities;
using Play.Inventory.Services.Exceptions;

namespace Play.Inventory.Services.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    private readonly IRepository<InventoryItem> inventoryItemsRepository;
    private readonly IRepository<CatalogItem> catalogItemsRepository;
    private readonly ILogger<GrantItemsConsumer> logger;

    public GrantItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository, ILogger<GrantItemsConsumer> logger)
    {
        this.inventoryItemsRepository = inventoryItemsRepository;
        this.catalogItemsRepository = catalogItemsRepository;
        this.logger = logger;
    }

    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Granting {Quantity} of catalog item {CatalogItemId} to user {UserId} with CorrelationId {CorrelationId}...",
            message.Quantity,
            message.CatalogItemId,
            message.UserId,
            message.CorrelationId);

        var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

        if (item is null)
            throw new UnknownItemException(message.CatalogItemId);

        var inventoryItem = await inventoryItemsRepository.GetAsync(
            item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity += message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await inventoryItemsRepository.UpdateAsync(inventoryItem);
        }

        var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
        var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));

        await Task.WhenAll(itemsGrantedTask, inventoryUpdatedTask);
    }
}