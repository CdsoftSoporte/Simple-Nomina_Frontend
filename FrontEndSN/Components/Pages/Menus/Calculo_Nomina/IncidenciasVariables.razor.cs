using DevExpress.Blazor;
using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class IncidenciasVariables:VentanaBase
	{
		protected List<IncidenciaVariableModel> ListaIncidencias { get; set; } = new();

		// Variables de selección
		protected int IdNominaSeleccionada { get; set; }
		protected int IdPeriodoSeleccionado { get; set; }

		protected IncidenciaVariableModel Modelo { get; set; } = new();
		private EditContext ContextoFormulario { get; set; }
		protected bool MostrarGoceSueldo => Modelo.IdConcepto >= 201 && Modelo.IdConcepto <= 299;
		protected bool MostrarModal { get; set; }

		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(
							CargarEmpleadosParaCombos(),
							CargarNominasParaCombo(),
							CargarConceptosParaCombo()
						);
		}

		protected async Task OnNominaChanged(int nuevoIdNomina)
		{
			IdNominaSeleccionada = nuevoIdNomina;
			IdPeriodoSeleccionado = 0; // Reiniciar periodo si cambian de nómina
			ListaIncidencias.Clear();  // Limpiar grid

			if (IdNominaSeleccionada > 0)
			{
				// Cargar los periodos de la nómina seleccionada
			    await CargarPeriodosParaCombo(IdNominaSeleccionada);
			}
			else
			{
				ListaPeriodos.Clear();
			}
		}

		protected void OnPeriodoChanged(int nuevoIdPeriodo)
		{
			if (nuevoIdPeriodo!=IdPeriodoSeleccionado)
			{
				ListaIncidencias.Clear();
				IdPeriodoSeleccionado = nuevoIdPeriodo;
			}
		}

		protected async Task BuscarIncidencias()
		{
			if (IdPeriodoSeleccionado == 0)
				return;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/IncidenciaVariable/ObtenerPorPeriodo/{IdPeriodoSeleccionado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<IncidenciaVariableModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaIncidencias = resultado?.Data ?? new();
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

		private async Task AbrirFormulario(IncidenciaVariableModel? item = null)
		{

			if (item == null)
			{
				Modelo = new IncidenciaVariableModel
				{
					Id = 0,
					IdPeriodo = IdPeriodoSeleccionado,
					Fecha = DateOnly.FromDateTime(DateTime.Now),
					Importe = 0.01m,
					ConGoceDeSueldo = 'N'
				};
			}
			else
			{
				Modelo = new IncidenciaVariableModel
				{
					Id = item.Id,
					IdEmpleado = item.IdEmpleado,
					IdPeriodo = item.IdPeriodo,
					IdConcepto = item.IdConcepto,
					Importe = item.Importe,
					Fecha = item.Fecha,
					ConGoceDeSueldo = item.ConGoceDeSueldo
				};
			}

			ContextoFormulario = new EditContext(Modelo);
			MostrarModal = true;
			await PrepararVentana(600);
			StateHasChanged();
		}

		protected async Task AgregarIncidencia()
		{
			if (ContextoFormulario == null || !ContextoFormulario.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url = Modelo.Id == 0
					? "api/IncidenciaVariable/Agregar"
					: $"api/IncidenciaVariable/Editar/{Modelo.Id}";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(Modelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success",
						Modelo.Id == 0 ? "Movimiento registrado exitosamente." : "Movimiento actualizado correctamente.");
					MostrarModal = false;
					await BuscarIncidencias();
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
		}

		private async Task ConfirmarEliminar(IncidenciaVariableModel item)
		{
			bool confirmed = await JS.InvokeAsync<bool>("notifier.confirm",
			"¿Eliminar Registro?",
			 $"¿Deseas borrar la incidencia? Esta acción no se puede deshacer");
			if (confirmed)
			{
				await BorrarIncidencia(item);
				await BuscarIncidencias();
			}
		}
		private async Task BorrarIncidencia(IncidenciaVariableModel incidencia)
		{

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/IncidenciaVariable/Borrar/{incidencia.Id}");
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

					await BuscarIncidencias();
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

		protected void OnConceptoCambiado(int nuevoIdConcepto)
		{
			Modelo.IdConcepto = nuevoIdConcepto;

			// Si el concepto ya no está en el rango 201-299, limpiamos el checkbox por seguridad
			if (!MostrarGoceSueldo)
			{
				Modelo.ConGoceDeSueldo = 'N';
			}
		}


		//este método es propio del DxGrid, este funciona para acceder a propiedades dentro del mismo y poder editar cosas de él (en este caso
		//lo estoy utilizando para poner un width: 100% al Searchbox, se pueden editar más cosas de css e incluso asignarle clases)
		void Grid_CustomizeElement(GridCustomizeElementEventArgs e)
		{
			if (e.ElementType == GridElementType.SearchBoxContainer)
			{
				e.Style = "Width: 100%;";


			}
		}

		private async Task CerrarFormulario()
		{
			MostrarModal = false;
			await InvokeAsync(StateHasChanged);
		}


		public class IncidenciaVariableModel
		{
			public int Id { get; set; }

			[JsonPropertyName("id_empleado")]
			public int IdEmpleado { get; set; }

			[JsonPropertyName("id_periodo")]
			public int IdPeriodo { get; set; }
			
			[JsonPropertyName("id_concepto")]
			public int IdConcepto { get; set; }
			public decimal Importe { get; set; }
			public DateOnly Fecha { get; set; }

			[JsonPropertyName("con_goce_sueldo")]
			public char ConGoceDeSueldo { get; set; }

			[JsonPropertyName("nombre_empleado")]
			public string NombreEmpleado { get; set; }
			
			[JsonPropertyName("nombre_concepto")]
			public string NombreConcepto { get; set; }

			[JsonIgnore]
			public bool ConGoceSueldoBool
			{
				get => ConGoceDeSueldo == 'S';
				set => ConGoceDeSueldo = value ? 'S' : 'N';
			}

		}
	}
}
