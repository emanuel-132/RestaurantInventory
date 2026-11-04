using System.ComponentModel.DataAnnotations;

namespace RestaurantInventory.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public required string Nombre { get; set; }
        
        public int CantidadOptima { get; set; }
        public int PuntoReorden { get; set; }
        
        // Vital para la tablilla de sensores de peso
        public float PesoPorUnidadGramos { get; set; }
        
        // 1 = QR (Cámara) + Peso, 2 = Solo Peso
        public int TipoMonitoreo { get; set; }

        public ICollection<LoteExpiracion> Lotes { get; set; } = new List<LoteExpiracion>();
    }
}