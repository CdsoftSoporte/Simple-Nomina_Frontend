using FrontEndSN.Clases;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class MenuCalculoNomina
	{
		private List<MenuOpcion> listaCatalogos = new()
		{

			new MenuOpcion { Titulo = "Nóminas", Descripcion = "Catálogo de Nóminas y periodos de pago.", Ruta = "/nominas_y_periodos", Icono = "bi bi-credit-card-2-front-fill", Color = "#0d6efd" },
			new MenuOpcion { Titulo = "Incidencias Variables", Descripcion = "Definición de conceptos que aplican a un periodo.", Ruta = "/incidencias_variables", Icono = "bi bi-clipboard-minus-fill", Color = "#6610f2" },
			new MenuOpcion { Titulo = "Repetitivos", Descripcion = "Definición de conceptos que aplicarán a varios periodos.", Ruta = "/departamentos", Icono = "bi bi-recycle", Color = "#d63384" },
			new MenuOpcion { Titulo = "Cálculo", Descripcion = "Calcular periodos de nómina.", Ruta = "/conceptos", Icono = "bi bi-calculator-fill", Color = "#198754" },
		//	new MenuOpcion { Titulo = "Movimientos Afiliatorios", Descripcion = "Captura y consulta de movimientos de Imss de empleados.", Ruta = "/movimientos_imss", Icono = "bi bi-journal-bookmark-fill", Color = "#fd7e14" },			
		//	new MenuOpcion { Titulo = "Salarios Mínimos", Descripcion = "Histórico de salarios por ley.", Ruta = "/salarios_minimos", Icono = "bi bi-currency-exchange", Color = "#20c997" },

		};
	}
}
