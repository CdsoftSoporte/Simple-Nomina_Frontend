using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace FrontEndSN.Components.Pages
{
	public partial class Dashboard
	{
		private string UserName { get; set; } = "Usuario";

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				await ObtenerNombreDesdeToken();
				StateHasChanged(); // Forzamos el redibujado con el nombre real
			}
		}

		private async Task ObtenerNombreDesdeToken()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (!string.IsNullOrEmpty(token))
				{
					var handler = new JwtSecurityTokenHandler();
					var jwtToken = handler.ReadJwtToken(token);

					// Intentamos buscar el claim de nombre (común en JWT)
					var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "Nombre" || c.Type == "Nombre");

					if (nameClaim != null)
					{
						UserName = nameClaim.Value;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error al leer el token: {ex.Message}");
				UserName = "Usuario";
			}
		}

		private List<Modulo> modulos = new List<Modulo>
		{
			new Modulo { Titulo = "Catálogos", Descripcion = "Gestión de catálogos generales como empleados,turnos, puestos, etc.", Icono = "oi oi-list", Ruta = "/menu_catalogos" },			
			new Modulo { Titulo = "Cálculo", Descripcion = "Administración de periodos, cálculo, monitoreo, etc.", Icono = "oi oi-calculator", Ruta = "/menu_calculo_nomina" },
			new Modulo { Titulo = "Timbrado", Descripcion = "Timbrar periodos de nómina.", Icono = "oi oi-globe", Ruta = "/logout" },
			new Modulo { Titulo = "Configuración", Descripcion = "Ajustes de la empresa y usuario.", Icono = "oi oi-cog", Ruta = "/menu_configuracion" },
			new Modulo { Titulo = "Selecciona Empresa", Descripcion = "Cambiar la empresa en la que se está trabajando.", Icono = "oi oi-hard-drive", Ruta = "/selecciona_empresa" },
			new Modulo { Titulo = "Cerrar Sesión", Descripcion = "Salir de forma segura del sistema.", Icono = "oi oi-account-logout", Ruta = "/logout" }
		

		};

		private void Navegar(string ruta)
		{
			if (ruta == "/logout")
			{
				cerrar_sesion();
			}

			NavigationManager.NavigateTo(ruta, forceLoad: false);
		}

		private async void cerrar_sesion()
		{
			await JS.InvokeVoidAsync("localStorage.removeItem", "Token");
			await JS.InvokeVoidAsync("localStorage.removeItem", "EmpresasTemporales");
			NavigationManager.NavigateTo("/login",forceLoad:true);
		}

		public class Modulo
		{
			public string Titulo { get; set; } = "";
			public string Descripcion { get; set; } = "";
			public string Icono { get; set; } = "";
			public string Ruta { get; set; } = "";
		}


	}
}
