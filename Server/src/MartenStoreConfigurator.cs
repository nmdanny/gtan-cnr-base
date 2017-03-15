using System;
using Marten;
using GTAIdentity.Models;
using Newtonsoft.Json;
using Marten.Services;

namespace GTAIdentity
{
    /// <summary>
    /// A base class for creating a <see cref="DocumentStore"/>, by providing some common <see cref="StoreOptions"/> for Marten. 
    /// <para/>
    /// Implement any environment-independent database options here, such as the schema, serialization, etc. 
    /// <para/>
    /// This is notably missing a connection string which you should implement in a subclass. See <see cref="DevelopmentStoreConfigurator"/> for a basic example.
    /// </summary>
    public abstract class MartenStoreConfigurator
    {
        /// <summary>
        /// Override this to configure the Store's options differently. 
        /// </para>
        /// You should call this base method within your override, unless you want to configure your Store completely from scratch.
        /// 
        /// </summary>
        /// <param name="options">The configuration object.</param>
        public virtual void BaseOptions(StoreOptions options)
        {
            options.AutoCreateSchemaObjects = AutoCreate.CreateOnly;


            var serializer = new JsonNetSerializer();
            serializer.EnumStorage = EnumStorage.AsInteger;
            serializer.Customize(x =>
            {
                x.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            });

            options.Serializer(serializer);
            options.Schema.For<Ban>().ForeignKey<Account>(x => x.AccountId)
                                     .ForeignKey<Account>(x => x.BannerAccountId);
            options.Schema.For<Account>().Index(x => x.Username, x =>
            {
                x.IsUnique = true;
            });

            options.Schema.For<Character>().ForeignKey<Account>(x => x.AccountId)
                                           .Index(x => x.Name, x =>
                                           {
                                               x.IsUnique = true;
                                           })
                                           .AddSubClass<Cop>()
                                           .AddSubClass<Civilian>();

        }

        /// <summary>
        /// Constructs a <see cref="DocumentStore"/>. 
        /// </summary>
        /// <returns>Returns a <see cref="DocumentStore"/></returns>
        public DocumentStore GetStore()
        {
            return DocumentStore.For(BaseOptions);
        }
    }
}
