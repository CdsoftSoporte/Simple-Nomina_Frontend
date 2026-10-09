using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Calculo_Nomina
{
	public partial class DescuentosInfonavit
	{

		private bool cargando = false;
		private bool isModalVisible = false;
		private int IdPatronSeleccionado = 0;
		private int IdEmpleadoSeleccionado = 0;
		public List<CreditosInfonavit> ListaRegistros = new();
		public CreditosInfonavit editModel = new();

		private List<dynamic> ListaTiposDescuento = new() {
		new { Valor = "1", Texto = "Cuota Fija Mensual"},
		new { Valor = "2", Texto = "Porcentaje sobre S.D.I."},
		new { Valor = "3", Texto = "Factor de Descuento (VSM)"},
		};

		private List<dynamic> ListaEstatus = new()
		{
			new {Valor="A",Texto="Activo"},
			new {Valor="I",Texto="Inactivo"}
		};

		private List<dynamic> ListaAplicarPagoSeguro = new()
		{
			new {Valor="S",Texto="Sí"},
			new {Valor="N",Texto="No"},
		};



		protected override async Task OnInitializedAsync()
		{
			cargando = true;
			await Task.WhenAll(
					CargarPatronesParaCombo(),
					CargarEmpleadosParaCombos()
						);

			cargando = false;
			StateHasChanged();
		}

		private async Task OnEmpleadoChanged(int NuevoEmpleado)
		{
			IdEmpleadoSeleccionado = NuevoEmpleado;
			await ComprobarEstadoBusqueda();
		}

		private async Task OnPatronChanged(int NuevoPatron)
		{
			IdPatronSeleccionado = NuevoPatron;
			await ComprobarEstadoBusqueda();
		}

		private async Task ComprobarEstadoBusqueda()
		{
			if (IdEmpleadoSeleccionado==0 || IdPatronSeleccionado == 0)
			{
				return;
			}
			await ObtenerCreditos();
		}

		private async Task ObtenerCreditos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				cargando = true;
				StateHasChanged();

				var request = new HttpRequestMessage(HttpMethod.Get, $"api/DescuentosInfonavit/ObtenerCreditos/{IdPatronSeleccionado}/{IdEmpleadoSeleccionado}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<CreditosInfonavit>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					ListaRegistros = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message + errorData.Details ?? "Error inesperado"));
				}

			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message + ":" + ex.InnerException);
			}
			finally
			{
				await Task.Delay(800);
				cargando = false;
				StateHasChanged();
			}
		}

		private string ValidarControles()
		{
			if (editModel.Descripcion.Length > 50)
			{
				return "El campo descripción no puede ser mayor a 50 caracteres";
			}
			else if (editModel.NumCuenta.Length != 10)
			{
				return "El campo Número de Crédito debe tener 10 digitos";
			}
			else if (editModel.TipoDescuento == null)
			{
				return "Seleccione un tipo de descuento";
			}

			//una persona no puede tener mas de un crédito activo
			if (editModel.Estatus == "A" || editModel.Estatus == "Activo")
			{
				// Buscamos con Any() si existe otro en la lista que sea Activo, ignorando el Id actual (por si estamos editando)
				bool yaExisteActivo = ListaRegistros.Any(c =>
					(c.Estatus == "A" || c.Estatus == "Activo") &&
					c.Id != editModel.Id
				);

				if (yaExisteActivo)
				{
					return "El empleado ya tiene un crédito activo. No es posible tener dos créditos activos simultáneamente.";
				}
			}
			return "";
		}

		private async Task Guardar()
		{

			string mensaje_error = ValidarControles();

			if (mensaje_error.Length > 0)
			{
				await JS.InvokeVoidAsync("notifier.error",mensaje_error);
				return;
			}

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");


				bool esNuevo = editModel.Id == 0;
				string url = esNuevo ? "api/DescuentosInfonavit/Agregar" : $"api/DescuentosInfonavit/Editar/{editModel.Id}";
				var metodo = esNuevo? HttpMethod.Post:HttpMethod.Put;

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
						await ObtenerCreditos();

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

				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.Message+" "+ ex.InnerException);
			}
		}



		private void MostrarModal(CreditosInfonavit? item = null)
		{
			if (item == null)
			{
				editModel = new CreditosInfonavit();
				editModel.Id = 0;
				editModel.IdEmpleado = IdEmpleadoSeleccionado;
				editModel.IdPatron = IdPatronSeleccionado;
				editModel.NumCuenta = "";
				editModel.Descripcion = "";
				editModel.FechaInicio = DateTime.Now;
				editModel.FechaFin = null;
				editModel.TipoDescuento = null;
				editModel.Importe = 1;
				editModel.AplicarSeguro = "S";
				editModel.Estatus = "A";
			}
			else
			{
				editModel = new CreditosInfonavit 
				{ 
					Id = item.Id,
					IdEmpleado = IdEmpleadoSeleccionado,
					IdPatron = IdPatronSeleccionado,
					NumCuenta = item.NumCuenta,
					Descripcion=item.Descripcion,
					FechaInicio=item.FechaInicio,
					FechaFin=item.FechaFin,
					TipoDescuento=item.TipoDescuento switch
					{
						"Cuota Fija Mensual" =>"1" ,
						"Porcentaje sobre S.D.I." =>"2" ,
						"Factor de Descuento (VSM)" =>"3" ,
						_=>"Desconocido"
					},
					Importe = item.Importe,
					AplicarSeguro=(item.AplicarSeguro=="Sí"?"S":"N"),
					Estatus=(item.Estatus=="Activo"?"A":"I")
				};
			}
			isModalVisible = true;
		}



	}



	public class CreditosInfonavit
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }


		[JsonPropertyName("descripcion")]
		public string Descripcion { get; set; }


		[JsonPropertyName("numero_cuenta_infonavit")]
		public string NumCuenta { get; set; }


		[JsonPropertyName("fecha_inicio")]
		public DateTime FechaInicio { get; set; }

		
		[JsonPropertyName("fecha_final")]
		public DateTime? FechaFin { get; set; }


		[JsonPropertyName("tipo_descuento")]
		public string TipoDescuento { get; set; }


		[JsonPropertyName("importe_descuento")]
		public decimal Importe { get; set; }


		[JsonPropertyName("aplicar_pago_seguro")]
		public string AplicarSeguro { get; set; }


		[JsonPropertyName("estatus")]
		public string Estatus { get; set; }

		[JsonPropertyName("id_empleado")]
		public int IdEmpleado { get; set; }
		[JsonPropertyName("id_patron")]
		public int IdPatron { get; set; }

	}


}
