using FrontEndSN.Clases;

namespace FrontEndSN.Components.Pages.Menus.Menu_Especiales
{
	public partial class MenuEspeciales
	{
		private List<MenuOpcion> listaCatalogos = new()
		{

			new MenuOpcion { Titulo = "Generar archivos para SUA", Descripcion = "Generar archivos de importación de movimientos al SUA", Ruta = "/generar_archivos_sua", Icono = "bi bi-universal-access", Color = "#0d6efd" },
			new MenuOpcion { Titulo = "Generar archivos para IDSE", Descripcion = "Generar archivos de importación de movimientos al IDSE", Ruta = "/generar_archivos_sua", Icono = "bi bi-diagram-3-fill", Color = "#6610f2" },
			new MenuOpcion { Titulo = "Cálculo de Aumentos Salariales", Descripcion = "Calcular y generar aumentos salariales masivos.", Ruta = "/calculo_aumentos_salariales", Icono = "bi bi bi-cash-coin", Color = "#d63384" },
		//	new MenuOpcion { Titulo = "Cálculo", Descripcion = "Calcular y visualizar periodos de nómina.", Ruta = "/calculo_nomina", Icono = "bi bi-calculator-fill", Color = "#198754" },
		//	new MenuOpcion { Titulo = "Periodos Manuales", Descripcion = "Captura y edita totalmente un periodo de nomina", Ruta = "/periodos_manuales", Icono = "bi bi-pencil-square", Color = "#fd7e14" },
		//	new MenuOpcion { Titulo = "Calculo de Aguinaldos", Descripcion = "Cálculo masivo de aguinaldos.", Ruta = "/calculo_aguinaldos", Icono = "bi bi-cash-stack", Color = "#20c997" },
		};
	}

}



