using FrontEndSN.Components.Pages.Menus.Catalogos;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Drawing.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Menu_Configuracion
{
	public partial class ConfiguracionUsuario
	{

		private bool Cargando = true;
		private UsuarioConfiguracion Modelo = new ();
		private string ConfirmarContrasena = "";

		private bool verActual = false;
		private bool verNueva = false;
		private bool verConfirmar = false;


		protected override async Task OnInitializedAsync()
		{

			await CargarInfo();

			Cargando = false;
			StateHasChanged();
		}

		private async Task CargarInfo()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (string.IsNullOrEmpty(token))
				{

					Console.WriteLine("No se encontró el token de autenticación.");
					Nav.NavigateTo("/login");
					return;
				}

				var request = new HttpRequestMessage(HttpMethod.Get, "api/acceso/ObtenerPerfil");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<UsuarioConfiguracion>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);
					Modelo = resultado.Data ?? new();

				}
				else
				{
					Console.WriteLine($"Error en la petición: {response.StatusCode}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error de conexión: {ex.Message}");
			}
			finally
			{

				StateHasChanged();
			}
		}

		private async Task GuardarCambios()
		{
			try
			{
				if (Modelo.NuevaContrasena != ConfirmarContrasena)
				{ 
					await JS.InvokeVoidAsync("notifier.error", "Los campos de nueva contraseña no coinciden, verifique e intentelo de nuevo.");
					return;
				}

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				string url = $"api/Acceso/Editar/{Modelo.id}";
				var metodo = HttpMethod.Post;

				var request = new HttpRequestMessage(metodo, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				request.Content = JsonContent.Create(Modelo);
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<object>>();

					if (resultado != null && resultado.IsSuccess)
					{
						string mensajeExito ="Perfil actualizado correctamente";
						await JS.InvokeVoidAsync("notifier.success", mensajeExito);

						cerrar_sesion();

					}
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();

					string mensajePrincipal = errorData?.Message ?? "Error inesperado";
					string detalleTecnico = errorData?.Details ?? "No hay detalles adicionales.";
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar la información: " + mensajePrincipal + " " + detalleTecnico);
				}
			}
			catch (Exception ex)
			{

				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.InnerException);
			}
		}


		private async Task ConfirmarGuardar()
		{
	
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
				"Actualizar Perfil",
				"¿Deseas guardar los cambios? Se cerrará la sesión actual.");

			if (confirmed)
			{
				await GuardarCambios();
			}
		}

		private async void cerrar_sesion()
		{
			await JS.InvokeVoidAsync("localStorage.removeItem", "Token");
			await JS.InvokeVoidAsync("localStorage.removeItem", "EmpresasTemporales");
			Modelo = new();
			Nav.NavigateTo("/login", forceLoad: true);
			StateHasChanged();
		}


		public class UsuarioConfiguracion
		{
			public int id { get; set; }


			public string NombreCompleto { get; set; }
			public string NombreUsuario { get; set; }

			public string Correo { get; set; }
			public string Telefono { get; set; }
			
			public string? Contrasena { get; set; }
			public string? NuevaContrasena { get; set; }
		
		
		
		}

	}
}
