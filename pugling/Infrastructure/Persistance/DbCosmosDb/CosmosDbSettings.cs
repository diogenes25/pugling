namespace pugling.Infrastructure.Persistance.DbCosmosDb
{
    public class CosmosDbSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string AccountKey { get; set; } = string.Empty;
        public string DatabaseName { get; internal set; }
        public string ContainerName { get; internal set; }

        public CosmosDbSettings(IConfiguration configuration)
        {
            Endpoint = configuration["CosmosDb:Endpoint"];
            AccountKey = configuration["CosmosDb:AccountKey"];
            DatabaseName = configuration["CosmosDb:DatabaseName"];
            ContainerName = configuration["CosmosDb:ContainerName"];
        }
    }
}