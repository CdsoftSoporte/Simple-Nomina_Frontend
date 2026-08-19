using FrontEndSN.Components.Modales;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class CentrosCostos
	{

		private bool cargando = false;
		private AgregarCentroCostos ModalCentroCostos { get; set; }

		List<CentrosDeCostos> ListaCentros;

		protected CentrosDeCostos CentroModel = new();

		protected override async Task OnInitializedAsync()
		{
			await CargarCentrosDeCostos();
		}



		private async Task CargarCentrosDeCostos()
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

				var request = new HttpRequestMessage(HttpMethod.Get, "api/CentroDeCostos/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<CentrosDeCostos>>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);
					ListaCentros = resultado.Data ?? new();

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

		private void AbrirModal()
		{
			ModalCentroCostos.Mostrar();
		}

		private void AbrirModal(CentrosDeCostos centro)
		{
			ModalCentroCostos.Mostrar(centro.Id,centro.Nombre,centro.Descripcion);
		}

		private async Task ConfirmarEliminar(CentrosDeCostos item)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			 $"¿Deseas borrar el centro de costos {item.Nombre}? Esta acción no se puede deshacer");
			if (confirmed)
			{
				await Eliminar(item.Id);
				await CargarCentrosDeCostos();
			}
		}

		private async Task Eliminar(int id)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Delete, $"api/CentroDeCostos/Borrar/{id}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{

					if (response.IsSuccessStatusCode)
					{
						await JS.InvokeVoidAsync("notifier.success", "Registro eliminado correctamente");
					}
					else
					{
						await JS.InvokeVoidAsync("notifier.error", response.Content);
					}

					await CargarCentrosDeCostos();
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

	}

	public class CentrosDeCostos
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }
		
		[JsonPropertyName("nombre")]
		public string Nombre { get; set; }

		[JsonPropertyName("descripcion")]
		public string Descripcion { get; set; }
	}

	public class CentrosDeCostosItem
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("nombre")]
		public string Nombre { get; set; }

		public string NombreCompleto => $"{Id} - {Nombre}";
	}



}
