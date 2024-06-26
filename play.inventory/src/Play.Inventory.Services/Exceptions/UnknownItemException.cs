namespace Play.Inventory.Services.Exceptions;

[Serializable]
internal class UnknownItemException : Exception
{
    public Guid ItemId { get; }
    public UnknownItemException(Guid itemId) : base($"Unknown item'{itemId}'")
    {
        this.ItemId = itemId;
    }
}