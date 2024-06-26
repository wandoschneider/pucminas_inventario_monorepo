using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Services.Dtos;
using Play.Catalog.Services.Entities;
using Play.Common;

namespace Play.Catalog.Services.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string AdminRole = "Admin";


    private readonly IRepository<Item> itemsRepository;
    private readonly IPublishEndpoint publishEndpoint;

    public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
    {
        this.itemsRepository = itemsRepository;
        this.publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    [Authorize(Policies.Read)]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
    {
        var items = (await itemsRepository.GetAllAsync())
                    .Select(item => item.AsDto());

        return Ok(items);
    }

    [HttpGet("{id}")]
    [Authorize(Policies.Read)]
    public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
    {
        var item = await itemsRepository.GetAsync(id);

        if (item is null)
            return NotFound();

        return item.AsDto();
    }

    [HttpPost]
    [Authorize(Policies.Write)]
    public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
    {
        var item = new Item
        {
            Name = createItemDto.Name,
            Description = createItemDto.Description,
            Price = createItemDto.Price,
            CreatedDate = DateTimeOffset.UtcNow
        };

        await itemsRepository.CreateAsync(item);

        await publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description, item.Price));

        return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    [Authorize(Policies.Write)]
    public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
    {
        var existingItem = await itemsRepository.GetAsync(id);

        if (existingItem is null)
            return NotFound();

        existingItem.Name = updateItemDto.Name;
        existingItem.Description = updateItemDto.Description;
        existingItem.Price = updateItemDto.Price;

        await itemsRepository.UpdateAsync(existingItem);

        await publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description, existingItem.Price));

        return Ok(existingItem);
    }

    [HttpDelete("{id}")]
    [Authorize(Policies.Write)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var item = await itemsRepository.GetAsync(id);

        if (item is null)
            return NotFound();

        await itemsRepository.RemoveAsync(item.Id);

        await publishEndpoint.Publish(new CatalogItemDeleted(item.Id));

        return Ok(item);
    }
}