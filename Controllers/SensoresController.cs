using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Data;
using RestaurantInventory.Models;

namespace RestaurantInventory.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SensoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SensoresController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("lectura")]
        public async Task<IActionResult> RegistrarLectura([FromBody] SensorPayload payload)
        {
            Producto? producto = null;

            if (!string.IsNullOrEmpty(payload.CodigoQR))
            {
                var lote = await _context.Lotes.Include(l => l.Producto)
                                               .FirstOrDefaultAsync(l => l.CodigoQR == payload.CodigoQR);
                if (lote != null) producto = lote.Producto;
            }
            else if (payload.ProductoId.HasValue)
            {
                producto = await _context.Productos.FindAsync(payload.ProductoId.Value);
            }

            if (producto == null)
                return NotFound(new { mensaje = "Producto o QR no reconocido por el sistema." });

            if (producto.PesoPorUnidadGramos <= 0)
                return BadRequest(new { mensaje = "El producto no tiene un peso por unidad configurado para hacer la matemática." });

            int cantidadCalculada = (int)Math.Floor(payload.PesoTotalGramos / producto.PesoPorUnidadGramos);
            var inventario = await _context.Inventarios.FirstOrDefaultAsync(i => i.ProductoId == producto.Id);
            
            if (inventario == null)
            {
                inventario = new InventarioActual
                {
                    ProductoId = producto.Id,
                    CantidadEstimada = cantidadCalculada,
                    UltimaLecturaPesoGramos = payload.PesoTotalGramos,
                    UltimaActualizacion = DateTime.UtcNow,
                    FechaExpiracionMasProxima = payload.FechaExpiracion.HasValue ? payload.FechaExpiracion.Value.ToUniversalTime() : null
                };
                _context.Inventarios.Add(inventario);
            }
            else
            {
                inventario.CantidadEstimada = cantidadCalculada;
                inventario.UltimaLecturaPesoGramos = payload.PesoTotalGramos;
                inventario.UltimaActualizacion = DateTime.UtcNow;
                
                if (payload.FechaExpiracion.HasValue)
                {
                    inventario.FechaExpiracionMasProxima = payload.FechaExpiracion.Value.ToUniversalTime();
                }
                
                _context.Inventarios.Update(inventario);
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                mensaje = "Lectura registrada exitosamente", 
                producto = producto.Nombre,
                cantidadActual = cantidadCalculada
            });
        }
    }
}