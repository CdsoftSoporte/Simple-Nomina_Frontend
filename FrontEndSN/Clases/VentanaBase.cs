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
		protected List<RegistroPatronalItem> ListaPatrones {  get; set; }
		protected List<CentrosDeCostosItem> ListaCentros { get; set; }
		protected List<DepartamentoItem> ListaDepartamentos { get; set; }
		protected List<PuestoItem> ListaPuestos { get; set; }


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
			ListaEjercicios ??= new List<EjerciciosItem>();
			for (int i = 2026; i <= anioActual + 1; i++)
			{
				ListaEjercicios.Add(new EjerciciosItem { Ejercicio = i.ToString() });
			}
			
		}

		protected async Task CargarDepartamentosParaCombo(char TodosDepartamentos='N')
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Departamentos/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<DepartamentoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaDepartamentos = resultado?.Data ?? new();
					if (TodosDepartamentos == 'S')
					{
						ListaDepartamentos.Insert(0, new DepartamentoItem { Id = 0, Nombre="Todos Los Deptos." });
					}

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

		protected async Task CargarPuestosParaCombo(char TodosPuestos='N')
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Puesto/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PuestoItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaPuestos = resultado?.Data ?? new();
					if (TodosPuestos == 'S')
					{
						ListaPuestos.Insert(0, new PuestoItem { Id = 0, Nombre = "Todos Los Puestos" });
					}

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

		protected async Task CargarPatronesParaCombo()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/RegistrosPatronales/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistroPatronalItem>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaPatrones = resultado?.Data ?? new();
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
		}

		protected async Task CargarEmpleadosParaCombos(char TodosEmpleados='N')
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
					if (TodosEmpleados == 'S')
					{
						ListaEmpleados.Insert(0, new EmpleadoDTO { Id = 0, Paterno = "Todos Los Empleados", Materno = "" });
					}
					
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

		protected async Task CargarCentrosDeCostosParaCombo(char TodosLosCentros='N')
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
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<CentrosDeCostosItem>>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);
					ListaCentros = resultado.Data ?? new();
					
					if (TodosLosCentros == 'S')
					{
						ListaCentros.Insert(0, new CentrosDeCostosItem { Id = 0, Nombre = "Todos los Centros" });
					}
					

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

	}
}
