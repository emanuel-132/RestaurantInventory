using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantInventory.Models
{
    public class ListaCompra
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("Producto")]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        
        public int CantidadFaltante { get; set; }
        
        // "Stock Bajo" o "Expiración"
        [MaxLength(50)]
        public string Motivo { get; set; } = "";
        public DateTime FechaAgregado { get; set; }
    }
}