using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace MyStore.Models
{
    public class Store
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
    class StoreDb : DbContext
    {
        public StoreDb(DbContextOptions options) : base(options) { }
        public DbSet<Store> Stores { get; set; } = null!;
    }
}