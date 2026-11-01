namespace RestaurantInventory.Models
{
    public class SensorPayload
    {
        // Puede enviar el ID directo (Bandeja análoga)
        public int? ProductoId { get; set; } 
        
        // O puede enviar el QR leído por la cámara
        public string? CodigoQR { get; set; } 
        
        // La lectura del sensor HX711
        public float PesoTotalGramos { get; set; }

        public DateTime? FechaExpiracion { get; set; }
    }
}