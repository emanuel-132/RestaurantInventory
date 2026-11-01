using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantInventory.Models
{
    public class InventarioActual
    {
        [Key]
        public int Id { get; set; }
        
        [ForeignKey("Producto")]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }
        
        public int CantidadEstimada { get; set; }
        public float UltimaLecturaPesoGramos { get; set; }
        public DateTime UltimaActualizacion { get; set; }
        public DateTime? FechaExpiracionMasProxima { get; set; }
    }
}