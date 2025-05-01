namespace pugling.Infrastructure.DbCosmosDb
{
    public record CosmosDbSettings
    {
        public string AccountEndpoint { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string DatabaseName { get; internal set; }
        public string ContainerName { get; internal set; }
    }
}