namespace Zayna.Store.Server.Data;

public sealed class DatabaseConfig
{
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Database { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public int MinimumPoolSize { get; set; }
    public int MaximumPoolSize { get; set; }

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={UserName};Password={Password};Maximum Pool Size={MaximumPoolSize};Minimum Pool Size={MinimumPoolSize}";
}
