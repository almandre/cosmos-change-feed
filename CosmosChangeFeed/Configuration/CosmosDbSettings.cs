namespace CosmosChangeFeed.Configuration
{
    public class CosmosDbSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string SourceContainer { get; set; } = string.Empty;
        public string LeaseContainer { get; set; } = string.Empty;

        public void Deconstruct(out string endpoint, out string key, out string databaseName, out string sourceContainer, out string leaseContainer)
        {
            endpoint = Endpoint;
            key = Key;
            databaseName = DatabaseName;
            sourceContainer = SourceContainer;
            leaseContainer = LeaseContainer;
        }
    }
}
