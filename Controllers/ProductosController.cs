using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Data;
using RestaurantInventory.Models;

namespace RestaurantInventory.Controllers
{
    public class ProductosController : Controller
    {
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var catalogo = await _context.Productos
                .Select(p => new ProductoCatalogoViewModel
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    // Extraemos la cantidad actual desde la tabla de inventarios
                    CantidadActual = _context.Inventarios.Where(i => i.ProductoId == p.Id).Select(i => i.CantidadEstimada).FirstOrDefault(),
                    CantidadOptima = p.CantidadOptima,
                    PuntoReorden = p.PuntoReorden,
                    TipoMonitoreo = p.TipoMonitoreo,
                    FechaExpiracion = _context.Inventarios
                        .Where(i => i.ProductoId == p.Id)
                        .Select(i => i.FechaExpiracionMasProxima)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(catalogo);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Añadimos los parámetros CantidadInicial y FechaExpiracion para recibirlos del formulario HTML
        public async Task<IActionResult> Create([Bind("Nombre,CantidadOptima,PuntoReorden,PesoPorUnidadGramos,TipoMonitoreo")] Producto producto, int CantidadInicial, DateTime? FechaExpiracion)
        {
            if (ModelState.IsValid)
            {
                // 1. Guardar el producto base primero para que PostgreSQL le asigne un ID
                _context.Add(producto);
                await _context.SaveChangesAsync(); 

                // 2. Si el usuario ingresó inventario inicial, crear el registro en InventarioActual
                if (CantidadInicial > 0 || FechaExpiracion.HasValue)
                {
                    var inventario = new InventarioActual
                    {
                        ProductoId = producto.Id,
                        CantidadEstimada = CantidadInicial,
                        UltimaLecturaPesoGramos = CantidadInicial * producto.PesoPorUnidadGramos,
                        UltimaActualizacion = DateTime.UtcNow,
                        FechaExpiracionMasProxima = FechaExpiracion.HasValue ? FechaExpiracion.Value.ToUniversalTime() : null
                    };
                    _context.Inventarios.Add(inventario);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null) return NotFound();

            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}