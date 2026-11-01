namespace RestaurantInventory.Models
{
    public class CompraViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadOptima { get; set; }
        public int PuntoReorden { get; set; }
        public int CantidadActual { get; set; }
        public int CantidadComprar { get; set; }
        public DateTime? FechaExpiracion { get; set; }
    }
}