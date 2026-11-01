using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantInventory.Data;
using RestaurantInventory.Models;
using System.Diagnostics;

namespace RestaurantInventory.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = new DashboardViewModel();

            dashboardData.TotalProductos = await _context.Productos.CountAsync();

            var inventarios = await _context.Inventarios
                .Include(i => i.Producto)
                .ToListAsync();

            foreach (var inv in inventarios)
            {
                if (inv.Producto != null)
                {
                    // 1. Evaluar si cruzó el Punto de Reorden
                    if (inv.CantidadEstimada <= inv.Producto.PuntoReorden)
                    {
                        dashboardData.AlertasStock++;
                        dashboardData.ProductosCriticos.Add(new AlertaInventario
                        {
                            NombreProducto = inv.Producto.Nombre,
                            CantidadActual = inv.CantidadEstimada,
                            PuntoReorden = inv.Producto.PuntoReorden
                        });
                    }

                    // 2. Evaluar si expira en 3 días o menos
                    if (inv.FechaExpiracionMasProxima.HasValue && inv.FechaExpiracionMasProxima.Value <= DateTime.UtcNow.AddDays(3))
                    {
                        dashboardData.ProductosPorExpirar.Add(new AlertaExpiracion
                        {
                            NombreProducto = inv.Producto.Nombre,
                            FechaExpiracion = inv.FechaExpiracionMasProxima.Value.ToLocalTime()
                        });
                    }
                }
            }

            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}