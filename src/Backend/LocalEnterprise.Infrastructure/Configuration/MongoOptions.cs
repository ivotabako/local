namespace LocalEnterprise.Infrastructure.Configuration;

public sealed class MongoOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
}
