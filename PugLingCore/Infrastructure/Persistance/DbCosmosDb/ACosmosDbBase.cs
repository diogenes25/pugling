using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace pugling.Infrastructure.Persistance.DbCosmosDb
{
    public abstract class ACosmosDbBase
    {
        protected readonly ILogger<ACosmosDbBase> _logger;

        private readonly CosmosDbContainerFactory _cosmosDbContainerFactory;
        protected readonly Container _container;

        public ACosmosDbBase(CosmosDbContainerFactory cosmosDbContainerFactory, string containerName, ILogger<ACosmosDbBase> logger)
        {
            _cosmosDbContainerFactory = cosmosDbContainerFactory;
            _container = _cosmosDbContainerFactory.GetContainer(containerName);
            _logger = logger;
        }

        public Container GetContainer(string containerName)
        {
            return _cosmosDbContainerFactory.GetContainer(containerName);
        }
    }
}