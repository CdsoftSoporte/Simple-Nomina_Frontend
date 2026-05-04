using FrontEndSN.Clases;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class MenuCatalogos
	{


		private List<MenuOpcion> listaCatalogos = new()
        {
			new MenuOpcion { Titulo = "Puestos", Descripcion = "Definición de cargos y salarios base.", Ruta = "/puestos", Icono = "bi bi-briefcase-fill", Color = "#0d6efd" },
			new MenuOpcion { Titulo = "Departamentos", Descripcion = "Estructura organizacional de la empresa.", Ruta = "/departamentos", Icono = "bi bi-diagram-3-fill", Color = "#6610f2" },
			new MenuOpcion { Titulo = "Conceptos", Descripcion = "Percepciones y deducciones de nómina.", Ruta = "/conceptos", Icono = "bi bi-list-check", Color = "#d63384" },
			new MenuOpcion { Titulo = "Movimientos Afiliatorios", Descripcion = "Captura y consulta de movimientos de Imss de empleados.", Ruta = "/movimientos_imss", Icono = "bi bi-journal-bookmark-fill", Color = "#198754" },
			new MenuOpcion { Titulo = "Turnos", Descripcion = "Horarios y jornadas de trabajo.", Ruta = "/turnos", Icono = "bi bi-clock-history", Color = "#fd7e14" },
			new MenuOpcion { Titulo = "Salarios Mínimos", Descripcion = "Histórico de salarios por ley.", Ruta = "/salarios_minimos", Icono = "bi bi-currency-exchange", Color = "#20c997" },

			new MenuOpcion { Titulo = "UMA", Descripcion = "Histórico de Unidad de Medida y Actualización.", Ruta = "/uma", Icono = "bi bi-diamond", Color = "#0d6efd" },
			new MenuOpcion { Titulo = "UMI", Descripcion = "Histórica de Unidad Mixta Infonavit.", Ruta = "/umi", Icono = "bi bi-diamond-fill", Color = "#6610f2" },
			new MenuOpcion { Titulo = "Grupos de Prestaciones", Descripcion = "Grupo de prestaciones de Ley y personalizados.", Ruta = "/gruposprestaciones", Icono = "bi bi-clipboard2-heart", Color = "#d63384" },
			new MenuOpcion { Titulo = "Registros Patronales", Descripcion = "Catálogo y configuracion de Patrones.", Ruta = "/registros_patronales", Icono = "bi bi-people-fill", Color = "#198754" },			
			new MenuOpcion { Titulo = "Empleados", Descripcion = "Catálogo de empleados.", Ruta = "/empleados", Icono = "bi bi-person-badge-fill", Color = "#fd7e14" },
		};


	}
}
