using FrontEndSN.Clases;

namespace FrontEndSN.Components.Pages.Menus.Menu_Reportes
{
	public partial class MenuReportes
	{
		private List<MenuOpcion> listaReportes = new()
		{

			new MenuOpcion { Titulo = "Kardex de vacaciones", Descripcion = "Estado de cuenta de vacaciones de empleados.", Ruta = "/kardex_vacaciones", Icono = "bi bi-calendar-range-fill", Color = "#0d6efd" },
			//new MenuOpcion { Titulo = "Incidencias Variables", Descripcion = "Definición de conceptos que aplican a un periodo.", Ruta = "/incidencias_variables", Icono = "bi bi-clipboard-minus-fill", Color = "#6610f2" },
			//new MenuOpcion { Titulo = "Repetitivos", Descripcion = "Definición de conceptos que aplicarán a varios periodos.", Ruta = "/departamentos", Icono = "bi bi-recycle", Color = "#d63384" },
			//new MenuOpcion { Titulo = "Cálculo", Descripcion = "Calcular y visualizar periodos de nómina.", Ruta = "/calculo_nomina", Icono = "bi bi-calculator-fill", Color = "#198754" },
			//new MenuOpcion { Titulo = "Periodos Manuales", Descripcion = "Captura y edita totalmente un periodo de nomina", Ruta = "/periodos_manuales", Icono = "bi bi-pencil-square", Color = "#fd7e14" },			
			//new MenuOpcion { Titulo = "Salarios Mínimos", Descripcion = "Histórico de salarios por ley.", Ruta = "/salarios_minimos", Icono = "bi bi-currency-exchange", Color = "#20c997" },

		};
	}
}
