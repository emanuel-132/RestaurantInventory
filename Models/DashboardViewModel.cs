namespace RestaurantInventory.Models
{
    // Modelo para el Home (Dashboard)
    public class DashboardViewModel
    {
        public int TotalProductos { get; set; }
        public int AlertasStock { get; set; }
        public List<AlertaInventario> ProductosCriticos { get; set; } = new();
        public List<AlertaExpiracion> ProductosPorExpirar { get; set; } = new(); 
    }

    public class AlertaInventario
    {
        public string NombreProducto { get; set; } = string.Empty;
        public int CantidadActual { get; set; }
        public int PuntoReorden { get; set; }
    }

    public class AlertaExpiracion 
    {
        public string NombreProducto { get; set; } = string.Empty;
        public DateTime FechaExpiracion { get; set; }
    }

    // Modelo para llevar los datos cruzados al Catálogo
    public class ProductoCatalogoViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadActual { get; set; }
        public int CantidadOptima { get; set; }
        public int PuntoReorden { get; set; }
        public int TipoMonitoreo { get; set; }
        public DateTime? FechaExpiracion { get; set; }
    }
}