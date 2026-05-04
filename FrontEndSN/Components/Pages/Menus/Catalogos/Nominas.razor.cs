using DevExpress.Blazor;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Nominas
	{
		private List<NominaModel> Lista = new();
		private List<RegistroPatronalModel> PatronesList = new();
		private NominaModel? NominaSeleccionada = null;
		private NominaModel Modelo = new();
		private EditContext? editContext;
		private bool isModalVisible = false;
		private int ejercicioInt = DateTime.Now.Year;

		private List<PeriodoModel> Periodos = new();
		private bool tienePeriodos = false;
		private bool mostrarGeneracion = false;
		private bool generandoPeriodos = false;
		private DateTime fechaInicioGeneracion = new DateTime(DateTime.Now.Year, 1, 1);
		private DateTime fechaFinGeneracion = new DateTime(DateTime.Now.Year, 12, 31);


		private List<dynamic> estatusOpciones = new()
		{
			new { Valor = 'S', Texto = "Activo"   },
			new { Valor = 'N', Texto = "Inactivo" }
		};

		private List<dynamic> tiposNomina = new()
		{
			new { Valor = 'M', Texto = "Mensual"   },
			new { Valor = 'Q', Texto = "Quincenal" },
			new { Valor = 'S', Texto = "Semanal"   },
			new { Valor = 'D', Texto = "Decenal"   },
			new { Valor = 'C', Texto = "Catorcenal"}
		};

		private List<dynamic> naturalezaOpciones = new()
		{
			new { Valor = 'O', Texto = "Ordinaria"     },
			new { Valor = 'E', Texto = "Extraordinaria"},
			new { Valor = 'A', Texto = "Aguinaldo"     },
			new { Valor = 'F', Texto = "Finiquito"     },
			new { Valor = 'L', Texto = "Liquidación"   }
			
		};


		private bool isModalPeriodoVisible = false;
		private PeriodoModel PeriodoModelo = new();
		private EditContext? editContextPeriodo;

		protected override async Task OnInitializedAsync()
		{
			await Task.WhenAll(CargarLista(), CargarPatrones());
		}

		private async void OnNominaChanged(NominaModel n)
		{
			NominaSeleccionada = n;
			mostrarGeneracion = false;
			await CargarPeriodos(n.id);
			StateHasChanged();
		}

		private async Task CargarPeriodos(int idNomina)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) return;

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/Periodos/ListarPorNomina/{idNomina}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PeriodoModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Periodos = resultado?.Data ?? new();
					tienePeriodos = Periodos.Any();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error al cargar periodos: " + ex.Message);
			}
			finally
			{
				StateHasChanged();
			}
		}

		private void MostrarPanelGeneracion()
		{
			fechaInicioGeneracion = new DateTime(DateTime.Now.Year, 1, 1);
			fechaFinGeneracion = new DateTime(DateTime.Now.Year, 12, 31);
			mostrarGeneracion = true;
			StateHasChanged();
		}

		private async Task GenerarPeriodos()
		{
			if (NominaSeleccionada == null) return;
			if (fechaInicioGeneracion >= fechaFinGeneracion)
			{
				await JS.InvokeVoidAsync("notifier.error", "La fecha de inicio debe ser menor a la fecha fin.");
				return;
			}

			generandoPeriodos = true;
			StateHasChanged();

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var payload = new
				{
					IdNomina = NominaSeleccionada.id,
					FechaInicio = DateOnly.FromDateTime(fechaInicioGeneracion),
					FechaFin = DateOnly.FromDateTime(fechaFinGeneracion)
				};

				var request = new HttpRequestMessage(HttpMethod.Post, "api/Periodos/GenerarPeriodos");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(payload);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", "Periodos generados correctamente.");
					mostrarGeneracion = false;
					await CargarPeriodos(NominaSeleccionada.id);
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", errorData?.Message ?? "Error al generar periodos.");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				generandoPeriodos = false;
				StateHasChanged();
			}
		}

		private async Task OnPeriodoSaving(GridEditModelSavingEventArgs e)
		{
			var editModel = (PeriodoModel)e.EditModel;
			var dataItem = (PeriodoModel)e.DataItem;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) return;

				// Sincronizar fecha_pago desde DateTime a DateOnly
				editModel.fecha_pago = DateOnly.FromDateTime(editModel.fecha_pago_dt);

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/Periodos/Editar/{editModel.id}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(editModel);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					// Actualizamos el item en la lista local
					dataItem.numero = editModel.numero;
					dataItem.naturaleza = editModel.naturaleza;
					dataItem.fecha_pago = editModel.fecha_pago;
					dataItem.descripcion = editModel.descripcion;
					await JS.InvokeVoidAsync("notifier.success", "Periodo actualizado.");
				}
				else
				{
					await JS.InvokeVoidAsync("notifier.error", "Error al guardar el periodo.");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error: " + ex.Message);
			}

			await InvokeAsync(StateHasChanged);
		}


		private async Task CargarLista()
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
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<NominaModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Lista = resultado?.Data ?? new();
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

		private async Task CargarPatrones()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) return;

				var request = new HttpRequestMessage(HttpMethod.Get, "api/RegistrosPatronales/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistroPatronalModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					PatronesList = resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error al cargar patrones: " + ex.Message);
			}
		}

		private string ObtenerDescripcionTipo(char tipo) => tipo switch
		{
			'M' => "Mensual",
			'Q' => "Quincenal",
			'S' => "Semanal",
			'D' => "Decenal",
			'C' => "Catorcenal",
			_ => "N/A"
		};

		private void MostrarModal(NominaModel? item = null)
		{
			if (item == null)
			{
				Modelo = new NominaModel
				{
					id = 0,
					descripcion = "",
					tipo = 'Q',
					activo = 'S',
					ejercicio = DateTime.Now.Year.ToString()
				};
				ejercicioInt = DateTime.Now.Year;
			}
			else
			{
				Modelo = new NominaModel
				{
					id = item.id,
					descripcion = item.descripcion,
					id_patron = item.id_patron,
					tipo = item.tipo,
					activo = item.activo,
					ejercicio = item.ejercicio
				};
				ejercicioInt = int.TryParse(item.ejercicio, out int anio) ? anio : DateTime.Now.Year;
			}

			editContext = new EditContext(Modelo);
			isModalVisible = true;
			StateHasChanged();
		}


		private void MostrarModalPeriodo()
		{
			PeriodoModelo = new PeriodoModel
			{
				id_nomina = NominaSeleccionada!.id,
				numero = Periodos.Any() ? Periodos.Max(p => p.numero) + 1 : 1,
				naturaleza = 'O',
				desde_dt = DateTime.Today,
				hasta_dt = DateTime.Today,
				fecha_pago_dt = DateTime.Today,
				descripcion = ""
			};

			editContextPeriodo = new EditContext(PeriodoModelo);
			isModalPeriodoVisible = true;
			StateHasChanged();
		}

		private async Task GuardarPeriodoManual()
		{
			if (editContextPeriodo == null || !editContextPeriodo.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				// Sincronizar DateOnly desde DateTime
				PeriodoModelo.desde = DateOnly.FromDateTime(PeriodoModelo.desde_dt);
				PeriodoModelo.hasta = DateOnly.FromDateTime(PeriodoModelo.hasta_dt);
				PeriodoModelo.fecha_pago = DateOnly.FromDateTime(PeriodoModelo.fecha_pago_dt);

				// Detectar si es última nómina del mes
				PeriodoModelo.ultima_nomina_mes = PeriodoModelo.hasta.Month != PeriodoModelo.hasta.AddDays(1).Month ? 'S' : 'N';
				PeriodoModelo.periodo_manual = 'S';

				var request = new HttpRequestMessage(HttpMethod.Post, "api/Periodos/AgregarManual");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(PeriodoModelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", "Periodo agregado exitosamente.");
					isModalPeriodoVisible = false;
					await CargarPeriodos(NominaSeleccionada!.id);
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", errorData?.Message ?? "Error al guardar.");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			// Sincronizar ejercicio del SpinEdit al modelo
			Modelo.ejercicio = ejercicioInt.ToString();

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url = Modelo.id == 0
					? "api/Nominas/Agregar"
					: $"api/Nominas/Editar/{Modelo.id}";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(Modelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success",
						Modelo.id == 0 ? "Nómina creada exitosamente." : "Nómina actualizada correctamente.");
					isModalVisible = false;
					await CargarLista();
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

		// Modelos
		public class NominaModel
		{
			public int id { get; set; }

			[Required(ErrorMessage = "La descripción es obligatoria")]
			[StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
			public string descripcion { get; set; } = string.Empty;

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione un registro patronal")]
			public int id_patron { get; set; }

			[Required(ErrorMessage = "Seleccione un tipo de nómina")]
			public char tipo { get; set; } = 'Q';

			public char activo { get; set; } = 'S';

			[Required(ErrorMessage = "El ejercicio es obligatorio")]
			public string ejercicio { get; set; } = DateTime.Now.Year.ToString();
			public DateTime? fecha_creacion { get; set; }
			public DateTime? fecha_actualizacion { get; set; }
		}

		public class RegistroPatronalModel
		{
			public int id { get; set; }
			public string rfc { get; set; } = string.Empty;
			public string razon_social { get; set; } = string.Empty;
		}

		public class PeriodoModel
		{
			public int id { get; set; }
			public int id_nomina { get; set; }
			public int numero { get; set; }
			public char naturaleza { get; set; } = 'O';
			public DateOnly desde { get; set; }
			public DateOnly hasta { get; set; }
			public DateOnly fecha_pago { get; set; }
			public char? periodo_manual { get; set; } = 'N';

			// Auxiliares para DxDateEdit
			[System.Text.Json.Serialization.JsonIgnore]
			[Required(ErrorMessage = "La fecha desde es obligatoria")]
			public DateTime desde_dt
			{
				get => desde == DateOnly.MinValue ? DateTime.Today : desde.ToDateTime(TimeOnly.MinValue);
				set => desde = DateOnly.FromDateTime(value);
			}

			[System.Text.Json.Serialization.JsonIgnore]
			[Required(ErrorMessage = "La fecha hasta es obligatoria")]
			public DateTime hasta_dt
			{
				get => hasta == DateOnly.MinValue ? DateTime.Today : hasta.ToDateTime(TimeOnly.MinValue);
				set => hasta = DateOnly.FromDateTime(value);
			}

			// DateTime auxiliar para el DxDateEdit que no acepta DateOnly
			[System.Text.Json.Serialization.JsonIgnore]
			public DateTime fecha_pago_dt
			{
				get => fecha_pago.ToDateTime(TimeOnly.MinValue);
				set => fecha_pago = DateOnly.FromDateTime(value);
			}

			public string? descripcion { get; set; }
			public char ultima_nomina_mes { get; set; } = 'N';
		}
	}
}
