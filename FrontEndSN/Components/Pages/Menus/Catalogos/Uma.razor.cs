using Azure;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Uma
	{

		private List<Umas> uma;

		protected Umas editModel = new();

		private bool isModalVisible = false;

		protected override async Task OnInitializedAsync()
		{
			await CargarDatos();
		}


		private async Task CargarDatos()
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

				var request = new HttpRequestMessage(HttpMethod.Get, "api/UMA/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<Umas>>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);
					uma = resultado.Data ?? new();

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


		private void MostrarModal(Umas? item = null)
		{
			if (item == null)
			{
				editModel = new Umas();
				editModel.a_partir_de = new DateOnly(DateTime.Now.Year, 1, 1);
				editModel.id = 0;
				editModel.importe = 0;
			}
			else
			{
				editModel = new Umas { id = item.id, a_partir_de = item.a_partir_de, importe = item.importe };
			}
			isModalVisible = true;
		}

		private async Task Guardar()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");


				bool esNuevo = editModel.id == 0;
				string url = esNuevo ? "api/UMA/Agregar" : $"api/UMA/Editar/{editModel.id}";
				var metodo = HttpMethod.Post;

				var request = new HttpRequestMessage(metodo, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				request.Content = JsonContent.Create(editModel);
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<object>>();

					if (resultado != null && resultado.IsSuccess)
					{

						isModalVisible = false;
						await CargarDatos();

						string mensajeExito = esNuevo ? "Registro creado con éxito" : "Registro actualizado correctamente";
						await JS.InvokeVoidAsync("notifier.success", mensajeExito);
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

		private async Task ConfirmarEliminar(Umas item)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			 $"¿Deseas borrar la UMA de {item.a_partir_de:dd/MM/yyyy} con importe de {item.importe}?");
			if (confirmed)
			{
				await Eliminar(item.id);
				await CargarDatos();
			}
		}

		private async Task Eliminar(int id)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/UMA/Borrar/{id}");
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

					await CargarDatos();
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


		public class Umas
		{
			public int id { get; set; }
			public DateOnly a_partir_de { get; set; }
			public decimal importe { get; set; }
		}



	}
}
