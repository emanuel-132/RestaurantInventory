using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Models;

namespace RestaurantInventory.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Estas propiedades DbSet le dicen a EF que cree tablas para cada modelo
        public DbSet<Producto> Productos { get; set; }
        public DbSet<InventarioActual> Inventarios { get; set; }
        public DbSet<LoteExpiracion> Lotes { get; set; }
        public DbSet<ListaCompra> Compras { get; set; }
    }
}