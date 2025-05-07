using Microsoft.Azure.Cosmos;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace pugling.Infrastructure.Persistance.DbCosmosDb
{
    public class CosmosDbContainerFactory
    {
        private readonly CosmosDbSettings _cosmosDbSettings;
        private readonly CosmosClient _client;

        public CosmosDbContainerFactory(CosmosDbSettings cosmosDbSettings)
        {
            _cosmosDbSettings = cosmosDbSettings;
            var clientOptions = new CosmosClientOptions
            {
                Serializer = new CosmosSystemTextJsonSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNameCaseInsensitive = true // optional, erhöht Robustheit
                })
            };

            _client = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.AccountKey, clientOptions);
        }

        public Container GetContainer(string containerName)
        {
            return _client.GetDatabase(_cosmosDbSettings.DatabaseName)
                .GetContainer(containerName);
        }
    }

    public class CosmosSystemTextJsonSerializer : CosmosSerializer
    {
        private readonly JsonSerializerOptions _options;

        public CosmosSystemTextJsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public override T FromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using (stream)
            {
                return JsonSerializer.Deserialize<T>(stream, _options);
            }
        }

        public override Stream ToStream<T>(T input)
        {
            var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                JsonSerializer.Serialize(writer, input, _options);
            }
            stream.Position = 0;
            return stream;
        }
    }
}