using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Data;
using RestaurantInventory.Models;

namespace RestaurantInventory.Controllers
{
    public class ListaComprasController : Controller
    {
        private readonly AppDbContext _context;

        public ListaComprasController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var resumenCompras = await _context.Productos
                .Select(p => new CompraViewModel
                {
                    ProductoId = p.Id,
                    Nombre = p.Nombre,
                    CantidadOptima = p.CantidadOptima,
                    PuntoReorden = p.PuntoReorden,
                    CantidadActual = _context.Inventarios
                        .Where(i => i.ProductoId == p.Id)
                        .Select(i => i.CantidadEstimada)
                        .FirstOrDefault(),
                    FechaExpiracion = _context.Inventarios
                        .Where(i => i.ProductoId == p.Id)
                        .Select(i => i.FechaExpiracionMasProxima)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Lógica de negocio: Filtrar y calcular
            var listaFiltrada = resumenCompras
                .Where(item => 
                    item.CantidadActual <= item.PuntoReorden || 
                    (item.FechaExpiracion.HasValue && item.FechaExpiracion.Value <= DateTime.Now.AddDays(3))
                )
                .Select(item => {
                    // Si expira pronto, forzamos la compra completa porque lo actual se botará
                    bool expiraPronto = item.FechaExpiracion.HasValue && item.FechaExpiracion.Value <= DateTime.Now.AddDays(3);
                    
                    item.CantidadComprar = expiraPronto ? item.CantidadOptima : item.CantidadOptima - item.CantidadActual;
                    
                    // Añadimos una alerta visual al nombre si está por expirar
                    if (expiraPronto) item.Nombre += " (Expira Pronto)";
                    
                    return item;
                })
                .ToList();

            return View(listaFiltrada);
        }
    }
}