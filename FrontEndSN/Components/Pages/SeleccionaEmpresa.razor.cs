using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Text.Json;
using static FrontEndSN.Components.Pages.Login;
using static System.Net.WebRequestMethods;

namespace FrontEndSN.Components.Pages
{
	public partial class SeleccionaEmpresa
	{
		private List<EmpresaAcceso> MisEmpresas = new();

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				// Leemos el JSON que guardamos en el login
				var json = await JS.InvokeAsync<string>("localStorage.getItem", "EmpresasTemporales");

				if (!string.IsNullOrEmpty(json))
				{
					MisEmpresas = JsonSerializer.Deserialize<List<EmpresaAcceso>>(json);
					StateHasChanged(); // Refresca la pantalla para mostrar las empresas
				}
			}
		}

		private async Task Entrar(int idEmpresa)
		{
			try
			{

				var tokenViejo = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

			
				var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7241/api/acceso/SeleccionarEmpresa");

				// Agregar el token actual al Header
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenViejo);

	
				request.Content = JsonContent.Create(idEmpresa);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<Response>();

					if (result != null && !string.IsNullOrEmpty(result.token))
					{
						// Reemplazamos el token viejo por el nuevo en el localStorage
						await JS.InvokeVoidAsync("localStorage.setItem", "Token", result.token);

						//  Notificar al AuthProvider y saltar al Dashboard
						var customProvider = (CustomAuthStateProvider)AuthProvider;
						customProvider.NotifyUserAuthenticated(result.token);

					    //	await JS.InvokeVoidAsync("localStorage.removeItem", "EmpresasTemporales"); 
						NavigationManager.NavigateTo("/dashboard",forceLoad:true);
					}
				}
				else
				{
					Console.WriteLine("Error al seleccionar empresa");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
			}
		}

		private class Response
		{
			public bool isSuccess { get; set; }
			public string? token { get; set; }
		}
		public class EmpresaAcceso
		{
			public int Id { get; set; }
			public string Nombre { get; set; }
			public string Rfc { get; set; }
		}

	}
}
