
using Bogus;
using DnsClient;
using Microsoft.EntityFrameworkCore;
using MonarchDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MonarchDB
{
    using System;
    using System.Collections.Generic;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using MongoDB.Driver;

    
    public class Dynastie
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Land { get; set; }
        public DateTime Beginnherrschaft { get; set; }
        public DateTime? Endeherrschaft { get; set; }
        public List<Monarch> Monarchen { get; set; }
    }

    public class Monarch
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Regierungsbeginn { get; set; }
        public DateTime? Regierungsende { get; set; }
        public int DynastieId { get; set; }
        public Dynastie Dynastie { get; set; }
    }

    // MongoDB Model Classes
    public class MongoDynastie
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Land { get; set; }
        public DateTime Beginnherrschaft { get; set; }
        public DateTime? Endeherrschaft { get; set; }
        public List<MongoMonarch> Monarchen { get; set; }
    }

    public class MongoMonarch
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public DateTime Regierungsbeginn { get; set; }
        public DateTime? Regierungsende { get; set; }
    }

    public static SalesDatabase FromConnectionString(string connectionString, bool logging = false)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        if (logging)
        {
            settings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    // Bei update Statements geben wir die Anweisung aus, wie wir sie in
                    // der Shell eingeben könnten.
                    if (e.Command.TryGetValue("updates", out var updateCmd))
                    {
                        var collection = e.Command.GetValue("update");
                        var isUpdateOne = updateCmd[0]["q"].AsBsonDocument.Contains("_id");
                        foreach (var cmd in updateCmd.AsBsonArray)
                        {
                            Console.WriteLine($"db.getCollection(\"{collection}\").{(isUpdateOne ? "updateOne" : "updateMany")}({updateCmd[0]["q"]}, {updateCmd[0]["u"]})");
                        }
                    }
                    // Bei aggregate Statements geben wir die Anweisung aus, wie wir sie in
                    // der Shell eingeben könnten.
                    if (e.Command.TryGetValue("aggregate", out var aggregateCmd))
                    {
                        var collection = aggregateCmd.AsString;
                        Console.WriteLine($"db.getCollection(\"{collection}\").aggregate({e.Command["pipeline"]})");
                    }

                    // Bei Filter Statements geben wir die find Anweisung aus.
                    if (e.Command.TryGetValue("find", out var findCmd))
                    {
                        var collection = findCmd.AsString;
                        Console.WriteLine($"db.getCollection(\"{collection}\").find({e.Command["filter"]})");
                    }
                });
            };
        }
        var client = new MongoClient(settings);
        var db = client.GetDatabase("salesDb");
        // LowerCase property names.
        var conventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(ignoreIfNull: true)
            };
        ConventionRegistry.Register(nameof(CamelCaseElementNameConvention), conventions, _ => true);
        return new MonarchDatabase(client, db);
    }

    
    

}
public class DynastyContext : DbContext
{
    public DbSet<Dynastie> Dynasties { get; set; }
    public DbSet<Monarch> Monarchs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=dynasty.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Monarch>()
            .HasOne(m => m.Dynastie)
            .WithMany(d => d.Monarchen)
            .HasForeignKey(m => m.DynastieId);
    }
}

public class MongoDynastyService
{
    private readonly IMongoCollection<MongoDynastie> _dynasties;

    public MongoDynastyService()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        var database = client.GetDatabase("DynastyDB");
        _dynasties = database.GetCollection<MongoDynastie>("Dynasties");
    }

    public void AddDynasty(MongoDynastie dynasty)
    {
        _dynasties.InsertOne(dynasty);
    }

    public List<MongoDynastie> GetAllDynasties()
    {
        return _dynasties.Find(d => true).ToList();
    }

    public MongoDynastie GetDynastyById(ObjectId id)
    {
        return _dynasties.Find(d => d.Id == id).FirstOrDefault();
    }

    public void UpdateDynasty(ObjectId id, MongoDynastie updatedDynasty)
    {
        _dynasties.ReplaceOne(d => d.Id == id, updatedDynasty);
    }

    public void DeleteDynasty(ObjectId id)
    {
        _dynasties.DeleteOne(d => d.Id == id);
    }
}



