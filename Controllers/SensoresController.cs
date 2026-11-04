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


[HttpPost("salida")]
public async Task<IActionResult> RemoverQr([FromBody] SensorPayload payload)
{
    Producto? producto = null;
    LoteExpiracion? cajaFisica = null;

    // 1. TIPO 1: Salida de una caja física mediante QR
    if (!string.IsNullOrEmpty(payload.CodigoQR))
    {
        cajaFisica = await _context.Lotes
            .Include(u => u.Producto)
            .FirstOrDefaultAsync(u => u.CodigoQR == payload.CodigoQR);

        if (cajaFisica == null)
        {
            return NotFound(new { mensaje = "Código QR no reconocido." });
        }

        producto = cajaFisica.Producto;

        if (cajaFisica.Estado == "Usado")
        {
            return BadRequest(new { mensaje = "Esta caja ya fue registrada como usada." });
        }

        // Marcamos la caja como usada
        cajaFisica.Estado = "Usado";

        await _context.SaveChangesAsync();

        // Actualizamos el inventario contando las cajas activas
        var inventario = await _context.Inventarios
            .FirstOrDefaultAsync(i => i.ProductoId == producto.Id);

        if (inventario != null)
        {
            inventario.CantidadEstimada = await _context.Lotes
                .CountAsync(u => u.ProductoId == producto.Id && u.Estado == "Activo");

            inventario.UltimaActualizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }

    // 2. TIPO 2: Salida a granel mediante sensor de peso
    else if (payload.ProductoId.HasValue)
    {
        producto = await _context.Productos
            .FindAsync(payload.ProductoId.Value);

        if (producto == null)
        {
            return NotFound(new { mensaje = "Producto no reconocido." });
        }

        if (producto.PesoPorUnidadGramos <= 0)
        {
            return BadRequest(new {
                mensaje = "El producto no tiene un peso por unidad configurado."
            });
        }

        // Calculamos cuántas unidades quedan según el peso actual
        int cantidadCalculada = (int)Math.Floor(
            payload.PesoTotalGramos / producto.PesoPorUnidadGramos
        );

        var inventario = await _context.Inventarios
            .FirstOrDefaultAsync(i => i.ProductoId == producto.Id);

        if (inventario == null)
        {
            return NotFound(new {
                mensaje = "No existe un registro de inventario para este producto."
            });
        }

        // Actualizamos la cantidad según el peso detectado
        inventario.CantidadEstimada = cantidadCalculada;
        inventario.UltimaLecturaPesoGramos = payload.PesoTotalGramos;
        inventario.UltimaActualizacion = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
    else
    {
        return BadRequest(new {
            mensaje = "Debe proporcionar un Código QR o un ProductoId."
        });
    }

    return Ok(new
    {
        mensaje = cajaFisica != null
            ? "Salida de caja física registrada correctamente."
            : "Salida a granel registrada correctamente.",

        producto = producto!.Nombre,
        estadoCaja = cajaFisica?.Estado
    });
}


        [HttpPost("lectura")]
        public async Task<IActionResult> RegistrarLectura([FromBody] SensorPayload payload)
        {
            Producto? producto = null;
            LoteExpiracion? cajaFisica = null;

            // 1. TIPO 1: Monitoreo por Cámara (Se recibió un Código QR)
            if (!string.IsNullOrEmpty(payload.CodigoQR))
            {
                // Cambiado a _context.Lotes
                cajaFisica = await _context.Lotes
                                           .Include(u => u.Producto)
                                           .FirstOrDefaultAsync(u => u.CodigoQR == payload.CodigoQR);
                
                if (cajaFisica != null) 
                {
                    producto = cajaFisica.Producto;
                    
                    if (payload.PesoTotalGramos < 50 && cajaFisica.Estado == "Activo")
                    {
                        cajaFisica.Estado = "Usado";
                    }
                    else if (payload.PesoTotalGramos >= 50 && cajaFisica.Estado == "Usado")
                    {
                        cajaFisica.Estado = "Activo";
                    }
                }
            }
            // 2. TIPO 2: Monitoreo Solo Peso (Se recibió un ID de producto directo, sin QR)
            else if (payload.ProductoId.HasValue)
            {
                producto = await _context.Productos.FindAsync(payload.ProductoId.Value);
            }

            if (producto == null)
                return NotFound(new { mensaje = "Producto o QR no reconocido por el sistema." });

            // 3. Lógica matemática a granel (Tu código original para Inventarios)
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
                // Si leímos un QR, no sobreescribimos la CantidadEstimada global con la matemática de una sola caja,
                // solo actualizamos el registro de peso. La matemática a granel se reserva para Tipo 2.
                if (string.IsNullOrEmpty(payload.CodigoQR)) 
                {
                    inventario.CantidadEstimada = cantidadCalculada;
                }
                
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
                mensaje = cajaFisica is not null ? "Lectura de caja individual registrada" : "Lectura a granel registrada", 
                producto = producto.Nombre,
                estadoCaja = cajaFisica?.Estado,
                cantidadGranel = string.IsNullOrEmpty(payload.CodigoQR) ? cantidadCalculada : (int?)null
            });
        }
    }
}