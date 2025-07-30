using Microsoft.Azure.Cosmos;
using static Microsoft.Azure.Cosmos.Container;
using CosmosChangeFeed.Models;
using Microsoft.Extensions.Configuration;
using CosmosChangeFeed.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var (endpoint, key, databaseName, sourceContainer, leaseContainer) = 
    configuration.GetSection("CosmosDb").Get<CosmosDbSettings>()!;

Console.WriteLine($"🔍 Connecting to:");
Console.WriteLine($"   Endpoint: {endpoint}");
Console.WriteLine($"   Database: {databaseName}");
Console.WriteLine($"   Source Container: {sourceContainer}");
Console.WriteLine($"   Lease Container: {leaseContainer}");
Console.WriteLine();

CosmosClient cosmosClient = new CosmosClient(endpoint, key);

Container productsContainer;
Container productsLease;

try
{
    Console.WriteLine("🔄 Testing database connection...");
    var database = cosmosClient.GetDatabase(databaseName);

    await database.ReadAsync();
    Console.WriteLine($"✅ Database '{databaseName}' found");

    Console.WriteLine("🔄 Testing container connection...");
    productsContainer = cosmosClient.GetContainer(databaseName, sourceContainer);

    await productsContainer.ReadContainerAsync();
    Console.WriteLine($"✅ Container '{sourceContainer}' found");

    Console.WriteLine("🔄 Creating lease container if needed...");
    await database.CreateContainerIfNotExistsAsync(leaseContainer, "/id");
    Console.WriteLine($"✅ Lease Container '{leaseContainer}' created successfully");

    productsLease = cosmosClient.GetContainer(databaseName, leaseContainer);
}
catch (CosmosException ex)
{
    Console.WriteLine($"❌ Cosmos DB Error:");
    Console.WriteLine($"   Status Code: {ex.StatusCode}");
    Console.WriteLine($"   Sub Status: {ex.SubStatusCode}");
    Console.WriteLine($"   Message: {ex.Message}");
    Console.WriteLine($"   ActivityId: {ex.ActivityId}");
    Console.WriteLine();
    Console.WriteLine($"🔧 Troubleshooting suggestions:");
    Console.WriteLine($"   1. Verify the endpoint URL is correct");
    Console.WriteLine($"   2. Check if the access key is valid and not expired");
    Console.WriteLine($"   3. Ensure database '{databaseName}' exists");
    Console.WriteLine($"   4. Ensure container '{sourceContainer}' exists");
    return;
}

ChangesHandler<Product> handleChanges = async (
    IReadOnlyCollection<Product> changes,
    CancellationToken cancellationToken
) =>
{
    Console.WriteLine($"Started batch of {changes.Count} changes.");

    foreach (Product product in changes)
    {
        await Console.Out.WriteLineAsync($"Detected Operation:\t[{product.id}],\tName: {product.name},\tCategory: {product.category},\tQuantity: {product.quantity},\tPrice: {product.price},\tClearance: {product.clearance}");
    }
};

var builder = productsContainer.GetChangeFeedProcessorBuilder<Product>(
    processorName: "productsProcessor",
    onChangesDelegate: handleChanges
);

ChangeFeedProcessor processor = builder
    .WithInstanceName("consoleApp")
    .WithLeaseContainer(productsLease)
    .Build();

await processor.StartAsync();

Console.WriteLine($"Run\tListening for changes in {productsContainer.Id} container...");
Console.WriteLine($"Press any key to stop");
Console.ReadKey();
