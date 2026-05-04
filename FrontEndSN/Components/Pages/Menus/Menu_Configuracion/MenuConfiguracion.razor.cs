using FrontEndSN.Clases;

namespace FrontEndSN.Components.Pages.Menus.Menu_Configuracion
{
	public partial class MenuConfiguracion
	{
		private List<MenuOpcion> listaConfiguraciones = new()
		{
			new MenuOpcion { Titulo = "Empresa", Descripcion = "Definición de datos generales de la empresa", Ruta = "/configuracion_empresa", Icono = "bi bi-building-fill-gear", Color = "#0d6efd" },
			new MenuOpcion { Titulo = "Mi Usuario", Descripcion = "Datos generales de tu usuario.", Ruta = "/configuracion_usuario", Icono = "bi bi-person-fill-gear", Color = "#6610f2" }
			
		};
	}
}
