using Azure.Core;
using FrontEndSN.Clases;
using FrontEndSN.Components.Modales;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class PeriodosManuales : VentanaBase
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

		// Variable para controlar la referencia del componente hijo
		private AgregarEmpleado ModalEmpleado { get; set; }
		private AgregarConcepto ModalConcepto { get; set; }

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
			if (IdPeriodoSeleccionado > 0 && IdNominaSeleccionada > 0)
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
			if (IdPeriodoSeleccionado == 0 || IdNominaSeleccionada == 0)
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
					{
						await SumaTotales();
					}
					else
					{
						LimpiarTotales();
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

		private async Task ConfirmarEliminarEmpleado(PeriodoConceptos item)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			 $"¿Deseas eliminar al empleado {item.NombreCompleto} del periodo de nómina? Esta acción no se puede deshacer");
			if (confirmed)
			{
				await EliminarEmpleado(IdPeriodoSeleccionado,item.IdEmpleado);
				await CargaConceptosYEmpleados();
			}
		}

		private async Task EliminarEmpleado(int IdPeriodo,int IdEmpleado)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Delete, $"api/Periodos/EliminarEmpleado/{IdPeriodo}/{IdEmpleado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{

					if (response.IsSuccessStatusCode)
					{
						await JS.InvokeVoidAsync("notifier.success","Empleado eliminado del periodo correctamente");
					}
					else
					{
						await JS.InvokeVoidAsync("notifier.error", response.Content);
					}

					await CargaConceptosYEmpleados();
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

		private async Task ConfirmarEliminarConcepto(int IdEmpleado, int IdConcepto)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			"¿Deseas eliminar el concepto del periodo de nómina? Esta acción no se puede deshacer");
			
			if (confirmed)
			{
				await EliminarConcepto(IdPeriodoSeleccionado, IdEmpleado,IdConcepto);
				await CargaConceptosYEmpleados();
			}
		}

		private async Task EliminarConcepto(int IdPeriodo, int IdEmpleado,int IdConcepto)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Delete, $"api/Periodos/EliminarConcepto/{IdPeriodo}/{IdEmpleado}/{IdConcepto}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{

					if (response.IsSuccessStatusCode)
					{
						await JS.InvokeVoidAsync("notifier.success", "Concepto eliminado del empleado correctamente");
					}
					else
					{
						await JS.InvokeVoidAsync("notifier.error", response.Content);
					}

					await CargaConceptosYEmpleados();
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

		private void AbrirModalEmpleado()
		{
			ModalEmpleado?.Mostrar(IdPeriodoSeleccionado);
		}

		private void AbrirModalConcepto(int IdEmpleado,string NombreEmpleado)
		{
			ModalConcepto.Mostrar(IdNominaSeleccionada,IdPeriodoSeleccionado,IdEmpleado,NombreEmpleado);
		}

		private void AbrirModalConcepto(int IdEmpleado, int IdConcepto,decimal Importe,string NombreEmpleado)
		{
			ModalConcepto.Mostrar(IdNominaSeleccionada, IdPeriodoSeleccionado, IdEmpleado, IdConcepto,Importe,NombreEmpleado);
		}


	}

}
