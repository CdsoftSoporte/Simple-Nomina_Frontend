using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace FrontEndSN.Components.Pages
{
	public partial class Login
	{


		private LoginModel loginModel = new();
		private bool isLoading = false;
		private string errorMessage = "";

		protected override async Task OnInitializedAsync()
		{
			var state = await AuthProvider.GetAuthenticationStateAsync();
			var user = state.User;
			if (state.User.Identity?.IsAuthenticated == true)
			{
				//	NavigationManager.NavigateTo("/", forceLoad: false);
				var tieneEmpresa = user.FindFirst(c => c.Type == "EmpresaIdActiva") != null;

				if (tieneEmpresa)
				{
					// Si ya tiene empresa en el token, lo mandamos al dashboard principal
					NavigationManager.NavigateTo("/", forceLoad: false);
				}
				else
				{
					// Si está autenticado pero NO tiene empresa elegida, lo mandamos a seleccionar
					NavigationManager.NavigateTo("/Selecciona_Empresa");
				}
			}
		}

		private async Task inicia_sesion()
		{
			errorMessage = string.Empty;
			isLoading = true;
			try
			{
				var response = await Http.PostAsJsonAsync("api/acceso/login", loginModel);
				if (response.IsSuccessStatusCode)
				{
					var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
					if (result != null && !string.IsNullOrEmpty(result.Token))
					{
						await JS.InvokeVoidAsync("localStorage.setItem", "Token", result.Token);
						var empresasJson = System.Text.Json.JsonSerializer.Serialize(result.Accesos_Empresas);
						//metemos las empresas a las que tiene acceso el usuario en localstorage para que las pueda cachar el Selecciona_Empresa
						await JS.InvokeVoidAsync("localStorage.setItem", "EmpresasTemporales", empresasJson);

						var customProvider = (CustomAuthStateProvider)AuthProvider;
						customProvider.NotifyUserAuthenticated(result.Token);

						NavigationManager.NavigateTo("/Selecciona_Empresa");
					}
				}
				else
				{
					errorMessage = "Usuario o contraseña incorrectos.";
					isLoading = false;
				}
			}
			catch (Exception ex)
			{
				errorMessage = "Error al conectar con el servidor. Intente más tarde." + ex.InnerException;
			}
			finally
			{
				isLoading = false;
				StateHasChanged();
			}
		}

		public async void manda_inicia_sesion(KeyboardEventArgs e)
		{
			if (e.Key == "Enter" && !isLoading)
			{
				await inicia_sesion();
			}
		}

		private void ForgotPassword()
		{
			NavigationManager.NavigateTo("/forgot-password");
		}

		public class LoginModel
		{
			public string nombre_usuario { get; set; } = String.Empty;
			public string contrasena { get; set; } = String.Empty;
		}

		private class LoginResponse
		{
			public bool isSuccess { get; set; }
			public string? Token { get; set; }
			public List<EmpresaAcceso> Accesos_Empresas { get; set; }
		}
		public class EmpresaAcceso
		{
			public int Id { get; set; }
			public string Nombre { get; set; }
			public string Rfc { get; set; }
		}
	
	}
}