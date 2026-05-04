using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using DevExpress.Blazor;
using FrontEndSN.Components.Pages.Menus.Catalogos;

namespace FrontEndSN.Clases
{
	public class VentanaBase : ComponentBase
	{
		[Inject] protected IJSRuntime JS { get; set; }
		[Inject] protected NavigationManager Nav { get; set; }
		[Inject] protected HttpClient Http { get; set; }

		protected int VentanaX { get; set; }
		protected int VentanaY { get; set; }
		protected bool MostrarFormulario { get; set; }
		protected List<EmpleadoDTO> ListaEmpleados { get; set; }
		protected List<NominaItem> ListaNominas { get; set; }
		protected List<PeriodosItem> ListaPeriodos { get; set; }
		protected List<ConceptoItem> ListaConceptos { get; set; }
		protected List<EjerciciosItem> ListaEjercicios {  get; set; }


		protected void Grid_CustomizeElement(GridCustomizeElementEventArgs e)
		{
			if (e.ElementType == GridElementType.SearchBoxContainer)
			{
				e.Style = "Width: 100%;";


			}
		}
		protected async Task PrepararVentana(int anchoModal)
		{
			var anchoPantalla = await JS.InvokeAsync<int>("eval", "window.innerWidth");
			VentanaX = Math.Max(10, (anchoPantalla / 2) - (anchoModal / 2));
			VentanaY = 100;
			MostrarFormulario = true;
		}

		protected void LlenarEjerciciosParaCombo()
		{
			int anioActual = DateTime.Now.Year;
			//siempre llenará de 2026 al actual +1
			for (int i = 2026; i <= anioActual + 1; i++)
			{
				ListaEjercicios.Add(new EjerciciosItem { Ejercicio = i.ToString() });
			}
		}

		protected async Task CargarEmpleadosParaCombos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }
 
				var request = new HttpRequestMessage(HttpMethod.Get, "api/Empleados/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<EmpleadoDTO>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaEmpleados = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
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

		protected async Task CargarNominasParaCombo()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Nominas/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<NominaItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaNominas = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
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

		protected async Task CargarPeriodosParaCombo(int idnomina)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Periodos/ListarPorNomina/{idnomina}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PeriodosItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaPeriodos = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
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

		protected async Task CargarConceptosParaCombo()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Conceptos/ListarDTO");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<ConceptoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaConceptos = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
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

	}
}
