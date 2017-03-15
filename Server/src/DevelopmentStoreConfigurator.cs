using Marten;

namespace GTAIdentity
{
    /// <summary>
    /// A class that configures a Store, connecting via the given ConnectionString.
    /// </summary>
    public class DevelopmentStoreConfigurator : MartenStoreConfigurator
    {
        public string ConnectionString { get; set; } = "host=localhost;database=GTAIdentity;password=postgres;username=postgres";
        public override void BaseOptions(StoreOptions options)
        {
            base.BaseOptions(options);
            options.Connection(ConnectionString);
            options.AutoCreateSchemaObjects = AutoCreate.All;
        }

    }
}
