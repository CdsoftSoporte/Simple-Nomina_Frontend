using FrontEndSN.Clases;

namespace FrontEndSN.Components.Pages.Menus.Menu_Especiales
{
	public partial class MenuEspeciales
	{
		private List<MenuOpcion> listaCatalogos = new()
		{

			new MenuOpcion { Titulo = "Generar archivos para SUA", Descripcion = "Generar archivos de importación de movimientos al SUA", Ruta = "/generar_archivos_sua", Icono = "bi bi-universal-access", Color = "#0d6efd" },

		};
	}

}



