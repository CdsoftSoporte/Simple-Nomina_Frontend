using Azure.Core;
using FrontEndSN.Clases;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Calculo_Nomina.IncidenciasVariables;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class CalculoNomina:VentanaBase
	{
		private bool empleadoExpandido = true;
		protected int IdNominaSeleccionada { get; set; }
		protected int IdPeriodoSeleccionado { get; set; }

		protected bool cargando = true;

		protected List<PeriodoConceptos> PeriodoConceptos = new();

		protected decimal TotalPercepciones = 0;
		protected decimal TotalDeducciones = 0;
		protected decimal TotalNeto = 0;

		// Colección para saber qué IDs de empleados están expandidos
		private HashSet<int> empleadosExpandidos = new HashSet<int>();

		protected override async Task OnInitializedAsync()
		{
			await CargarNominasParaCombo();
			cargando = false;
			StateHasChanged();
			
		}
		protected async Task OnNominaChanged(int nuevoIdNomina)
		{
			IdNominaSeleccionada = nuevoIdNomina;
			IdPeriodoSeleccionado = 0;
			PeriodoConceptos.Clear();

			if (IdNominaSeleccionada > 0)
			{
				
				await CargarPeriodosParaCombo(IdNominaSeleccionada);
			}
			else
			{
				ListaPeriodos.Clear();
				LimpiarTotales();

			}
		}



		protected async Task OnPeriodoChanged(int nuevoIdPeriodo)
		{
			IdPeriodoSeleccionado = nuevoIdPeriodo;
			PeriodoConceptos.Clear();
			LimpiarTotales();
			if (IdPeriodoSeleccionado>0 && IdNominaSeleccionada > 0)
			{
				CargaConceptosYEmpleados();
			}
		}

		private void ToggleEmpleado(int IdEmpleado)
		{
			if (empleadosExpandidos.Contains(IdEmpleado))
			{
				empleadosExpandidos.Remove(IdEmpleado);
			}
			else
			{
				empleadosExpandidos.Add(IdEmpleado);
			}
		}

		private async Task ExportarAnaliticoDeNomina()
		{

			var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
			if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

			var request = new HttpRequestMessage(HttpMethod.Get, $"api/Excel/AnaliticoDeNomina/{IdPeriodoSeleccionado}");
			 request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await Http.SendAsync(request);

			response.EnsureSuccessStatusCode();

			using var fileStream = await response.Content.ReadAsStreamAsync();

			using var streamRef = new DotNetStreamReference(stream: fileStream);

			await JS.InvokeVoidAsync("DescargarArchivos", streamRef, "Analitico.xlsx");
		}

		protected async Task CargaConceptosYEmpleados()
		{
			if (IdPeriodoSeleccionado == 0 || IdNominaSeleccionada==0)
				return;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Periodos/ObtenerPeriodo/{IdPeriodoSeleccionado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PeriodoConceptos>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					PeriodoConceptos = resultado?.Data ?? new();
					if (PeriodoConceptos.Count > 0)
						await SumaTotales();
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

		private async Task SumaTotales()
		{

			// Verificamos que la lista no esté vacía o nula
			if (PeriodoConceptos != null && PeriodoConceptos.Any())
			{
				TotalPercepciones = PeriodoConceptos.Sum(e => e.totalPercepciones);
				TotalDeducciones = PeriodoConceptos.Sum(e => e.totalDeducciones);
				TotalNeto = PeriodoConceptos.Sum(e => e.netoAPagar);
			}
			else
			{
				LimpiarTotales();
			}
		}

		private void LimpiarTotales()
		{
			TotalPercepciones = 0;
			TotalDeducciones = 0;
			TotalNeto = 0;
		}

		protected async Task EjecutarCalculo()
		{

			bool confirmado = await JS.InvokeAsync<bool>("notifier.confirm", "Calcular Nómina", "¿Estás seguro de calcular la nómina para este periodo? La información actual se sobreescribirá");

			if (!confirmado) return;

			cargando = true;
			StateHasChanged();

			var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
			if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

			var request = new HttpRequestMessage(HttpMethod.Post, $"api/Calculo/CalcularNomina/{IdPeriodoSeleccionado}");
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

			var response = await Http.SendAsync(request);


			
			try
			{

				if (response.IsSuccessStatusCode)
				{
					await Task.Delay(800);
					await JS.InvokeVoidAsync("notifier.success", "Nómina calculada con éxito.");


					await CargaConceptosYEmpleados();
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
				cargando = false;
			}
		}


		private async Task ImprimirListaDeRaya()
		{
			var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
			if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

			var request = new HttpRequestMessage(HttpMethod.Get, $"api/Excel/ListaDeRaya/{IdPeriodoSeleccionado}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await Http.SendAsync(request);
			response.EnsureSuccessStatusCode();

			var htmlContent = await response.Content.ReadAsStringAsync();

			await JS.InvokeVoidAsync("AbrirImpresion", htmlContent);
		}



	}



	public class PeriodoConceptos
	{
		[JsonPropertyName("id_empleado")]
		public int IdEmpleado { get; set; }
		[JsonPropertyName("nombre_completo")]
		public string NombreCompleto{ get; set; }
		public string puesto{ get; set; }
		public string departamento{ get; set; }
		public List<ConceptoValorDTO> Conceptos{ get; set; }
		public decimal totalPercepciones{ get; set; }

		public decimal totalDeducciones{ get; set; }

		public decimal netoAPagar{ get; set; }

	}
	public class ConceptoValorDTO
	{
		public int id { get; set; }
		public string descripcion { get; set; }
		public decimal importe { get; set; }
		public char afecta_neto { get; set; }
		public char tipo { get; set; }
	}
}
