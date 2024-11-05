// See https://aka.ms/new-console-template for more information

using DbiProject.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;


var options = new DbContextOptionsBuilder()
            .UseSqlite("Data Source = damage.db")
            .LogTo(Console.WriteLine)
            .Options
            ;

ProjectContext db = new ProjectContext(options);
db.Database.EnsureDeleted();
db.Database.EnsureCreated();

db.Damages.Add(new Damage(new Room("classroom", new Roomnumber("A", "2", 14)), "broken window"));
db.SaveChanges();

var d = db.Damages.FirstOrDefault();
Console.WriteLine(d?.RepairStatus);


