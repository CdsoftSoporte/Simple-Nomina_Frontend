using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FrontEndSN.Components.Modales
{
	public partial class AgregarEmpleado:VentanaBase
	{
		private int IdEmpleadoSeleccionado = 0;
		private int IdPeriodoSeleccionado = 0;
		private bool IsVisible { get; set; } = false;

		[Parameter] public EventCallback OnEmpleadoAgregado { get; set; }


		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(
							CargarEmpleadosParaCombos()
						);
		}

		public void Mostrar(int idperiodo)
		{
			IsVisible = true;
			IdPeriodoSeleccionado=idperiodo;
			IdEmpleadoSeleccionado = 0;
			StateHasChanged();
		}

		public void Cerrar()
		{
			IsVisible = false;
			StateHasChanged();
		}

		private async Task Guardar(int IdPeriodoSeleccionado)
		{
			try
			{
				if (IdEmpleadoSeleccionado == 0)
					return;

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url=$"api/Periodos/AgregarEmpleadoAPeriodo/{IdPeriodoSeleccionado}/{IdEmpleadoSeleccionado}";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success","Empleado agregado exitosamente.");
					await OnEmpleadoAgregado.InvokeAsync();
					Cerrar();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				StateHasChanged(); 
			}
		}


		private async Task OnEmpleadoChanged(int id)
		{
			IdEmpleadoSeleccionado = id;
			
		}


	}
}
