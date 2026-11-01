using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantInventory.Models
{
    public class LoteExpiracion
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("Producto")]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        
        [Required, MaxLength(50)]
        public required string CodigoQR { get; set; }
        public DateTime FechaExpiracion { get; set; }
        
        // Posibles estados: "Activo", "Usado", "Expirado"
        [MaxLength(20)]
        public string Estado { get; set; } = "Activo";
    }
}