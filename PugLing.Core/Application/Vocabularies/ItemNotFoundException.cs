namespace PugLing.Core.Application.Vocabularies;

public class ItemNotFoundException(string id) : Exception($"Item with id {id} not found.")
{
}