using Microsoft.Azure.Cosmos;

namespace pugling.Infrastructure.DbCosmosDb
{
    public abstract class ACosmosDbBase
    {
        protected readonly ILogger<ACosmosDbBase> _logger;

        public string ContainerName { get; }

        private readonly CosmosDbSettings _cosmosDbSettings;
        private CosmosClient _client;
        protected Container _container;

        public ACosmosDbBase(CosmosDbSettings cosmosDbSettings, string containerName, ILogger<ACosmosDbBase> logger)
        {
            ContainerName = containerName;
            _cosmosDbSettings = cosmosDbSettings;
            _client = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.AccountKey);
            _logger = logger;
            InitializeCosmosClientAndContainer();
        }

        private void InitializeCosmosClientAndContainer()
        {
            _container = _client.GetDatabase(_cosmosDbSettings.DatabaseName)
                 .GetContainer(ContainerName);
        }
    }
}